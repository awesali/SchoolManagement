using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
namespace SchoolManagement.Service;

public interface IImportEmailService
{
    Task EnqueueAsync(string recipient, string subject, string body);
}

// Saving uses the caller's context and transaction, so rolled-back imports send no mail.
public sealed class ImportEmailService(AppDbContext db, IDataProtectionProvider protection) : IImportEmailService
{
    public Task EnqueueAsync(string recipient, string subject, string body)
    {
        var protector = protection.CreateProtector("SchoolManagement.ImportEmail.v1");
        db.ImportEmails.Add(new ImportEmail {
            Recipient = recipient,
            ProtectedPayload = protector.Protect(JsonSerializer.Serialize(new[] { subject, body }))
        });
        return Task.CompletedTask;
    }
}

public sealed class ImportEmailWorker(IServiceScopeFactory scopes, ILogger<ImportEmailWorker> logger) : BackgroundService
{
    // Additive schema setup also supports databases maintained without EF migrations.
    public override async Task StartAsync(CancellationToken token)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await EnsureSchemaAsync(db, token);
        await base.StartAsync(token);
    }

    public static Task EnsureSchemaAsync(AppDbContext db, CancellationToken token = default) => db.Database.ExecuteSqlRawAsync("""
        BEGIN TRANSACTION;
        DECLARE @result int;
        EXEC @result=sys.sp_getapplock @Resource='ImportEmails.Schema', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
        IF @result < 0 BEGIN ROLLBACK; THROW 51002, 'Unable to initialize import email queue.', 1; END;
        IF OBJECT_ID('dbo.ImportEmails', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.ImportEmails (
                Id bigint IDENTITY PRIMARY KEY,
                Recipient nvarchar(320) NOT NULL,
                ProtectedPayload nvarchar(max) NOT NULL,
                NextAttemptAt datetime2 NOT NULL,
                LockedUntil datetime2 NULL,
                Attempts int NOT NULL
            );
            CREATE INDEX IX_ImportEmails_NextAttemptAt ON dbo.ImportEmails(NextAttemptAt);
        END;
        COMMIT;
        """, token);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;
                var ids = await db.ImportEmails.AsNoTracking()
                    .Where(x => x.NextAttemptAt <= now && (x.LockedUntil == null || x.LockedUntil < now))
                    .OrderBy(x => x.Id).Select(x => x.Id).Take(4).ToListAsync(stoppingToken);
                await Task.WhenAll(ids.Select(id => DeliverAsync(id, stoppingToken)));
                await Task.Delay(ids.Count == 0 ? 2000 : 100, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Import email queue processing failed; queued messages remain pending.");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task DeliverAsync(long id, CancellationToken token)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        // Atomic lease prevents multiple API instances sending the same queued item.
        var claimed = await db.ImportEmails.Where(x => x.Id == id && x.NextAttemptAt <= now && (x.LockedUntil == null || x.LockedUntil < now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LockedUntil, now.AddMinutes(5)), token);
        if (claimed == 0) return;
        var row = await db.ImportEmails.SingleAsync(x => x.Id == id, token);
        try
        {
            var protector = scope.ServiceProvider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("SchoolManagement.ImportEmail.v1");
            var payload = JsonSerializer.Deserialize<string[]>(protector.Unprotect(row.ProtectedPayload))!;
            await scope.ServiceProvider.GetRequiredService<IEmailService>().SendEmailAsync(row.Recipient, payload[0], payload[1]);
            // Remove delivered credentials rather than retaining plaintext or encrypted mail indefinitely.
            await db.ImportEmails.Where(x => x.Id == id).ExecuteDeleteAsync(token);
        }
        catch (Exception ex)
        {
            row.Attempts++;
            row.LockedUntil = null;
            row.NextAttemptAt = DateTime.UtcNow.AddSeconds(Math.Min(3600, 30 * Math.Pow(2, Math.Min(row.Attempts, 7))));
            await db.SaveChangesAsync(token);
            logger.LogWarning("Import email {EmailId} delivery failed ({ErrorType}); retry scheduled, attempt {Attempt}.", id, ex.GetType().Name, row.Attempts);
        }
    }
}
