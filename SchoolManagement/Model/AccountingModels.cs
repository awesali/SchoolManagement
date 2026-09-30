// Backend section: domain and database models.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SchoolManagement.Model;

[Index(nameof(SchoolId), nameof(Code), IsUnique = true)]
// Represents accounting domain data.
public class AccountingAccount
{
    public int Id { get; set; }
    public int SchoolId { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = "";

    [Required, MaxLength(120)]
    public string Name { get; set; } = "";

    [Required, MaxLength(20)]
    public string Type { get; set; } = "Asset";
    public bool IsBank { get; set; }
}

[Index(nameof(SchoolId), nameof(RequestId), IsUnique = true)]
[Index(nameof(SchoolId), nameof(SourceKey), IsUnique = true)]
public class AccountingVoucher
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public Guid RequestId { get; set; }

    [MaxLength(100)]
    public string? SourceKey { get; set; }

    [MaxLength(20)]
    public string Kind { get; set; } = "Journal";
    public DateTime Date { get; set; }

    [MaxLength(500)]
    public string Narration { get; set; } = "";

    [MaxLength(120)]
    public string Reference { get; set; } = "";

    [MaxLength(20)]
    public string Status { get; set; } = "Draft";
    public int Revision { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? PostedBy { get; set; }
    public DateTime? PostedAt { get; set; }
    public int? ReversalOfId { get; set; }
    public List<AccountingLine> Lines { get; set; } = new();
}

public class AccountingLine
{
    public int Id { get; set; }
    public int AccountingVoucherId { get; set; }
    public int AccountId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Credit { get; set; }
    public DateTime? ClearedDate { get; set; }

    [MaxLength(120)]
    public string? BankReference { get; set; }
}

public class AccountingPeriod
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int SchoolId { get; set; }
    public DateTime? LockedThrough { get; set; }
}

public class AccountingAudit
{
    public long Id { get; set; }
    public int SchoolId { get; set; }
    public int UserId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;

    [MaxLength(40)]
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
}

public class AccountingReconciliation
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int AccountId { get; set; }
    public DateTime Date { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal StatementBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BookBalance { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnclearedNet { get; set; }
    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
