using Microsoft.EntityFrameworkCore;
using SchoolManagement.Model;

namespace SchoolManagement.Data;

public partial class AppDbContext
{
    public DbSet<ReceptionEntry> ReceptionEntries => Set<ReceptionEntry>();
}
