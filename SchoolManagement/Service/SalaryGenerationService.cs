// Backend section: application services and shared rules.
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;

namespace SchoolManagement.Service;

// Implements salary generation application behavior.
public sealed class SalaryGenerationService : BackgroundService
{
    // Dependencies and state used by this component.
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SalaryGenerationService> _logger;

    // Creates the component with its required dependencies.
    public SalaryGenerationService(
        IServiceScopeFactory scopeFactory,
        ILogger<SalaryGenerationService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateDueSalaries(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Automatic salary generation failed.");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task GenerateDueSalaries(CancellationToken cancellationToken)
    {
        var indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "India Standard Time" : "Asia/Kolkata"
        );
        var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indiaTimeZone).Date;

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dueStructures = await (
            from salary in context.StaffSalaryStructure
            join staff in context.Staff on salary.StaffId equals staff.Id
            where
                salary.IsActive
                && staff.IsActive
                && salary.EffectiveFrom.Date <= today
                && salary.SalaryGenerationDay <= today.Day
            select salary
        )
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (dueStructures.Count == 0)
            return;

        var staffIds = dueStructures.Select(x => x.StaffId).Distinct().ToList();
        var existingStaffIds = await context
            .SalaryPayment.Where(x =>
                staffIds.Contains(x.StaffId)
                && x.SalaryMonth == today.Month
                && x.SalaryYear == today.Year
            )
            .Select(x => x.StaffId)
            .ToListAsync(cancellationToken);
        var existing = existingStaffIds.ToHashSet();

        var generatedCount = 0;
        foreach (var salary in dueStructures.Where(x => !existing.Contains(x.StaffId)))
        {
            context.SalaryPayment.Add(
                new SalaryPayment
                {
                    StaffId = salary.StaffId,
                    schoolId = salary.schoolId,
                    SalaryMonth = today.Month,
                    SalaryYear = today.Year,
                    BasicSalary = salary.BasicSalary,
                    NetSalary = salary.BasicSalary,
                    Status = "Pending",
                    CreatedDate = today,
                }
            );
            generatedCount++;
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Generated {Count} due salary records for {Month}/{Year}.",
                generatedCount,
                today.Month,
                today.Year
            );
        }
    }
}
