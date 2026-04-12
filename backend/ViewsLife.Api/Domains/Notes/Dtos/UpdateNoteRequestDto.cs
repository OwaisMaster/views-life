using System.ComponentModel.DataAnnotations;
using ViewsLife.Api.Common.Validation;

namespace ViewsLife.Api.Domains.Notes.Dtos;

/// <summary>
/// Payload used to update an existing note.
/// </summary>
public sealed class UpdateNoteRequestDto
{
    [Required]
    [TrimmedString]
    [StringLength(200, ErrorMessage = "Title must be 200 characters or fewer.")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [TrimmedString]
    [StringLength(50000, ErrorMessage = "Content must be 50000 characters or fewer.")]
    public string Content { get; set; } = string.Empty;

    [TrimmedString(allowEmpty: true)]
    [StringLength(50, ErrorMessage = "Visibility must be 50 characters or fewer.")]
    public string Visibility { get; set; } = "Private";
}
