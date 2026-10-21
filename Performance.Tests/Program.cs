using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data.Common;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SchoolManagement.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using SchoolManagement.Data;
using SchoolManagement.Model;
using SchoolManagement.Repository;
using SchoolManagement.Service;

// Only creates and deletes its own randomly named LOCAL test database.
var database = "PerformanceTest_" + Guid.NewGuid().ToString("N");
var counter = new ReadCounter();
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=" + database + ";Integrated Security=true;TrustServerCertificate=true")
    .AddInterceptors(counter).Options;
await using var db = new AppDbContext(options);
void Check(bool passed, string name) { if (!passed) throw new Exception(name); Console.WriteLine("PASS: " + name); }
T Row<T>() where T : new() {
    var row = new T();
    foreach (var p in typeof(T).GetProperties().Where(p => p.CanWrite && p.PropertyType == typeof(string)))
        p.SetValue(row, p.Name == "GenderCode" ? "M" : "test");
    return row;
}
try {
    await db.Database.EnsureCreatedAsync();

    await db.Database.ExecuteSqlRawAsync("DROP TABLE dbo.ImportEmails");
    await ImportEmailWorker.EnsureSchemaAsync(db);
    await ImportEmailWorker.EnsureSchemaAsync(db);
    var protection = new EphemeralDataProtectionProvider();
    var mail = new TestImportMail();
    var services = new ServiceCollection();
    services.AddScoped(_ => new AppDbContext(options));
    services.AddSingleton<IDataProtectionProvider>(protection);
    services.AddSingleton<IEmailService>(mail);
    using var provider = services.BuildServiceProvider();
    var worker = new ImportEmailWorker(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ImportEmailWorker>.Instance);
    Task Deliver(long id) => (Task)typeof(ImportEmailWorker).GetMethod("DeliverAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(worker, new object[] { id, CancellationToken.None })!;
    await using (var rollback = await db.Database.BeginTransactionAsync()) {
        await new ImportEmailService(db, protection).EnqueueAsync("rollback@example.com", "Subject", "Secret password");
        await db.SaveChangesAsync();
        await rollback.RollbackAsync();
    }
    db.ChangeTracker.Clear();
    Check(await db.ImportEmails.CountAsync() == 0, "rolled-back imports leave no deliverable credential emails");
    var timer = System.Diagnostics.Stopwatch.StartNew();
    await using (var commit = await db.Database.BeginTransactionAsync()) {
        var queue = new ImportEmailService(db, protection);
        for (var i = 0; i < 1000; i++) await queue.EnqueueAsync("student@example.com", "Welcome", "Secret password");
        await db.SaveChangesAsync();
        await commit.CommitAsync();
    }
    Console.WriteLine($"BENCHMARK: queued 1,000 encrypted emails in {timer.ElapsedMilliseconds}ms without SMTP");
    Check(mail.Sent == 0 && await db.ImportEmails.CountAsync() == 1000, "bulk email enqueue is durable and performs no SMTP calls");
    var queued = await db.ImportEmails.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
    Check(!queued.ProtectedPayload.Contains("Secret password"), "queued credential contents are encrypted");
    mail.Fail = true;
    await Deliver(queued.Id);
    var failedMail = await db.ImportEmails.AsNoTracking().SingleAsync(x => x.Id == queued.Id);
    Check(failedMail.Attempts == 1 && failedMail.NextAttemptAt > DateTime.UtcNow && failedMail.LockedUntil == null, "SMTP failure retains the queue item and schedules a retry");
    mail.Fail = false;
    await Deliver(queued.Id);
    Check(mail.Sent == 0, "scheduled retry is not claimed before its due time");
    await db.ImportEmails.Where(x => x.Id == queued.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1)));
    await Deliver(queued.Id);
    Check(mail.Sent == 1 && !await db.ImportEmails.AnyAsync(x => x.Id == queued.Id), "successful delivery decrypts credentials and removes the queued secret");
    await db.ImportEmails.ExecuteDeleteAsync();
    db.ChangeTracker.Clear();

    if (args.Contains("--import-email-only")) return;
    var school = Row<Schools>(); school.IsActive = true; db.Schools.Add(school);
    var role = Row<Roles>(); db.Roles.Add(role);
    await db.SaveChangesAsync();
    var students = Enumerable.Range(1, 5000).Select(i => { var r = Row<Students>(); r.StudentName = "Student " + i; r.SchoolId = school.Id; r.IsActive = true; return r; }).ToList();
    var staff = Enumerable.Range(1, 5000).Select(i => { var r = Row<Staff>(); r.SchoolId = school.Id; r.RoleId = role.Id; r.IsActive = true; return r; }).ToList();
    db.Students.AddRange(students); db.Staff.AddRange(staff); await db.SaveChangesAsync();
    foreach (var student in students) { var d = Row<Student_Documents>(); d.StudentId = student.Id; db.Student_Documents.Add(d); }
    foreach (var person in staff) { var d = Row<StaffDocument>(); d.StaffId = person.Id; db.StaffDocuments.Add(d); }
    var user = Row<Users>(); user.RoleId = 20; db.Users.Add(user);
    db.Permissions.AddRange(new Permission { Key = "role" }, new Permission { Key = "direct" }, new Permission { Key = "override" }, new Permission { Key = "inactive", IsActive = false });
    await db.SaveChangesAsync();
    var permissions = await db.Permissions.OrderBy(x => x.Id).ToListAsync();
    db.RolePermissions.Add(new RolePermission { RoleId = 20, PermissionId = permissions[0].Id, IsAllowed = true });
    db.EmployeePermissions.Add(new EmployeePermission { UserId = user.Id, PermissionId = permissions[1].Id, IsAllowed = true });
    db.PermissionOverrides.AddRange(new PermissionOverride { UserId = user.Id, PermissionId = permissions[2].Id, IsAllowed = true }, new PermissionOverride { UserId = user.Id, PermissionId = permissions[0].Id, IsAllowed = false });
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();
    var studentRepo = new StudentRepository(db, null!, null!, null!, null!, null!, null!);
    counter.Reads = 0;
    var first = await studentRepo.GetStudentsBySchoolIdAsync(school.Id, 1, 10);
    Check(first.TotalRecords == 5000 && first.Data.Count == 10 && first.Data.All(x => x.Documents.Count == 1), "student page and documents");
    Check(counter.Reads == 4, "student query count stays at four per page");
    var second = await studentRepo.GetStudentsBySchoolIdAsync(school.Id, 2, 10);
    Check(!first.Data.Select(x => x.Id).Intersect(second.Data.Select(x => x.Id)).Any(), "student pages do not overlap");
    var ctor = typeof(AdminRepository).GetConstructors().Single();
    var constructorArgs = ctor.GetParameters().Select(p => p.ParameterType == typeof(AppDbContext) ? (object)db : null).ToArray();
    var admin = (AdminRepository)ctor.Invoke(constructorArgs);
    counter.Reads = 0;
    var staffPage = await admin.GetStaffFullAsync(school.Id, 1, 10);
    Check(staffPage.TotalRecords == 5000 && staffPage.Data.Count == 10 && staffPage.Data.All(x => x.Documents.Count == 1), "staff page and documents");
    Check(counter.Reads == 3, "staff query count stays at three per page");
    counter.Reads = 0;
    var dashboard = await admin.GetDashboardData(school.Id);
    Check(dashboard.Data!.TotalEmployees == 5000 && counter.Reads == 1, "dashboard counters use one query");
    counter.Reads = 0;
    var service = new PermissionService(db, new ConfigurationBuilder().Build());
    var effective = await service.EffectivePermissionsAsync(user.Id);
    Check(effective.OrderBy(x => x).SequenceEqual(new[] { "direct", "override", "role" }), "additive role, direct and override permissions preserved");
    Check(counter.Reads == 1, "permissions use one SQL query");
    var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("RoleId", "20") }, "test"));
    counter.Reads = 0;
    Check(await service.HasPermissionAsync(principal, "role") && await service.HasPermissionAsync(principal, "direct"), "multiple permission checks succeed");
    Check(counter.Reads == 1, "multiple permission checks share one request query");

    var session = new AcademicSessions { SchoolId = school.Id, Year_Start = DateTime.Today.AddMonths(-6), Year_End = DateTime.Today.AddMonths(6) };
    db.AcademicSessions.Add(session);
    var classes = Enumerable.Range(1, 100).Select(i => new Classes { SchoolId = school.Id, ClassName = "Class " + i, IsActive = true }).ToList();
    db.Classes.AddRange(classes);
    var subjects = Enumerable.Range(1, 10).Select(i => new Subjects { SchoolId = school.Id, SubjectName = "Subject " + i, IsActive = true }).ToList();
    db.Subjects.AddRange(subjects); await db.SaveChangesAsync();
    var sections = classes.Select(c => new SectionDetails { ClassId = c.Id, SchoolId = school.Id, SectionName = "A", IsActive = true }).ToList();
    db.SectionDetails.AddRange(sections); await db.SaveChangesAsync();
    foreach (var section in sections) foreach (var subject in subjects) db.SectionSubjects.Add(new SectionSubjects { SchoolId = school.Id, SectionId = section.Id, SubjectId = subject.Id, IsActive = true });
    var enrollments = students.Select((student, i) => new StudentEnrollment { StudentId = student.Id, SchoolId = school.Id, ClassId = sections[i % sections.Count].ClassId, SectionId = sections[i % sections.Count].Id, SessionId = session.Id }).ToList();
    db.StudentEnrollment.AddRange(enrollments);
    db.StaffSalaryStructure.AddRange(staff.Select(person => new StaffSalaryStructure { StaffId = person.Id, schoolId = school.Id, BasicSalary = 10000, IsActive = true }));
    await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    counter.Reads = 0;
    var classPage = await new ClassRepository(db).GetClassDetailsPagedAsync(school.Id, 1, 10);
    Check(classPage.TotalRecords == 100 && classPage.Data.Count == 10 && classPage.Data.All(c => c.Sections.Count == 1 && c.Sections[0].Subjects.Count == 10), "class page contains complete section and subject batches");
    Check(counter.Reads == 4, "class detail query count is independent of section and subject count");
    counter.Reads = 0;
    var payroll = new StaffRepository(db, null!, null!, null!, null!, null!);
    await payroll.GenerateMonthlySalary(10, 2026, school.Id);
    Check(counter.Reads == 2, "5,000 payroll records use two lookups rather than one per employee");
    Check(await db.SalaryPayment.CountAsync() == 5000, "salary generation creates one payment per employee");
    await payroll.GenerateMonthlySalary(10, 2026, school.Id);
    Check(await db.SalaryPayment.CountAsync() == 5000, "salary generation skips already generated payments");
    var vehicleType = new VehicleType { SchoolId = school.Id, VehicleTypeName = "Bus", DefaultCapacity = 40 };
    db.VehicleTypes.Add(vehicleType); await db.SaveChangesAsync();
    db.TransportVehicles.AddRange(Enumerable.Range(1, 500).Select(i => new TransportVehicle { SchoolId = school.Id, VehicleTypeId = vehicleType.Id, VehicleName = "Bus " + i, VehicleNumber = i.ToString(), Capacity = 40 }));
    await db.SaveChangesAsync();
    var transport = new TransportController(db, null!, null!);
    JsonElement Body(IActionResult result) => JsonSerializer.SerializeToElement(((ObjectResult)result).Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    counter.Reads = 0;
    var vehiclePage = Body(await transport.Vehicles(school.Id, 1, 50)).GetProperty("data");
    var vehiclePage2 = Body(await transport.Vehicles(school.Id, 2, 50)).GetProperty("data");
    Check(vehiclePage.GetArrayLength() == 50 && vehiclePage2.GetArrayLength() == 50 && counter.Reads == 2, "transport pages are bounded SQL queries");
    Check(!vehiclePage.EnumerateArray().Select(x => x.GetProperty("id").GetInt32()).Intersect(vehiclePage2.EnumerateArray().Select(x => x.GetProperty("id").GetInt32())).Any(), "transport pages do not overlap");
    var exam = new Exams { SchoolId = school.Id, Name = "Exam", IsActive = true, ResultPublished = true, CreatedDate = DateTime.UtcNow };
    db.Exams.Add(exam); await db.SaveChangesAsync();
    db.ExamSchedules.AddRange(Enumerable.Range(0, 100).Select(i => new ExamSchedules { SchoolId = school.Id, ExamId = exam.Id, ClassId = classes[0].Id, SectionId = sections[0].Id, SubjectId = subjects[0].Id, ExamDate = DateTime.Today.AddDays(i), Status = "Scheduled", IsActive = true }));
    await db.SaveChangesAsync();
    counter.Reads = 0;
    var examFeed = Body(await new AdminController(null!, db).DashboardExams(school.Id)).GetProperty("data");
    Check(examFeed.GetProperty("events").GetArrayLength() == 5 && counter.Reads == 2, "dashboard exam feed remains bounded with many schedules");
    var uniqueEmail = "portal-test@example.com";
    await db.Students.Where(x => x.Id == students[0].Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Email, uniqueEmail));
    var credential = Row<Students_Parents_Creds>(); credential.Email = uniqueEmail; credential.School_Id = school.Id; credential.RoleName = "Student"; credential.Status = "Active";
    db.Students_Parents_Creds.Add(credential); await db.SaveChangesAsync();
    var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, credential.Id.ToString()), new Claim(ClaimTypes.Role, "Student") }, "test")) };
    var portal = new StudentSelfServiceController(db, null!, null!) { ControllerContext = new ControllerContext { HttpContext = http } };
    counter.Reads = 0;
    var documentSection = Body(await portal.Overview("documents")).GetProperty("data");
    var sectionReads = counter.Reads;
    Check(documentSection.EnumerateObject().Count() == 2 && documentSection.GetProperty("documents").GetArrayLength() == 1 && sectionReads == 5, "portal fetches only profile and requested documents");
    counter.Reads = 0;
    var legacyPortal = Body(await portal.Overview()).GetProperty("data");
    Check(legacyPortal.TryGetProperty("fees", out _) && legacyPortal.TryGetProperty("timetable", out _) && counter.Reads > sectionReads, "legacy portal response remains compatible");
    var type = Row<FeeType>(); type.SchoolId = school.Id; db.FeeTypes.Add(type); await db.SaveChangesAsync();
    var fee = new StudentFee { StudentId = students[0].Id, EnrollmentId = enrollments[0].Id, FeeTypeId = type.Id, SchoolId = school.Id, SessionId = session.Id, Amount = 100, Status = "Pending", IsActive = true };
    db.StudentFees.Add(fee); await db.SaveChangesAsync();
    var payment = Row<FeePayments>(); payment.StudentFeeId = fee.Id; payment.SchoolId = school.Id; payment.AmountPaid = 25; payment.IsActive = true;
    db.FeePayments.Add(payment); await db.SaveChangesAsync();
    var pendingFees = await studentRepo.GetPendingFeesAsync(school.Id, classes[0].Id, sections[0].Id, session.Id, true);
    var feeRow = JsonSerializer.SerializeToElement(pendingFees.First());
    Check(feeRow.GetProperty("Paid").GetDecimal() == 25 && feeRow.GetProperty("Balance").GetDecimal() == 75, "fee aggregate preserves payment and outstanding amounts");
    var teacherRole = Row<Roles>(); teacherRole.RoleName = "Teacher"; teacherRole.IsActive = true;
    var principalRole = Row<Roles>(); principalRole.RoleName = "Principal"; principalRole.IsActive = true;
    var caRole = Row<Roles>(); caRole.RoleName = "Accountant"; caRole.IsActive = true;
    db.Roles.AddRange(teacherRole, principalRole, caRole); await db.SaveChangesAsync();
    var teacherUser = Row<Users>(); teacherUser.RoleId = teacherRole.Id; teacherUser.School_Id = school.Id; teacherUser.IsActive = true;
    var principalUser = Row<Users>(); principalUser.RoleId = principalRole.Id; principalUser.School_Id = school.Id; principalUser.IsActive = true;
    var caUser = Row<Users>(); caUser.RoleId = caRole.Id; caUser.School_Id = school.Id; caUser.IsActive = true;
    db.Users.AddRange(teacherUser, principalUser, caUser); await db.SaveChangesAsync();
    await db.Staff.Where(x => x.Id == staff[0].Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.usersid, teacherUser.Id));
    await db.SectionDetails.Where(x => x.Id == sections[0].Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.StaffId, staff[0].Id));
    db.SectionSubjectTeachers.Add(new SectionSubjectTeachers { SchoolId = school.Id, SectionId = sections[0].Id, SubjectId = subjects[0].Id, StaffId = staff[0].Id });
    db.SyllabusProgress.AddRange(Enumerable.Range(1, 1000).Select(i => new SyllabusProgress { SchoolId = school.Id, SessionId = session.Id, SectionId = sections[0].Id, SubjectId = subjects[0].Id, StaffId = staff[0].Id, ProgressDate = DateTime.Today.AddDays(-i), TotalChapters = 1000, PlannedChapters = 1000, CompletedChapters = i }));
    await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    ControllerContext Caller(Users account) => new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()), new Claim("RoleId", account.RoleId!.Value.ToString()) }, "test")) } };
    var syllabus = new SyllabusController(db) { ControllerContext = Caller(teacherUser) };
    var syllabusRows = Body(await syllabus.Teacher(DateTime.Today)).GetProperty("rows");
    Check(syllabusRows.GetArrayLength() == 1 && syllabusRows[0].GetProperty("completedChapters").GetInt32() == 1, "syllabus SQL selects the latest snapshot from 1,000 history records");
    var teacher = new TeacherController(db, new AllowPermissions(), studentRepo) { ControllerContext = Caller(teacherUser) };
    var teacherBody = Body(await teacher.Workspace(DateTime.Today));
    Check(teacherBody.GetProperty("classes").GetArrayLength() == 1, "teacher workspace preserves assigned classes and sections");
    var principalDashboard = new PrincipalController(db, new AllowPermissions()) { ControllerContext = Caller(principalUser) };
    var principalBody = Body(await principalDashboard.Dashboard(DateTime.Today));
    Check(principalBody.GetProperty("students").GetProperty("total").GetInt32() == 5000, "principal attendance overview counts 5,000 enrollments");
    var ca = new CaController(db, new AllowPermissions()) { ControllerContext = Caller(caUser) };
    var payrollSummary = Body(await ca.Dashboard(new DateTime(2026, 10, 1))).GetProperty("payroll");
    Check(payrollSummary.GetProperty("count").GetInt32() == 5000 && payrollSummary.GetProperty("total").GetDecimal() == 50000000m, "accountant payroll summary aggregates 5,000 salaries correctly");
    await db.Students.Where(x => x.Id == students[0].Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.StudentName, "Ayesha First"));
    await db.Students.Where(x => x.Id == students[1].Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.StudentName, "Ayesha Second"));
    await db.Staff.Where(x => x.Id == staff[0].Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Name, "Priya Teacher").SetProperty(x => x.RoleId, teacherRole.Id));
    await db.Staff.Where(x => x.Id == staff[1].Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Name, "Priya Principal").SetProperty(x => x.RoleId, principalRole.Id));
    await db.Staff.Where(x => x.Id == staff[2].Id).ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Name, "Priya Admin"));
    var otherSchool = Row<Schools>(); db.Schools.Add(otherSchool); await db.SaveChangesAsync();
    var otherStudent = Row<Students>(); otherStudent.SchoolId = otherSchool.Id; otherStudent.StudentName = "Ayesha Other School"; otherStudent.IsActive = true;
    var otherStaff = Row<Staff>(); otherStaff.SchoolId = otherSchool.Id; otherStaff.RoleId = role.Id; otherStaff.Name = "Priya Other School";
    db.Students.Add(otherStudent); db.Staff.Add(otherStaff); await db.SaveChangesAsync();
    var studentSearch = await studentRepo.GetStudentsBySchoolIdAsync(school.Id, 1, 1, " ayesha ");
    var nextStudentSearch = await studentRepo.GetStudentsBySchoolIdAsync(school.Id, 2, 1, "ayesha");
    Check(studentSearch.TotalRecords == 2 && studentSearch.Data.Count == 1 && nextStudentSearch.Data.Count == 1 && studentSearch.Data[0].Id != nextStudentSearch.Data[0].Id, "student partial-name search is trimmed, case insensitive, paginated and school scoped");
    var staffSearch = await admin.GetStaffFullAsync(school.Id, 1, 2, search: " priya ");
    var nextStaffSearch = await admin.GetStaffFullAsync(school.Id, 2, 2, search: "priya");
    Check(staffSearch.TotalRecords == 3 && staffSearch.Data.Count == 2 && nextStaffSearch.Data.Count == 1, "staff name search includes admin, teacher and principal records within one school");
    var unmatchedSearch = await studentRepo.GetStudentsBySchoolIdAsync(school.Id, 1, 10, "name-not-present");
    Check(unmatchedSearch.TotalRecords == 0 && unmatchedSearch.Data.Count == 0, "unmatched student search returns an empty page");
    var principalSearch = Body(await principalDashboard.SearchStudents("Ayesha", 1, 1));
    Check(principalSearch.GetProperty("totalRecords").GetInt32() == 2 && principalSearch.GetProperty("data").GetArrayLength() == 1, "principal searches students within their own school with pagination");
    var principalUnfiltered = Body(await principalDashboard.SearchStudents("", 1, 20));
    Check(principalUnfiltered.GetProperty("totalRecords").GetInt32() == 5000 && principalUnfiltered.GetProperty("data").GetArrayLength() == 20, "empty principal search restores all students with bounded pagination");
    var invalidPrincipal = new PrincipalController(db, new AllowPermissions()) { ControllerContext = Caller(teacherUser) };
    Check(await invalidPrincipal.SearchStudents("Ayesha") is ForbidResult, "teacher cannot use principal student search");
    // Separate connections simulate API processes importing siblings simultaneously.
    await using (var firstImport = new AppDbContext(options))
    await using (var secondImport = new AppDbContext(options))
    {
        await using var firstTransaction = await firstImport.Database.BeginTransactionAsync();
        await using var secondTransaction = await secondImport.Database.BeginTransactionAsync();
        await StudentLoginEmailLock.AcquireAsync(firstImport, " Parent@Example.com ", "first@example.com");
        var waiting = StudentLoginEmailLock.AcquireAsync(secondImport, "second@example.com", "parent@example.com");
        await Task.Delay(200);
        Check(!waiting.IsCompleted, "concurrent sibling imports wait for the shared parent email across connections");
        await firstTransaction.CommitAsync();
        await waiting.WaitAsync(TimeSpan.FromSeconds(5));
        await secondTransaction.CommitAsync();
        Check(true, "email lock releases on commit and normalizes whitespace/case");
        await using var retryTransaction = await firstImport.Database.BeginTransactionAsync();
        await StudentLoginEmailLock.AcquireAsync(firstImport, "parent@example.com", "PARENT@example.com");
        await retryTransaction.RollbackAsync();
        await using var afterRollback = await secondImport.Database.BeginTransactionAsync();
        await StudentLoginEmailLock.AcquireAsync(secondImport, "parent@example.com");
        await afterRollback.CommitAsync();
        Check(true, "email lock handles duplicate keys and releases on rollback");
    }
    var indexSql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "../../../../SchoolManagement/Database/Performance/list-query-indexes.sql"));
    await db.Database.ExecuteSqlRawAsync(indexSql);
    await db.Database.ExecuteSqlRawAsync(indexSql);
    var applicationSql = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "../../../../SchoolManagement/Database/Performance/application-query-indexes.sql"));
    await db.Database.ExecuteSqlRawAsync(applicationSql);
    await db.Database.ExecuteSqlRawAsync(applicationSql);
    var indexNames = Regex.Matches(applicationSql, @"CREATE NONCLUSTERED INDEX \[(.*?)\]").Select(m => m.Groups[1].Value).ToList();
    await using var indexCommand = db.Database.GetDbConnection().CreateCommand();
    await db.Database.OpenConnectionAsync();
    indexCommand.CommandText = "SELECT name FROM sys.indexes WHERE name LIKE 'IX_Perf_%'";
    var actualIndexes = new HashSet<string>();
    await using (var reader = await indexCommand.ExecuteReaderAsync()) while (await reader.ReadAsync()) actualIndexes.Add(reader.GetString(0));
    Check(indexNames.All(actualIndexes.Contains), "all application-wide indexes exist: missing " + string.Join(", ", indexNames.Except(actualIndexes)));
    Check(true, "both index scripts apply and are rerunnable");
} finally { await db.Database.EnsureDeletedAsync(); }

