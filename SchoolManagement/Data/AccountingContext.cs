using Microsoft.EntityFrameworkCore;
using SchoolManagement.Model;
namespace SchoolManagement.Data;
public partial class AppDbContext
{
    public DbSet<AccountingAccount> AccountingAccounts => Set<AccountingAccount>();
    public DbSet<AccountingVoucher> AccountingVouchers => Set<AccountingVoucher>();
    public DbSet<AccountingLine> AccountingLines => Set<AccountingLine>();
    public DbSet<AccountingPeriod> AccountingPeriods => Set<AccountingPeriod>();
    public DbSet<AccountingAudit> AccountingAudits => Set<AccountingAudit>();
    public DbSet<AccountingReconciliation> AccountingReconciliations => Set<AccountingReconciliation>();
}
