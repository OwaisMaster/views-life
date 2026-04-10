using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Notes.Dtos;
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
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost")
    });

    private async Task<HttpClient> CreateAuthenticatedClientAsync(
        string displayName,
        string email,
        string password,
        string tenantName)
    {
        var client = CreateClient();

        var registerRequest = new RegisterRequestDto
        {
            DisplayName = displayName,
            Email = email,
            Password = password,
            TenantName = tenantName
        };

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register",
            registerRequest);

        response.EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task AuthenticatedUser_CanCreateAndReadOwnNote()
    {
        HttpClient client = await CreateAuthenticatedClientAsync(
            "Note User",
            "note.user@example.com",
            "Password!123",
            "Note Tenant");

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
        HttpClient tenantOneClient = await CreateAuthenticatedClientAsync(
            "Tenant One User",
            "tenant.one@example.com",
            "Password!123",
            "Tenant One");

        HttpClient tenantTwoClient = await CreateAuthenticatedClientAsync(
            "Tenant Two User",
            "tenant.two@example.com",
            "Password!123",
            "Tenant Two");

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

    [Fact]
    public async Task CrossTenantNoteAccess_IsNotAllowed()
    {
        HttpClient tenantOneClient = await CreateAuthenticatedClientAsync(
            "Tenant One User",
            "tenant.one2@example.com",
            "Password!123",
            "Tenant One 2");

        HttpClient tenantTwoClient = await CreateAuthenticatedClientAsync(
            "Tenant Two User",
            "tenant.two2@example.com",
            "Password!123",
            "Tenant Two 2");

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
        HttpClient client = await CreateAuthenticatedClientAsync(
            "Note Validation User",
            "note.validation@example.com",
            "Password!123",
            "Validation Tenant");

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
        HttpClient client = await CreateAuthenticatedClientAsync(
            "Delete User",
            "delete.user@example.com",
            "Password!123",
            "Delete Tenant");

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
