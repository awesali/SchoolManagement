using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SchoolManagement.Controllers;
using SchoolManagement.Data;
using SchoolManagement.Model;
using SchoolManagement.Service;
using static SchoolManagement.Controllers.AccountingController;

// This executable creates and removes only its own randomly named LOCAL test database.
// No application connection string, credentials or remote database is used.
var dbName = "AccountingTest_" + Guid.NewGuid().ToString("N");
var master = @"Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=30";
var connection = new SqlConnectionStringBuilder(master) { InitialCatalog = dbName }.ConnectionString;
var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../SchoolManagement"));
var passed = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception("FAILED: " + message); Console.WriteLine("PASS: " + message); passed++; }
async Task Sql(string cs, string sql) { await using var c = new SqlConnection(cs); await c.OpenAsync(); await using var cmd = new SqlCommand(sql, c) { CommandTimeout = 60 }; await cmd.ExecuteNonQueryAsync(); }
AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
JsonElement Json(IActionResult result) => JsonSerializer.SerializeToElement(((ObjectResult)result).Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
int Code(IActionResult r) => r is ObjectResult o ? o.StatusCode ?? 200 : r is StatusCodeResult s ? s.StatusCode : r is ForbidResult ? 403 : 200;
async Task<IActionResult> Call(Func<AccountingController, Task<IActionResult>> run, string method = "GET", int user = 1, bool allow = true)
{
    await using var db = Context();
    var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.ToString()), new Claim("RoleId", "1") }, "test")) };
    http.Request.Method = method;
    var action = new ActionContext(http, new RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
    var controller = new AccountingController(db, new TestPermissions(allow)) { ControllerContext = new ControllerContext(action) };
    var executing = new ActionExecutingContext(action, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller);
    ActionExecutedContext? executed = null;
    await controller.OnActionExecutionAsync(executing, async () => {
        executed = new ActionExecutedContext(action, new List<IFilterMetadata>(), controller);
        try { executed.Result = await run(controller); } catch (Exception ex) { executed.Exception = ex; }
        return executed;
    });
    if (executed?.Exception != null && !executed.ExceptionHandled) throw executed.Exception;
    return executing.Result ?? executed?.Result ?? throw new Exception("Missing result");
}
// Exercise MVC discovery itself: direct controller calls cannot detect startup binding errors.
var routingBuilder = WebApplication.CreateBuilder(Array.Empty<string>());
routingBuilder.Services.AddControllers().AddApplicationPart(typeof(AccountingController).Assembly);
await using (var routingApp = routingBuilder.Build())
{
    routingApp.MapControllers();
    var actions = routingApp.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items
        .OfType<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>()
        .Where(a => a.ControllerTypeInfo.AsType() == typeof(AccountingController)).ToList();
    Check(actions.Any(a => a.ActionName == nameof(AccountingController.Workspace)), "MVC discovers accounting endpoints");
    Check(actions.All(a => a.ActionName != nameof(AccountingController.OnActionExecutionAsync)), "MVC excludes the accounting authorization filter from API actions");
}
if (args.Contains("--routing-only")) return;
await Sql(master, $"CREATE DATABASE [{dbName}]");
try
{
    await Sql(connection, """
CREATE TABLE Schools(Id INT PRIMARY KEY, IsActive BIT NOT NULL);
INSERT Schools VALUES(1,1),(2,1);
CREATE TABLE Roles(Id INT PRIMARY KEY, RoleName NVARCHAR(100) NOT NULL, IsActive BIT NOT NULL);
INSERT Roles VALUES(1,'CA',1),(2,'Teacher',1);
CREATE TABLE Users(Id INT PRIMARY KEY, Name NVARCHAR(100) NOT NULL, Email NVARCHAR(100) NOT NULL, Phone NVARCHAR(100) NOT NULL, Password_Hash NVARCHAR(100) NOT NULL, RoleId INT NULL, School_Id INT NULL, Last_Login DATETIME2 NULL, Status BIT NOT NULL, Created_At DATETIME2 NOT NULL, IsActive BIT NOT NULL);
INSERT Users VALUES(1,'CA One','','','',1,1,NULL,1,GETUTCDATE(),1),(2,'CA Two','','','',1,2,NULL,1,GETUTCDATE(),1),(3,'Teacher','','','',2,1,NULL,1,GETUTCDATE(),1);
CREATE TABLE ErpModules(Id INT IDENTITY PRIMARY KEY,[Key] NVARCHAR(80),Name NVARCHAR(120),SortOrder INT,IsActive BIT);
CREATE TABLE ErpPages(Id INT IDENTITY PRIMARY KEY,ModuleId INT,[Key] NVARCHAR(100),Name NVARCHAR(120),SortOrder INT,IsActive BIT);
CREATE TABLE ErpActions(Id INT IDENTITY PRIMARY KEY,[Key] NVARCHAR(60),Name NVARCHAR(100),SortOrder INT,IsActive BIT);
CREATE TABLE Permissions(Id INT IDENTITY PRIMARY KEY,PageId INT,ActionId INT,[Key] NVARCHAR(220),IsActive BIT);
CREATE TABLE RolePermissions(Id INT IDENTITY PRIMARY KEY,RoleId INT,PermissionId INT,IsAllowed BIT,ModifiedAt DATETIME2);
CREATE TABLE InventoryStudentOrders(Id INT PRIMARY KEY,SchoolId INT);
CREATE TABLE InventoryPayments(Id INT PRIMARY KEY,StudentOrderId INT,PaymentDate DATETIME2,ReceiptNumber NVARCHAR(100),Amount DECIMAL(18,2),PaymentMode NVARCHAR(50));
INSERT InventoryStudentOrders VALUES(1,1),(2,2);
INSERT InventoryPayments VALUES(1,1,'2026-01-08','I-001',40,'Online'),(2,2,'2026-01-08','I-OTHER',50,'Online');
CREATE TABLE TransportFees(Id INT PRIMARY KEY,SchoolId INT);
CREATE TABLE TransportFeePayments(Id INT PRIMARY KEY,TransportFeeId INT,PaymentDate DATETIME2,ReceiptNumber NVARCHAR(100),Amount DECIMAL(18,2),PaymentMode NVARCHAR(50));
INSERT TransportFees VALUES(1,1),(2,2);
INSERT TransportFeePayments VALUES(1,1,'2026-01-07','T-001',30,'Online'),(2,2,'2026-01-07','T-OTHER',60,'Cash');
CREATE TABLE SalaryPayment(Id INT PRIMARY KEY,StaffId INT,schoolId INT,SalaryMonth INT,SalaryYear INT,NetSalary DECIMAL(18,2),Status NVARCHAR(20),PaymentDate DATETIME2 NULL,PaymentMethod NVARCHAR(50));
INSERT SalaryPayment VALUES(1,1,1,1,2026,20,'Paid','2026-01-06','Cash'),(2,2,2,1,2026,70,'Paid','2026-01-06','Cash');
CREATE TABLE FeePayments(Id INT PRIMARY KEY,SchoolId INT,IsActive BIT,Payment_Date DATETIME2,AmountPaid DECIMAL(18,2),Payment_Mode NVARCHAR(50),Receipt_Number NVARCHAR(100));
INSERT FeePayments VALUES(1,1,1,'2026-01-05',100,'Cash','F-001'),(2,2,1,'2026-01-05',200,'Cash','OTHER-SCHOOL');
""");
    var schema = await File.ReadAllTextAsync(Path.Combine(root, "Database/Accounting/accounting.sql"));
    await Sql(connection, schema); await Sql(connection, schema);
    Check(true, "SQL migration can run twice");
    Check(Code(await Call(c => c.Initialize(), "POST")) == 200, "Initialize school chart");
    await Call(c => c.Initialize(), "POST");
    Check(Code(await Call(c => c.Initialize(), "POST", 2)) == 200, "Initialize second school");
    Check(Code(await Call(c => c.Workspace(new(2026,1,1), new(2026,1,31)), user: 3)) == 403, "Reject non-accountant role");
    Check(Code(await Call(c => c.Workspace(new(2026,1,1), new(2026,1,31)), allow: false)) == 403, "Reject missing accounting permission");
    List<AccountingAccount> accounts;
    await using (var db = Context()) accounts = await db.AccountingAccounts.Where(a => a.SchoolId == 1).ToListAsync();
    Check(accounts.Count == 11, "Initialization does not duplicate accounts");
    int A(string code) => accounts.Single(a => a.Code == code).Id;
    var date = new DateTime(2026,1,10);
    VoucherRequest Receipt(decimal debit = 100m, decimal credit = 100m) => new(Guid.NewGuid(), date, "Receipt", "Test receipt", "BANK-01", new() { new(A("1010"),debit,0), new(A("4000"),0,credit) });
    Check(Code(await Call(c => c.CreateVoucher(Receipt(100,99)), "POST")) == 400, "Reject unbalanced voucher");
    Check(Code(await Call(c => c.CreateVoucher(Receipt(100.001m,100.001m)), "POST")) == 400, "Reject fractions smaller than a paisa");
    int foreign;
    await using (var db = Context()) foreign = await db.AccountingAccounts.Where(a => a.SchoolId == 2).Select(a => a.Id).FirstAsync();
    var cross = Receipt(); cross.Lines[0] = new(foreign,100,0);
    Check(Code(await Call(c => c.CreateVoucher(cross), "POST")) == 400, "Reject another school's account");
    var editable = Receipt();
    var editCreated = Json(await Call(c => c.CreateVoucher(editable), "POST"));
    var editId = editCreated.GetProperty("id").GetInt32();
    var editRequest = editable with { Narration = "Updated draft", Lines = new() { new(A("1010"),150,0), new(A("4000"),0,150) } };
    Check(Code(await Call(c => c.EditVoucher(editId,editRequest), "PUT"))==200,"Edit a draft with balanced replacement lines");
    await using(var db=Context()) {
        Check(await db.AccountingLines.Where(l=>l.AccountingVoucherId==editId).SumAsync(l=>l.Debit)==150m,"Draft edit replaces old lines");
        var audit = await db.AccountingAudits.Where(a=>a.Action=="Edit draft").Select(a=>a.Detail).SingleAsync();
        Check(audit.Contains("before") && audit.Contains("after") && audit.Contains("100") && audit.Contains("150"),"Audit preserves amounts before and after editing");
    }
    Check(Code(await Call(c=>c.EditVoucher(editId,editRequest),"PUT"))==409,"Reject stale draft edit");
    await Call(c=>c.Decide(editId,"void",new(1,"Discard test draft",null)),"PUT");
    var request = Receipt();
    var created = await Call(c => c.CreateVoucher(request), "POST"); Check(Code(created)==200,"Save balanced draft");
    var id = Json(created).GetProperty("id").GetInt32();
    var duplicate = await Call(c => c.CreateVoucher(request), "POST");
    Check(Json(duplicate).GetProperty("id").GetInt32() == id, "Retry request does not duplicate voucher");
    Check(Code(await Call(c => c.Decide(id,"post",new(0,"",null)), "PUT", 2)) == 404, "Another school cannot post voucher");
    Check(Code(await Call(c => c.Decide(id,"post",new(0,"",null)), "PUT")) == 200, "Post balanced voucher");
    Check(Code(await Call(c => c.EditVoucher(id,request), "PUT")) == 409, "Posted vouchers cannot be edited");
    Check(Code(await Call(c => c.Decide(id,"post",new(0,"",null)), "PUT")) == 409, "Stale revision cannot post twice");
    var workspace = Json(await Call(c => c.Workspace(new(2026,1,1),new(2026,1,31))));
    var report = workspace.GetProperty("report").EnumerateArray().ToList();
    Check(report.Sum(x=>x.GetProperty("closing").GetDecimal()) == 0m, "Trial balance remains balanced");
    var ledger = Json(await Call(c => c.Ledger(A("1010"),new(2026,1,1),new(2026,1,31))));
    Check(ledger.GetProperty("rows").GetArrayLength()==1,"Posted voucher appears in ledger");
    var line = ledger.GetProperty("rows")[0].GetProperty("id").GetInt32();
    Check(Code(await Call(c=>c.Clear(line,new(new(2026,1,9),"REF")), "PUT"))==400,"Reject bank clearance before voucher date");
    Check(Code(await Call(c=>c.Clear(line,new(new(2026,1,11),"REF")), "PUT"))==200,"Save bank clearance");
    var bank = Json(await Call(c=>c.Bank(A("1010"),new(2026,1,31))));
    Check(bank.GetProperty("reconciled").GetDecimal()==100m,"Bank reconciliation includes cleared entry");
    Check(Code(await Call(c=>c.Reconcile(new(A("1010"),new(2026,1,31),99)), "POST"))==400,"Reject mismatching bank statement");
    Check(Code(await Call(c=>c.Reconcile(new(A("1010"),new(2026,1,31),100)), "POST"))==200,"Save matching bank reconciliation");
    var sources = Json(await Call(c=>c.SourcePreview(new(2026,1,1),new(2026,1,31))));
    Check(sources.GetProperty("rows").GetArrayLength()==4,"All four source types are school scoped");
    var import = new ImportRequest(new(2026,1,1),new(2026,1,31),new(){"fee:1"},A("1010"));
    Check(Code(await Call(c=>c.Import(import),"POST"))==200,"Import receipt as draft");
    await Call(c=>c.Import(import),"POST");
    await using(var db=Context()) Check(await db.AccountingVouchers.CountAsync(v=>v.SchoolId==1 && v.SourceKey=="fee:1")==1,"Repeated imports do not duplicate source");
    var otherSources = new ImportRequest(new(2026,1,1),new(2026,1,31),new(){"salary:1","transport:1","inventory:1"},0);
    Check(Code(await Call(c=>c.Import(otherSources),"POST"))==400,"Non-cash imports require a valid bank");
    Check(Code(await Call(c=>c.Import(otherSources with { BankAccountId=A("1010") }),"POST"))==200,"Import payroll, transport and inventory drafts");
    Check(Code(await Call(c=>c.Import(import with { Keys=new(){"fee:2"} }),"POST"))==400,"Cannot import another school's source");
    Check(Code(await Call(c=>c.Lock(new(new(2026,1,31))),"PUT"))==400,"Drafts prevent period closing");
    List<int> extraDrafts;
    await using(var db=Context()) extraDrafts=await db.AccountingVouchers.Where(v=>v.SchoolId==1 && v.Status=="Draft" && v.SourceKey!="fee:1").Select(v=>v.Id).ToListAsync();
    foreach(var extra in extraDrafts) await Call(c=>c.Decide(extra,"void",new(0,"Review rejected",null)),"PUT");
    int importedId;
    await using(var db=Context()) importedId=await db.AccountingVouchers.Where(v=>v.SourceKey=="fee:1").Select(v=>v.Id).SingleAsync();
    await Call(c=>c.Decide(importedId,"void",new(0,"Test discarded import",null)),"PUT");
    Check(Code(await Call(c=>c.Decide(id,"reverse",new(1,"Correction",new(2026,1,12))),"PUT"))==200,"Reverse voucher with opposite posted entries");
    Check(Code(await Call(c=>c.Decide(id,"reverse",new(2,"Duplicate",new(2026,1,13))),"PUT"))==409,"Prevent duplicate reversal");
    ledger=Json(await Call(c=>c.Ledger(A("1010"),new(2026,1,1),new(2026,1,31))));
    Check(ledger.GetProperty("rows").EnumerateArray().Sum(l=>l.GetProperty("debit").GetDecimal()-l.GetProperty("credit").GetDecimal())==0,"Original and reversal net to zero");
    Check(Code(await Call(c=>c.Lock(new(new(2026,1,31))),"PUT"))==200,"Close period after drafts resolved");
    Check(Code(await Call(c=>c.CreateVoucher(Receipt()),"POST"))==400,"Closed period rejects new entries");
    Check(Code(await Call(c=>c.Clear(line,new(null,"")),"PUT"))==400,"Closed period protects bank clearance");
    Check(Code(await Call(c=>c.Lock(new(new(2026,1,1))),"PUT"))==400,"Cannot move lock backwards");
    var other=Json(await Call(c=>c.Workspace(new(2026,1,1),new(2026,1,31)),user:2));
    Check(other.GetProperty("vouchers").GetArrayLength()==0 && other.GetProperty("report").EnumerateArray().All(a=>a.GetProperty("closing").GetDecimal()==0),"Reports isolate other school's transactions");
    Console.WriteLine($"All {passed} accounting integration checks passed.");
}
finally
{
    SqlConnection.ClearAllPools();
    await Sql(master, $"ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}]");
    Console.WriteLine("Disposable local database removed.");
}
sealed class TestPermissions(bool allow) : IPermissionService
{
    public Task<bool> HasPermissionAsync(ClaimsPrincipal principal,string key) => Task.FromResult(allow && (key.StartsWith("finance.accounts.") || key=="finance.fees.read" || key=="finance.salary.read" || key=="management.transport.read"));
    public Task<IReadOnlyList<string>> EffectivePermissionsAsync(int userId) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}
