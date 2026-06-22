using System.ComponentModel.DataAnnotations;

namespace IPA_Praesentationsverwaltung.Models.Domain;

/// <summary>
/// A physical room in which presentations take place. Stored as a separate
/// entity (3NF) so a room name is recorded only once and referenced by id.
/// </summary>
public class Room
{
    public int Id { get; set; }

    /// <summary>The room designation, e.g. "411".</summary>
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Presentations scheduled in this room.</summary>
    public ICollection<Presentation> Presentations { get; set; } = new List<Presentation>();
}
