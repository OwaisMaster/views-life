using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ViewsLife.Api.Common.Constants;
using ViewsLife.Api.Domains.Auth.Dtos;
using ViewsLife.Api.Domains.Notes.Dtos;
using ViewsLife.Api.Domains.Notes.Interfaces;

namespace ViewsLife.Api.Controllers;

/// <summary>
/// Exposes tenant-scoped note CRUD endpoints.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(NoteResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NoteResponseDto>> Create(
        [FromBody] CreateNoteRequestDto request,
        CancellationToken cancellationToken)
    {
        string tenantId = User.FindFirstValue(AuthConstants.TenantIdClaimType) ?? string.Empty;
        string userId = User.FindFirstValue(AuthConstants.UserIdClaimType) ?? string.Empty;

        NoteResponseDto response = await _noteService.CreateNoteAsync(
            tenantId,
            userId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = response.Id },
            response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<NoteResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<NoteResponseDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        string tenantId = User.FindFirstValue(AuthConstants.TenantIdClaimType) ?? string.Empty;

        var notes = await _noteService.GetNotesAsync(tenantId, cancellationToken);
        return Ok(notes);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NoteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponseDto>> GetById(
        string id,
        CancellationToken cancellationToken)
    {
        string tenantId = User.FindFirstValue(AuthConstants.TenantIdClaimType) ?? string.Empty;

        var note = await _noteService.GetNoteByIdAsync(tenantId, id, cancellationToken);
        if (note is null)
        {
            return NotFound();
        }

        return Ok(note);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(NoteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponseDto>> Update(
        string id,
        [FromBody] UpdateNoteRequestDto request,
        CancellationToken cancellationToken)
    {
        string tenantId = User.FindFirstValue(AuthConstants.TenantIdClaimType) ?? string.Empty;
        string userId = User.FindFirstValue(AuthConstants.UserIdClaimType) ?? string.Empty;

        var updated = await _noteService.UpdateNoteAsync(
            tenantId,
            userId,
            id,
            request,
            cancellationToken);

        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        string id,
        CancellationToken cancellationToken)
    {
        string tenantId = User.FindFirstValue(AuthConstants.TenantIdClaimType) ?? string.Empty;

        bool deleted = await _noteService.DeleteNoteAsync(tenantId, id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
