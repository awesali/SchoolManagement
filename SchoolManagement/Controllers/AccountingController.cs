using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Data;
using SchoolManagement.Model;
using SchoolManagement.Service;
using System.Security.Claims;
using System.Data;

namespace SchoolManagement.Controllers;

[ApiController, Authorize, Route("api/accounting")]
public class AccountingController : ControllerBase, IAsyncActionFilter
{
    private readonly AppDbContext db;
    private readonly IPermissionService permissions;
    private int schoolId, userId;
    private static DateTime Today => DateTime.UtcNow.AddHours(5.5).Date;
    public AccountingController(AppDbContext db, IPermissionService permissions) { this.db = db; this.permissions = permissions; }
    [NonAction]
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)) { context.Result = Unauthorized(); return; }
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);
        if (user == null) { context.Result = Unauthorized(); return; }
        var role = await db.Roles.Where(x => x.Id == user.RoleId && x.IsActive).Select(x => x.RoleName).FirstOrDefaultAsync();
        if (!new[] { "ca", "accountant", "chartered accountant" }.Contains(role?.Trim().ToLowerInvariant())) { context.Result = Forbid(); return; }
        schoolId = user.School_Id ?? await db.Staff.Where(x => x.usersid == userId && x.IsActive).Select(x => (int?)x.SchoolId).FirstOrDefaultAsync() ?? 0;
        if (!await db.Schools.AnyAsync(x => x.Id == schoolId && x.IsActive)) { context.Result = NotFound(new { message = "No active school is assigned." }); return; }
        var action = HttpContext.Request.Method == "GET" ? "read" : HttpContext.Request.Method == "PUT" ? "update" : "create";
        if (!await permissions.HasPermissionAsync(User, "finance.accounts." + action)) { context.Result = Forbid(); return; }
        var executed = await next();
        if (executed.Exception is Microsoft.Data.SqlClient.SqlException sql && sql.Number == 208)
        {
            executed.ExceptionHandled = true;
            executed.Result = StatusCode(503, new { message = "Accounting database setup is required. Apply Database/Accounting/accounting.sql." });
        }
        else if (executed.Exception is DbUpdateException || executed.Exception is Microsoft.Data.SqlClient.SqlException { Number: 1205 })
        {
            executed.ExceptionHandled = true;
            executed.Result = Conflict(new { message = "The record changed or conflicts with an existing entry. Refresh and retry." });
        }
    }
    private void Audit(string action, string detail) => db.AccountingAudits.Add(new AccountingAudit { SchoolId = schoolId, UserId = userId, Action = action, Detail = detail });
    private Task<DateTime?> Locked() => db.AccountingPeriods.Where(x => x.SchoolId == schoolId).Select(x => x.LockedThrough).FirstOrDefaultAsync();
    private bool ValidRange(DateTime from, DateTime to) => from.Date >= new DateTime(1900, 1, 1) && from.Date <= to.Date && to.Date <= Today;
    private IQueryable<AccountingVoucher> Vouchers => db.AccountingVouchers.Where(x => x.SchoolId == schoolId);
    private IQueryable<AccountingLine> PostedLines(DateTime end) => db.AccountingLines.Where(l => Vouchers.Any(v => v.Id == l.AccountingVoucherId && v.Status == "Posted" && v.Date < end));

    [HttpGet("workspace")]
    public async Task<IActionResult> Workspace(DateTime from, DateTime to, int page = 1)
    {
        if (!ValidRange(from, to) || page < 1 || page > 100000) return BadRequest(new { message = "Choose a valid date range and page." });
        from = from.Date; var end = to.Date.AddDays(1);
        var accounts = await db.AccountingAccounts.AsNoTracking().Where(x => x.SchoolId == schoolId).OrderBy(x => x.Code).ToListAsync();
        var sums = await (from l in db.AccountingLines.AsNoTracking()
                          join v in Vouchers on l.AccountingVoucherId equals v.Id
                          where v.Status == "Posted" && v.Date < end
                          group new { l, v } by l.AccountId into g
                          select new { accountId = g.Key,
                              opening = g.Sum(x => x.v.Date < (@from) ? x.l.Debit - x.l.Credit : 0),
                              debit = g.Sum(x => x.v.Date >= (@from) ? x.l.Debit : 0),
                              credit = g.Sum(x => x.v.Date >= (@from) ? x.l.Credit : 0) }).ToListAsync();
        var report = accounts.Select(a => {
            var s = sums.FirstOrDefault(x => x.accountId == a.Id);
            return new { a.Id, a.Code, a.Name, a.Type, a.IsBank, opening = s?.opening ?? 0, debit = s?.debit ?? 0, credit = s?.credit ?? 0,
                closing = (s?.opening ?? 0) + (s?.debit ?? 0) - (s?.credit ?? 0) };
        }).ToList();
        var query = Vouchers.AsNoTracking().Where(x => x.Date >= from && x.Date < end);
        var vouchers = await query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Skip((page - 1) * 50).Take(50).Include(x => x.Lines).ToListAsync();
        return Ok(new { accounts, report, vouchers, total = await query.CountAsync(), page,
            lockedThrough = await Locked(), audit = await db.AccountingAudits.AsNoTracking().Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.Id).Take(100).ToListAsync(),
            canCreate = await permissions.HasPermissionAsync(User, "finance.accounts.create"),
            canUpdate = await permissions.HasPermissionAsync(User, "finance.accounts.update") });
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize()
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var existing = await db.AccountingAccounts.Where(x => x.SchoolId == schoolId).Select(x => x.Code).ToListAsync();
        var defaults = new[] { ("1000","Cash","Asset",false), ("1010","Bank","Asset",true), ("1100","Student receivables","Asset",false),
            ("2000","Supplier payables","Liability",false), ("3000","Opening equity","Equity",false),
            ("4000","School fee receipts","Income",false), ("4010","Transport receipts","Income",false), ("4020","Inventory receipts","Income",false),
            ("5000","Salary payments","Expense",false), ("5010","Operating expenses","Expense",false), ("5020","Fee refunds","Expense",false) };
        foreach (var a in defaults.Where(x => !existing.Contains(x.Item1)))
            db.AccountingAccounts.Add(new AccountingAccount { SchoolId = schoolId, Code = a.Item1, Name = a.Item2, Type = a.Item3, IsBank = a.Item4 });
        if (!await db.AccountingPeriods.AnyAsync(x => x.SchoolId == schoolId)) db.AccountingPeriods.Add(new AccountingPeriod { SchoolId = schoolId });
        Audit("Initialize", "Default chart of accounts initialized.");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Chart of accounts is ready." });
    }
    public record AccountRequest(string Code, string Name, string Type, bool IsBank);
    [HttpPost("accounts")]
    public async Task<IActionResult> Account(AccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Length > 20 || string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 120 ||
            !AccountingRules.Types.Contains(request.Type) || (request.IsBank && request.Type != "Asset"))
            return BadRequest(new { message = "Enter a code, name and valid account type. Bank accounts must be assets." });
        var a = new AccountingAccount { SchoolId = schoolId, Code = request.Code.Trim(), Name = request.Name.Trim(), Type = request.Type, IsBank = request.IsBank };
        db.AccountingAccounts.Add(a); Audit("Create account", a.Code + " - " + a.Name);
        await db.SaveChangesAsync(); return Ok(a);
    }
    public record LineRequest(int AccountId, decimal Debit, decimal Credit);
    public record VoucherRequest(Guid RequestId, DateTime Date, string Kind, string Narration, string Reference, List<LineRequest> Lines, int Revision = 0);
    private async Task<string?> ValidateVoucher(VoucherRequest r, List<AccountingLine> lines)
    {
        if (!AccountingRules.Kinds.Contains(r.Kind) || r.RequestId == Guid.Empty || (r.Reference?.Length ?? 0) > 120) return "Choose a valid voucher type and reference.";
        var error = AccountingRules.Validate(r.Date, r.Narration, lines, await Locked(), Today);
        if (error != null) return error;
        var ids = lines.Select(x => x.AccountId).Distinct().ToList();
        if (await db.AccountingAccounts.CountAsync(x => x.SchoolId == schoolId && ids.Contains(x.Id)) != ids.Count) return "Every account must belong to your school.";
        return null;
    }
    [HttpPost("vouchers")]
    public async Task<IActionResult> CreateVoucher(VoucherRequest r)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var old = await Vouchers.FirstOrDefaultAsync(x => x.RequestId == r.RequestId);
        if (old != null) return Ok(new { old.Id, message = "Voucher already saved." });
        var lines = (r.Lines ?? new()).Select(x => new AccountingLine { AccountId = x.AccountId, Debit = x.Debit, Credit = x.Credit }).ToList();
        var error = await ValidateVoucher(r, lines); if (error != null) return BadRequest(new { message = error });
        var v = new AccountingVoucher { SchoolId = schoolId, RequestId = r.RequestId, Date = r.Date.Date, Kind = r.Kind, Narration = r.Narration.Trim(), Reference = r.Reference?.Trim() ?? "", CreatedBy = userId, Lines = lines };
        db.AccountingVouchers.Add(v); Audit("Create draft", System.Text.Json.JsonSerializer.Serialize(new { v.RequestId, v.Date, v.Kind, v.Narration, v.Reference, lines = v.Lines.Select(l => new { l.AccountId, l.Debit, l.Credit }) }));
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { v.Id, message = "Draft saved. Post it to include it in the ledger." });
    }
    [HttpPut("vouchers/{id:int}")]
    public async Task<IActionResult> EditVoucher(int id, VoucherRequest r)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var v = await Vouchers.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return NotFound();
        if (v.Status != "Draft" || v.Revision != r.Revision) return Conflict(new { message = "Only the current draft can be edited. Refresh first." });
        if (v.SourceKey != null) return BadRequest(new { message = "Imported source vouchers cannot be edited. Void an incorrect draft and correct the source using an adjustment voucher." });
        var lines = (r.Lines ?? new()).Select(x => new AccountingLine { AccountId = x.AccountId, Debit = x.Debit, Credit = x.Credit }).ToList();
        var error = await ValidateVoucher(r, lines); if (error != null) return BadRequest(new { message = error });
        if (await Locked() is DateTime closed && v.Date <= closed) return BadRequest(new { message = "This accounting period is locked." });
        Audit("Edit draft", $"Voucher {id}, revision {v.Revision}: {System.Text.Json.JsonSerializer.Serialize(new { before = new { v.Date, v.Kind, v.Narration, v.Reference, lines = v.Lines.Select(l => new { l.AccountId, l.Debit, l.Credit }) }, after = r })}");
        db.AccountingLines.RemoveRange(v.Lines); v.Lines = lines; v.Date = r.Date.Date; v.Kind = r.Kind; v.Narration = r.Narration.Trim(); v.Reference = r.Reference?.Trim() ?? ""; v.Revision++;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Draft updated." });
    }
    public record DecisionRequest(int Revision, string Reason, DateTime? Date);
    [HttpPut("vouchers/{id:int}/{decision}")]
    public async Task<IActionResult> Decide(int id, string decision, DecisionRequest r)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var v = await Vouchers.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == id);
        if (v == null) return NotFound();
        if (v.Revision != r.Revision) return Conflict(new { message = "This voucher changed. Refresh before continuing." });
        var locked = await Locked();
        if (decision == "post" && v.Status == "Draft")
        {
            var error = AccountingRules.Validate(v.Date, v.Narration, v.Lines, locked, Today);
            if (error != null) return BadRequest(new { message = error });
            v.Status = "Posted"; v.PostedAt = DateTime.UtcNow; v.PostedBy = userId; v.Revision++;
        }
        else if (decision == "void" && v.Status == "Draft")
        {
            if (locked.HasValue && v.Date <= locked.Value) return BadRequest(new { message = "This accounting period is locked." });
            if (string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Length > 400) return BadRequest(new { message = "Enter a reason of up to 400 characters." });
            v.Status = "Void"; v.Revision++;
        }
        else if (decision == "reverse" && v.Status == "Posted")
        {
            if (string.IsNullOrWhiteSpace(r.Reason) || r.Reason.Length > 400 || !r.Date.HasValue) return BadRequest(new { message = "Enter a reversal date and reason of up to 400 characters." });
            if (await Vouchers.AnyAsync(x => x.ReversalOfId == id)) return Conflict(new { message = "This voucher has already been reversed." });
            var reversal = new AccountingVoucher { SchoolId = schoolId, RequestId = Guid.NewGuid(), Date = r.Date.Value.Date, Kind = "Journal",
                Narration = $"Reversal of V-{id}: {r.Reason}", Reference = $"V-{id}", Status = "Posted", CreatedBy = userId, PostedBy = userId, PostedAt = DateTime.UtcNow, ReversalOfId = id,
                Lines = v.Lines.Select(x => new AccountingLine { AccountId = x.AccountId, Debit = x.Credit, Credit = x.Debit }).ToList() };
            var error = AccountingRules.Validate(reversal.Date, reversal.Narration, reversal.Lines, locked, Today);
            if (error != null || reversal.Date < v.Date) return BadRequest(new { message = error ?? "Reversal date cannot precede the original voucher." });
            db.AccountingVouchers.Add(reversal); v.Revision++;
        }
        else return BadRequest(new { message = "This action is not available for the voucher's current status." });
        Audit(decision, $"Voucher V-{id}: {r.Reason}");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Voucher " + decision + " completed." });
    }

    [HttpGet("ledger")]
    public async Task<IActionResult> Ledger(int accountId, DateTime from, DateTime to)
    {
        if (!ValidRange(from, to) || !await db.AccountingAccounts.AnyAsync(x => x.Id == accountId && x.SchoolId == schoolId)) return BadRequest(new { message = "Select an account and valid dates." });
        var end = to.Date.AddDays(1); from = from.Date;
        var query = from l in db.AccountingLines.AsNoTracking() join v in Vouchers on l.AccountingVoucherId equals v.Id
                    where l.AccountId == accountId && v.Status == "Posted" && v.Date < end
                    select new { l.Id, v.Date, voucherId = v.Id, v.Narration, v.Reference, l.Debit, l.Credit, l.ClearedDate, l.BankReference };
        var opening = await query.Where(x => x.Date < from).SumAsync(x => (decimal?)(x.Debit - x.Credit)) ?? 0m;
        var rows = await query.Where(x => x.Date >= from).OrderBy(x => x.Date).ThenBy(x => x.Id).Take(5001).ToListAsync();
        if (rows.Count > 5000) return BadRequest(new { message = "More than 5,000 ledger lines. Narrow the date range." });
        return Ok(new { opening, rows });
    }
    public record LockRequest(DateTime Date);
    [HttpPut("period")]
    public async Task<IActionResult> Lock(LockRequest r)
    {
        if (r.Date.Date > Today || r.Date.Year < 1900) return BadRequest(new { message = "Choose a valid lock date." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var period = await db.AccountingPeriods.FirstOrDefaultAsync(x => x.SchoolId == schoolId);
        if (period?.LockedThrough != null && r.Date.Date <= period.LockedThrough) return BadRequest(new { message = "Closed periods cannot be reopened here." });
        if (await Vouchers.AnyAsync(x => x.Status == "Draft" && x.Date <= r.Date.Date)) return BadRequest(new { message = "Post or void all drafts through this date before locking." });
        if (period == null) { period = new AccountingPeriod { SchoolId = schoolId }; db.AccountingPeriods.Add(period); }
        period.LockedThrough = r.Date.Date; Audit("Lock period", r.Date.ToString("yyyy-MM-dd"));
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Period locked. Future corrections require a voucher in an open period." });
    }
    public record ClearRequest(DateTime? Date, string Reference);
    [HttpPut("lines/{id:int}/clear")]
    public async Task<IActionResult> Clear(int id, ClearRequest r)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var line = await db.AccountingLines.FirstOrDefaultAsync(x => x.Id == id);
        var v = line == null ? null : await Vouchers.FirstOrDefaultAsync(x => x.Id == line.AccountingVoucherId && x.Status == "Posted");
        if (v == null || line == null || !await db.AccountingAccounts.AnyAsync(x => x.Id == line.AccountId && x.SchoolId == schoolId && x.IsBank)) return NotFound();
        if ((await Locked()) is DateTime closed && (v.Date <= closed || line.ClearedDate <= closed)) return BadRequest(new { message = "Bank clearance in a closed period cannot be changed." });
        if ((r.Date.HasValue && (r.Date.Value.Date < v.Date || r.Date.Value.Date > Today || string.IsNullOrWhiteSpace(r.Reference))) || (r.Reference?.Length ?? 0) > 120)
            return BadRequest(new { message = "Use a clearance date between the voucher date and today, and a bank reference up to 120 characters." });
        Audit("Bank clearance", $"Line {id}: {line.ClearedDate:yyyy-MM-dd}/{line.BankReference} to {r.Date:yyyy-MM-dd}/{r.Reference}");
        line.ClearedDate = r.Date?.Date; line.BankReference = r.Date.HasValue ? r.Reference.Trim() : null;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Bank clearance updated." });
    }
    [HttpGet("bank")]
    public async Task<IActionResult> Bank(int accountId, DateTime date)
    {
        if (!ValidRange(date, date) || !await db.AccountingAccounts.AnyAsync(x => x.Id == accountId && x.SchoolId == schoolId && x.IsBank)) return BadRequest(new { message = "Select a bank account and valid statement date." });
        var q = PostedLines(date.Date.AddDays(1)).Where(x => x.AccountId == accountId);
        var book = await q.SumAsync(x => (decimal?)(x.Debit - x.Credit)) ?? 0m;
        var uncleared = await q.Where(x => !x.ClearedDate.HasValue || x.ClearedDate.Value > date.Date).SumAsync(x => (decimal?)(x.Debit - x.Credit)) ?? 0m;
        return Ok(new { book, uncleared, reconciled = book - uncleared,
            history = await db.AccountingReconciliations.AsNoTracking().Where(x => x.SchoolId == schoolId && x.AccountId == accountId).OrderByDescending(x => x.Id).Take(25).ToListAsync() });
    }
    public record ReconcileRequest(int AccountId, DateTime Date, decimal StatementBalance);
    [HttpPost("reconcile")]
    public async Task<IActionResult> Reconcile(ReconcileRequest r)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!ValidRange(r.Date, r.Date) || !await db.AccountingAccounts.AnyAsync(x => x.Id == r.AccountId && x.SchoolId == schoolId && x.IsBank)
            || decimal.Round(r.StatementBalance, 2) != r.StatementBalance || Math.Abs(r.StatementBalance) > 999999999999m)
            return BadRequest(new { message = "Select a bank account, date and valid statement balance." });
        var q = PostedLines(r.Date.Date.AddDays(1)).Where(x => x.AccountId == r.AccountId);
        var book = await q.SumAsync(x => (decimal?)(x.Debit - x.Credit)) ?? 0m;
        var uncleared = await q.Where(x => !x.ClearedDate.HasValue || x.ClearedDate.Value > r.Date.Date).SumAsync(x => (decimal?)(x.Debit - x.Credit)) ?? 0m;
        if (book - uncleared != r.StatementBalance) return BadRequest(new { message = "The statement does not reconcile. Resolve the difference before saving." });
        db.AccountingReconciliations.Add(new AccountingReconciliation { SchoolId = schoolId, AccountId = r.AccountId, Date = r.Date.Date, StatementBalance = r.StatementBalance, BookBalance = book, UnclearedNet = uncleared, CreatedBy = userId });
        Audit("Reconcile bank", $"Account {r.AccountId} on {r.Date:yyyy-MM-dd}: statement {r.StatementBalance}");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = "Reconciliation snapshot saved." });
    }

    public record SourceRow(string Key, DateTime Date, string Reference, decimal Amount, string Mode, string AccountCode, bool Outflow);
    private async Task<List<SourceRow>> Sources(DateTime from, DateTime to)
    {
        var end = to.Date.AddDays(1); from = from.Date;
        var rows = new List<SourceRow>();
        if (await permissions.HasPermissionAsync(User, "finance.fees.read"))
            rows.AddRange(await db.FeePayments.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive && x.Payment_Date >= from && x.Payment_Date < end && x.AmountPaid > 0)
                .Select(x => new SourceRow("fee:" + x.Id, x.Payment_Date, x.Receipt_Number, x.AmountPaid, x.Payment_Mode, "4000", false)).Take(501).ToListAsync());
        if (await permissions.HasPermissionAsync(User, "finance.salary.read"))
            rows.AddRange(await db.SalaryPayment.AsNoTracking().Where(x => x.schoolId == schoolId && x.Status == "Paid" && x.PaymentDate >= from && x.PaymentDate < end && x.NetSalary > 0)
                .Select(x => new SourceRow("salary:" + x.Id, x.PaymentDate!.Value, "Salary " + x.StaffId + " " + x.SalaryMonth + "/" + x.SalaryYear, x.NetSalary, x.PaymentMethod ?? "", "5000", true)).Take(501).ToListAsync());
        if (await permissions.HasPermissionAsync(User, "management.transport.read"))
            rows.AddRange(await (from p in db.TransportFeePayments.AsNoTracking() join f in db.TransportFees on p.TransportFeeId equals f.Id
                where f.SchoolId == schoolId && p.PaymentDate >= (@from) && p.PaymentDate < end && p.Amount > 0
                select new SourceRow("transport:" + p.Id, p.PaymentDate, p.ReceiptNumber, p.Amount, p.PaymentMode, "4010", false)).Take(501).ToListAsync());
        if (await permissions.HasPermissionAsync(User, "finance.accounts.update"))
            rows.AddRange(await (from p in db.InventoryPayments.AsNoTracking() join o in db.InventoryStudentOrders on p.StudentOrderId equals o.Id
                where o.SchoolId == schoolId && p.PaymentDate >= (@from) && p.PaymentDate < end && p.Amount > 0
                select new SourceRow("inventory:" + p.Id, p.PaymentDate, p.ReceiptNumber, p.Amount, p.PaymentMode, "4020", false)).Take(501).ToListAsync());
        return rows.OrderBy(x => x.Date).ThenBy(x => x.Key).ToList();
    }
    [HttpGet("sources")]
    public async Task<IActionResult> SourcePreview(DateTime from, DateTime to)
    {
        if (!ValidRange(from, to)) return BadRequest(new { message = "Choose valid dates." });
        var rows = await Sources(from, to);
        if (rows.Count > 500) return BadRequest(new { message = "More than 500 source records. Narrow the date range." });
        var keys = rows.Select(x => x.Key).ToList();
        var imported = await Vouchers.Where(x => x.SourceKey != null && keys.Contains(x.SourceKey)).Select(x => new { x.SourceKey, x.Id, x.Status }).ToListAsync();
        return Ok(new { rows, imported });
    }
    public record ImportRequest(DateTime From, DateTime To, List<string> Keys, int BankAccountId);
    [HttpPost("import")]
    public async Task<IActionResult> Import(ImportRequest r)
    {
        if (!ValidRange(r.From, r.To) || r.Keys == null || r.Keys.Count == 0 || r.Keys.Count > 500) return BadRequest(new { message = "Choose a valid date range and 1–500 source records." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var sources = await Sources(r.From, r.To);
        if (sources.Count > 500) return BadRequest(new { message = "Narrow the source date range." });
        var selected = sources.Where(x => r.Keys.Contains(x.Key)).ToList();
        if (selected.Count != r.Keys.Distinct().Count()) return BadRequest(new { message = "Some sources are unavailable. Refresh the preview." });
        var accounts = await db.AccountingAccounts.Where(x => x.SchoolId == schoolId).ToListAsync();
        var cash = accounts.FirstOrDefault(x => x.Code == "1000" && x.Type == "Asset");
        var bank = accounts.FirstOrDefault(x => x.Id == r.BankAccountId && x.IsBank);
        var locked = await Locked(); var count = 0;
        foreach (var s in selected)
        {
            if (await Vouchers.AnyAsync(x => x.SourceKey == s.Key)) continue;
            var control = accounts.FirstOrDefault(x => x.Code == s.AccountCode && x.Type == (s.Outflow ? "Expense" : "Income"));
            var payment = s.Mode.Trim().Equals("Cash", StringComparison.OrdinalIgnoreCase) ? cash : bank;
            if (control == null || payment == null) return BadRequest(new { message = "Initialize accounts and select the bank receiving or paying these non-cash transactions." });
            var lines = new List<AccountingLine> {
                new() { AccountId = payment.Id, Debit = s.Outflow ? 0 : s.Amount, Credit = s.Outflow ? s.Amount : 0 },
                new() { AccountId = control.Id, Debit = s.Outflow ? s.Amount : 0, Credit = s.Outflow ? 0 : s.Amount }
            };
            var narration = "Imported " + s.Key + " - " + s.Reference;
            var error = AccountingRules.Validate(s.Date, narration, lines, locked, Today);
            if (error != null) return BadRequest(new { message = s.Key + ": " + error });
            db.AccountingVouchers.Add(new AccountingVoucher { SchoolId = schoolId, RequestId = Guid.NewGuid(), SourceKey = s.Key, Date = s.Date.Date,
                Kind = s.Outflow ? "Payment" : "Receipt", Narration = narration, Reference = s.Reference.Length > 120 ? s.Reference[..120] : s.Reference, CreatedBy = userId, Lines = lines }); count++;
        }
        Audit("Import drafts", $"{count} source records for {r.From:yyyy-MM-dd} to {r.To:yyyy-MM-dd}");
        await db.SaveChangesAsync(); await tx.CommitAsync(); return Ok(new { message = $"{count} draft vouchers imported. Review and post them." });
    }
}
