// Backend section: database context and change tracking.
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Model;

namespace SchoolManagement.Data;

// Configures reception data access.
public partial class AppDbContext
{
    public DbSet<ReceptionEntry> ReceptionEntries => Set<ReceptionEntry>();
}
