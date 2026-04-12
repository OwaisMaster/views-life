using ViewsLife.Api.Domains.Notes.Dtos;

namespace ViewsLife.Api.Domains.Notes.Interfaces;

/// <summary>
/// Defines note-related application operations.
/// </summary>
public interface INoteService
{
    Task<NoteResponseDto> CreateNoteAsync(
        string tenantId,
        string userId,
        CreateNoteRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<NoteResponseDto>> GetNotesAsync(
        string tenantId,
        CancellationToken cancellationToken = default);

    Task<NoteResponseDto?> GetNoteByIdAsync(
        string tenantId,
        string noteId,
        CancellationToken cancellationToken = default);

    Task<NoteResponseDto?> UpdateNoteAsync(
        string tenantId,
        string userId,
        string noteId,
        UpdateNoteRequestDto request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteNoteAsync(
        string tenantId,
        string noteId,
        CancellationToken cancellationToken = default);
}
