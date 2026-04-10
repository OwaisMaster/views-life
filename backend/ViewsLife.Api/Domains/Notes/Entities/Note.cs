using System.ComponentModel.DataAnnotations;

namespace ViewsLife.Api.Domains.Notes.Entities;

/// <summary>
/// Represents a note owned by a tenant and created by an authenticated user.
/// </summary>
public sealed class Note
{
    /// <summary>
    /// Primary identifier for the note.
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tenant that owns the note.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// User who created the note.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Note title.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Raw note content.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Visibility for the note, defaults to Private.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Visibility { get; set; } = "Private";

    /// <summary>
    /// Creation timestamp in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp in UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