sealed class ReadCounter : DbCommandInterceptor {
    public int Reads;
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
        if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)) Reads++;
        return ValueTask.FromResult(result);
    }
}


sealed class AllowPermissions : IPermissionService {
    public Task<bool> HasPermissionAsync(ClaimsPrincipal principal, string key) => Task.FromResult(true);
    public Task<IReadOnlyList<string>> EffectivePermissionsAsync(int userId) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}

sealed class TestImportMail : IEmailService {
    public bool Fail;
    public int Sent;
    public string LoginUrl => "http://localhost/login";
    public Task SendEmailAsync(string to, string subject, string body) {
        if (Fail) throw new InvalidOperationException("Test SMTP unavailable");
        if (body != "Secret password") throw new Exception("Decryption failed");
        Sent++; return Task.CompletedTask;
    }
    public Task SendEmailAsync(string to, string subject, string body, IReadOnlyCollection<EmailAttachment> attachments) => SendEmailAsync(to, subject, body);
    public Task SendEmailAsync(IReadOnlyCollection<string> recipients, string subject, string body, IReadOnlyCollection<EmailAttachment> attachments) => SendEmailAsync(recipients.First(), subject, body);
    public Task<(string subject, string body)> GetEmailTemplateAsync(string name, Dictionary<string,string> values) => Task.FromResult(("Subject", "Secret password"));
}
