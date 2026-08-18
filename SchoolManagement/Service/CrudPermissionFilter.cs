using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace SchoolManagement.Service;

/// <summary>
/// Applies CRUD permissions to every authenticated business API. The route map
/// is deliberately centralised so an endpoint cannot silently forget an
/// authorization attribute. Unmapped authenticated endpoints are denied for
/// non-super-admin users (secure by default).
/// </summary>
public sealed class CrudPermissionFilter : IAsyncAuthorizationFilter
{
    private readonly IPermissionService _permissions;
    public CrudPermissionFilter(IPermissionService permissions) => _permissions = permissions;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var descriptor = context.ActionDescriptor as ControllerActionDescriptor;
        if (descriptor == null || descriptor.ControllerName is "Auth" or "StudentParentAuth" or "Permissions") return;
        if (descriptor.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()) return;

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new { success = false, message = "Authentication is required." });
            return;
        }
        if (user.FindFirstValue("RoleId") == "1") return;

        var page = ResolvePage(context.HttpContext.Request.Path.Value ?? "");
        var action = ResolveAction(context.HttpContext.Request.Method, context.HttpContext.Request.Path.Value ?? "");
        var key = page == null ? null : $"{page}.{action}";
        if (key == null || !await _permissions.HasPermissionAsync(user, key))
            context.Result = new ObjectResult(new { success = false, message = "You do not have permission to perform this action.", permission = key ?? "unmapped-endpoint" }) { StatusCode = StatusCodes.Status403Forbidden };
    }

    private static string ResolveAction(string method, string path)
    {
        path = path.ToLowerInvariant();
        if (method == HttpMethods.Get) return "read";
        if (method == HttpMethods.Delete) return "delete";
        if (method is "PUT" or "PATCH") return "update";
        if (path.Contains("attendance") || path.Contains("payfee") || path.Contains("assignstudentfees") ||
            path.Contains("/promote") || path.Contains("publish") || path.Contains("generate") ||
            path.Contains("savemarks") || path.Contains("lockmarks") || path.Contains("/issue")) return "update";
        return "create";
    }

    private static string? ResolvePage(string rawPath)
    {
        var p = rawPath.ToLowerInvariant();
        if (p.Contains("student-promotion")) return p.Contains("history") || p.Contains("passed-out") ? "academics.promotion-history" : "academics.student-promotion";
        if (p.Contains("attendance")) return p.Contains("student") ? "attendance.students" : "attendance.staff";
        if (p.Contains("fee") || p.Contains("receipt")) return "finance.fees";
        if (p.Contains("salary") || p.Contains("/staff/assign") || p.Contains("/staff/generate") || p.Contains("/staff/pay") || p.Contains("/staff/history") || p.Contains("/staff/pending")) return "finance.salary";
        if (p.Contains("/api/student")) return "management.students";
        if (p.Contains("add-staff") || p.Contains("update-staff") || p.Contains("staff-by-school") || p.Contains("staff-emails") || p.Contains("delete-document") || p.Contains("get-roles")) return "management.staff";
        if (p.Contains("parent")) return "management.parents";
        if (p.Contains("academic-session") || p.Contains("create-session")) return "academics.sessions";
        if (p.Contains("/api/class")) return "academics.classes";
        if (p.Contains("/api/subject")) return "academics.subjects";
        if (p.Contains("/api/timetable")) return "academics.class-schedule";
        if (p.Contains("/api/exam")) return "exams.academic-exam";
        if (p.Contains("/api/transport")) return "management.transport";
        if (p.Contains("/api/inventory")) return "management.inventory";
        if (p.Contains("school-by-superadmin") || p.Contains("/api/admin/create")) return "management.schools";
        if (p.Contains("dashboardcard")) return "dashboard.dashboard";
        if (p.Contains("/api/common/subjects")) return "academics.subjects";
        if (p.Contains("/api/common/by-school")) return "academics.classes";
        return null;
    }
}
