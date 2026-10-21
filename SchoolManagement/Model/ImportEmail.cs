using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace SchoolManagement.Model;
[Index(nameof(NextAttemptAt))]
public class ImportEmail
{
    public long Id { get; set; }
    [MaxLength(320)] public string Recipient { get; set; } = "";
    public string ProtectedPayload { get; set; } = "";
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public DateTime? LockedUntil { get; set; }
    public int Attempts { get; set; }
}
