using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SchoolManagement.Service;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string key) : base(typeof(RequirePermissionFilter)) => Arguments = new object[] { key };
}

public sealed class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    private readonly string _key;
    private readonly IPermissionService _permissions;
    public RequirePermissionFilter(string key, IPermissionService permissions) { _key = key; _permissions = permissions; }
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!await _permissions.HasPermissionAsync(context.HttpContext.User, _key))
            context.Result = new ObjectResult(new { success = false, message = "You do not have permission to perform this action.", permission = _key }) { StatusCode = StatusCodes.Status403Forbidden };
    }
}
