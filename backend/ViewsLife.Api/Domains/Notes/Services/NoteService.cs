using Microsoft.EntityFrameworkCore;
using Ganss.Xss;
using ViewsLife.Api.Domains.Auth.Entities;
using ViewsLife.Api.Domains.Notes.Dtos;
using ViewsLife.Api.Domains.Notes.Entities;
using ViewsLife.Api.Domains.Notes.Interfaces;
using ViewsLife.Api.Infrastructure.Persistence;

namespace ViewsLife.Api.Domains.Notes.Services;

/// <summary>
/// Application service that manages tenant-scoped note operations.
/// </summary>
public sealed class NoteService : INoteService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly HtmlSanitizer _htmlSanitizer;

    public NoteService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        _htmlSanitizer = new HtmlSanitizer();
        
        // Configure allowed tags for rich text formatting
        var allowedTags = new[] { "p", "br", "strong", "em", "u", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li", "blockquote", "a", "img" };
        
        // Clear default tags and only add the allowed ones
        _htmlSanitizer.AllowedTags.Clear();
        foreach (var tag in allowedTags)
        {
            _htmlSanitizer.AllowedTags.Add(tag);
        }
        
        // Clear default attributes and only allow the specific ones we need
        _htmlSanitizer.AllowedAttributes.Clear();
        _htmlSanitizer.AllowedAttributes.Add("href");  // For links
        _htmlSanitizer.AllowedAttributes.Add("src");   // For images
        _htmlSanitizer.AllowedAttributes.Add("alt");   // For image alt text
        _htmlSanitizer.AllowedAttributes.Add("title"); // For tooltips
        
        // Only allow safe URL schemes - NO data: URLs which are XSS vectors
        _htmlSanitizer.AllowedSchemes.Clear();
        _htmlSanitizer.AllowedSchemes.Add("http");
        _htmlSanitizer.AllowedSchemes.Add("https");
        
        // Ensure dangerous tags are not allowed (explicit blocklist for safety)
        var dangerousTags = new[] { "script", "iframe", "object", "embed", "style", "link", "base", "meta", "form", "input", "button" };
        foreach (var tag in dangerousTags)
        {
            _htmlSanitizer.AllowedTags.Remove(tag);
        }
    }

    /// <summary>
    /// Sanitizes content based on its format. JSON content is safe and doesn't need sanitization.
    /// HTML content is sanitized to prevent XSS attacks.
    /// </summary>
    private string SanitizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        // Check if content is JSON (starts with { or [)
        var trimmed = content.Trim();
        if ((trimmed.StartsWith("{") && trimmed.EndsWith("}")) || 
            (trimmed.StartsWith("[") && trimmed.EndsWith("]")))
        {
            // Validate that it's valid JSON
            try
            {
                System.Text.Json.JsonDocument.Parse(trimmed);
                // JSON is safe from XSS - return as-is
                return content;
            }
            catch (System.Text.Json.JsonException)
            {
                // Invalid JSON, fall back to sanitization
                // This handles cases where content looks like JSON but isn't
            }
        }

        // Check if content contains HTML tags (indicating it's HTML)
        if (System.Text.RegularExpressions.Regex.IsMatch(content, @"<[^>]+>"))
        {
            // Contains HTML tags, sanitize it
            return _htmlSanitizer.Sanitize(content);
        }

        // Content doesn't appear to be HTML, return as-is
        // This covers plain text and JSON that might not match our detection
        return content;
    }

    public async Task<NoteResponseDto> CreateNoteAsync(
        string tenantId,
        string userId,
        CreateNoteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var note = new Note
        {
            TenantId = tenantId,
            CreatedByUserId = userId,
            Title = request.Title.Trim(),
            Content = SanitizeContent(request.Content.Trim()),
            Visibility = string.IsNullOrWhiteSpace(request.Visibility)
                ? "Private"
                : request.Visibility.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Add(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetNoteByIdAsync(tenantId, note.Id, cancellationToken) ?? throw new InvalidOperationException("Failed to retrieve created note");
    }

    public async Task<IEnumerable<NoteResponseDto>> GetNotesAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Note>()
            .Where(note => note.TenantId == tenantId)
            .Join(_dbContext.Set<ApplicationUser>(),
                note => note.CreatedByUserId,
                user => user.Id,
                (note, user) => new NoteResponseDto
                {
                    Id = note.Id,
                    TenantId = note.TenantId,
                    CreatedByUserId = note.CreatedByUserId,
                    CreatedByDisplayName = user.DisplayName,
                    Title = note.Title,
                    Content = note.Content,
                    Visibility = note.Visibility,
                    CreatedAtUtc = note.CreatedAtUtc,
                    UpdatedAtUtc = note.UpdatedAtUtc
                })
            .OrderByDescending(note => note.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<NoteResponseDto?> GetNoteByIdAsync(
        string tenantId,
        string noteId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Note>()
            .Where(note => note.Id == noteId && note.TenantId == tenantId)
            .Join(_dbContext.Set<ApplicationUser>(),
                note => note.CreatedByUserId,
                user => user.Id,
                (note, user) => new NoteResponseDto
                {
                    Id = note.Id,
                    TenantId = note.TenantId,
                    CreatedByUserId = note.CreatedByUserId,
                    CreatedByDisplayName = user.DisplayName,
                    Title = note.Title,
                    Content = note.Content,
                    Visibility = note.Visibility,
                    CreatedAtUtc = note.CreatedAtUtc,
                    UpdatedAtUtc = note.UpdatedAtUtc
                })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<NoteResponseDto?> UpdateNoteAsync(
        string tenantId,
        string userId,
        string noteId,
        UpdateNoteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var note = await _dbContext.Set<Note>()
            .SingleOrDefaultAsync(
                note => note.Id == noteId && note.TenantId == tenantId,
                cancellationToken);

        if (note is null)
        {
            return null;
        }

        note.Title = request.Title.Trim();
        note.Content = SanitizeContent(request.Content.Trim());
        note.Visibility = string.IsNullOrWhiteSpace(request.Visibility)
            ? "Private"
            : request.Visibility.Trim();
        note.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetNoteByIdAsync(tenantId, noteId, cancellationToken);
    }

    public async Task<bool> DeleteNoteAsync(
        string tenantId,
        string noteId,
        CancellationToken cancellationToken = default)
    {
        var note = await _dbContext.Set<Note>()
            .SingleOrDefaultAsync(
                note => note.Id == noteId && note.TenantId == tenantId,
                cancellationToken);

        if (note is null)
        {
            return false;
        }

        _dbContext.Remove(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static NoteResponseDto MapToDto(Note note) => new()
    {
        Id = note.Id,
        TenantId = note.TenantId,
        CreatedByUserId = note.CreatedByUserId,
        Title = note.Title,
        Content = note.Content,
        Visibility = note.Visibility,
        CreatedAtUtc = note.CreatedAtUtc,
        UpdatedAtUtc = note.UpdatedAtUtc
    };
}
