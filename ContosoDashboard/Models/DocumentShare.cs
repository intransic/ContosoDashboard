using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

/// <summary>
/// Represents a sharing relationship between a document and a recipient user
/// </summary>
public class DocumentShare
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int SharedWithUserId { get; set; }

    [Required]
    public int SharedByUserId { get; set; }

    [Required]
    public DateTime SharedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(DocumentId))]
    public Document Document { get; set; } = null!;

    [ForeignKey(nameof(SharedWithUserId))]
    public User SharedWith { get; set; } = null!;

    [ForeignKey(nameof(SharedByUserId))]
    public User SharedBy { get; set; } = null!;
}