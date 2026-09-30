// Backend section: domain and database models.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolManagement.Model;

[Table("ReceptionEntries")]
// Represents reception entry domain data.
public class ReceptionEntry
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [MaxLength(20)]
    public string Kind { get; set; } = "";

    [MaxLength(150)]
    public string Name { get; set; } = "";

    [MaxLength(30)]
    public string Phone { get; set; } = "";

    [MaxLength(150)]
    public string ContactPerson { get; set; } = "";

    [MaxLength(1000)]
    public string Purpose { get; set; } = "";

    [MaxLength(30)]
    public string Status { get; set; } = "";
    public DateTime ScheduledAt { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public int CreatedBy { get; set; }
    public int UpdatedBy { get; set; }

    [Timestamp]
    public byte[] Version { get; set; } = Array.Empty<byte>();
}
