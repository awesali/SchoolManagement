// Backend section: application services and shared rules.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SchoolManagement.Service;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
// Implements require permission application behavior.
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    // Creates the component with its required dependencies.
    public RequirePermissionAttribute(string key)
        : base(typeof(RequirePermissionFilter)) => Arguments = new object[] { key };
}

public sealed class RequirePermissionFilter : IAsyncAuthorizationFilter
{
    // Dependencies and state used by this component.
    private readonly string _key;
    private readonly IPermissionService _permissions;

    public RequirePermissionFilter(string key, IPermissionService permissions)
    {
        _key = key;
        _permissions = permissions;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!await _permissions.HasPermissionAsync(context.HttpContext.User, _key))
            context.Result = new ObjectResult(
                new
                {
                    success = false,
                    message = "You do not have permission to perform this action.",
                    permission = _key,
                }
            )
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
    }
}
