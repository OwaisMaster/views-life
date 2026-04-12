using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ViewsLife.Api.Common.Constants;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Domains.Notes.Dtos;
using ViewsLife.Api.Infrastructure.Persistence;
using Xunit;

namespace ViewsLife.Api.IntegrationTests.Notes;

public sealed class NotesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public NotesEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = false, // Disable cookies since we use test headers
        BaseAddress = new Uri("https://localhost")
    });

    private HttpClient CreateAuthenticatedClient(string userId, string tenantId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantIdHeader, tenantId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.TenantRoleHeader, "Owner");
        client.DefaultRequestHeaders.Add(TestAuthHandler.DisplayNameHeader, "Test User");
        return client;
    }

    private Task<(string userId, string tenantId)> SeedTestDataAsync()
    {
        return SeedTestDataAsync(string.Empty, string.Empty);
    }

    [Fact]
    public async Task AuthenticatedUser_CanCreateAndReadOwnNote()
    {
        var (userId, tenantId) = await SeedTestDataAsync();
        HttpClient client = CreateAuthenticatedClient(userId, tenantId);

        var createRequest = new CreateNoteRequestDto
        {
            Title = "My first note",
            Content = "This is a test note.",
            Visibility = "Private"
        };

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/notes",
            createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        NoteResponseDto? createdNote = await createResponse.Content.ReadFromJsonAsync<NoteResponseDto>();
        createdNote.Should().NotBeNull();
        createdNote!.Title.Should().Be(createRequest.Title);
        createdNote.Content.Should().Be(createRequest.Content);
        createdNote.Visibility.Should().Be(createRequest.Visibility);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/notes/{createdNote.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        NoteResponseDto? fetchedNote = await getResponse.Content.ReadFromJsonAsync<NoteResponseDto>();
        fetchedNote.Should().NotBeNull();
        fetchedNote!.Id.Should().Be(createdNote.Id);
        fetchedNote.TenantId.Should().Be(createdNote.TenantId);
    }

    [Fact]
    public async Task NoteList_ReturnsOnlyCurrentTenantNotes()
    {
        var (userId1, tenantId1) = await SeedTestDataAsync();
        var (userId2, tenantId2) = await SeedTestDataAsync("tenant2", "user2");

        HttpClient tenantOneClient = CreateAuthenticatedClient(userId1, tenantId1);
        HttpClient tenantTwoClient = CreateAuthenticatedClient(userId2, tenantId2);

        var firstNote = new CreateNoteRequestDto
        {
            Title = "Tenant One Note",
            Content = "Owned by tenant one.",
            Visibility = "Private"
        };

        var secondNote = new CreateNoteRequestDto
        {
            Title = "Tenant Two Note",
            Content = "Owned by tenant two.",
            Visibility = "Private"
        };

        HttpResponseMessage firstResponse = await tenantOneClient.PostAsJsonAsync(
            "/api/notes",
            firstNote);
        firstResponse.EnsureSuccessStatusCode();
        NoteResponseDto? createdFirst = await firstResponse.Content.ReadFromJsonAsync<NoteResponseDto>();
        createdFirst.Should().NotBeNull();

        HttpResponseMessage secondResponse = await tenantTwoClient.PostAsJsonAsync(
            "/api/notes",
            secondNote);
        secondResponse.EnsureSuccessStatusCode();
        NoteResponseDto? createdSecond = await secondResponse.Content.ReadFromJsonAsync<NoteResponseDto>();
        createdSecond.Should().NotBeNull();

        HttpResponseMessage listResponse = await tenantOneClient.GetAsync("/api/notes");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        NoteResponseDto[]? notes = await listResponse.Content.ReadFromJsonAsync<NoteResponseDto[]>();
        notes.Should().NotBeNull();
        notes!.Should().ContainSingle(note => note.Id == createdFirst!.Id);
        notes.Should().NotContain(note => note.Id == createdSecond!.Id);
    }

    private async Task<(string userId, string tenantId)> SeedTestDataAsync(string tenantSuffix = "", string userSuffix = "")
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        string userId = $"test-user-id{userSuffix}";
        string tenantId = $"test-tenant-id{tenantSuffix}";

        // Check if already exists
        if (await dbContext.Set<ApplicationUser>().AnyAsync(u => u.Id == userId))
        {
            return (userId, tenantId);
        }

        // Create test user
        var user = new ApplicationUser
        {
            Id = userId,
            DisplayName = $"Test User{userSuffix}",
            Email = $"test{userSuffix}@example.com",
            NormalizedEmail = $"TEST{userSuffix.ToUpper()}@EXAMPLE.COM",
            PasswordHash = "hashed-password",
            AuthProvider = "Local",
            ProviderSubjectId = $"test{userSuffix}@example.com",
            IsEmailVerified = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Create test tenant
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = $"Test Tenant{tenantSuffix}",
            Slug = $"test-tenant{tenantSuffix}",
            OwnerUserId = user.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Create tenant membership
        var membership = new TenantMembership
        {
            Id = Guid.NewGuid().ToString(),
            TenantId = tenant.Id,
            UserId = user.Id,
            Role = "Owner",
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Add(user);
        dbContext.Add(tenant);
        dbContext.Add(membership);
        await dbContext.SaveChangesAsync();

        return (user.Id, tenant.Id);
    }

    [Fact]
    public async Task CrossTenantNoteAccess_IsNotAllowed()
    {
        var (userId1, tenantId1) = await SeedTestDataAsync();
        var (userId2, tenantId2) = await SeedTestDataAsync("2", "2");

        HttpClient tenantOneClient = CreateAuthenticatedClient(userId1, tenantId1);
        HttpClient tenantTwoClient = CreateAuthenticatedClient(userId2, tenantId2);

        var createRequest = new CreateNoteRequestDto
        {
            Title = "Cross tenant note",
            Content = "Should not be visible outside tenant.",
            Visibility = "Private"
        };

        HttpResponseMessage createResponse = await tenantOneClient.PostAsJsonAsync(
            "/api/notes",
            createRequest);
        createResponse.EnsureSuccessStatusCode();

        NoteResponseDto? createdNote = await createResponse.Content.ReadFromJsonAsync<NoteResponseDto>();
        createdNote.Should().NotBeNull();

        HttpResponseMessage readResponse = await tenantTwoClient.GetAsync($"/api/notes/{createdNote!.Id}");
        readResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var updateRequest = new UpdateNoteRequestDto
        {
            Title = "Attempted update",
            Content = "Should not succeed.",
            Visibility = "Private"
        };

        HttpResponseMessage updateResponse = await tenantTwoClient.PutAsJsonAsync(
            $"/api/notes/{createdNote.Id}",
            updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        HttpResponseMessage deleteResponse = await tenantTwoClient.DeleteAsync($"/api/notes/{createdNote.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidPayload_ReturnsBadRequest()
    {
        var (userId, tenantId) = await SeedTestDataAsync();
        HttpClient client = CreateAuthenticatedClient(userId, tenantId);

        var invalidRequest = new CreateNoteRequestDto
        {
            Title = "   ",
            Content = "   ",
            Visibility = "Private"
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/notes",
            invalidRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AuthenticatedUser_CanDeleteOwnNote()
    {
        var (userId, tenantId) = await SeedTestDataAsync();
        HttpClient client = CreateAuthenticatedClient(userId, tenantId);

        var createRequest = new CreateNoteRequestDto
        {
            Title = "Note to delete",
            Content = "This note will be deleted.",
            Visibility = "Private"
        };

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            "/api/notes",
            createRequest);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        NoteResponseDto? createdNote = await createResponse.Content.ReadFromJsonAsync<NoteResponseDto>();
        createdNote.Should().NotBeNull();

        // Delete the note
        HttpResponseMessage deleteResponse = await client.DeleteAsync($"/api/notes/{createdNote!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the note is gone
        HttpResponseMessage getResponse = await client.GetAsync($"/api/notes/{createdNote.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnauthenticatedRequests_ReturnUnauthorized()
    {
        HttpClient client = CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/notes");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
