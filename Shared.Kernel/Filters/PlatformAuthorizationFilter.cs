using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Kernel.Responses;

namespace Shared.Kernel.Filters;

/// <summary>
/// Enforces the universal rule:
/// - If x-platform != true, x-workspace header MUST be present and valid GUID.
/// - If x-platform != true, the tenant is ONLY allowed to operate on their own workspace.
///   (This filter only ensures the x-workspace header exists. The specific controllers or MediatR commands 
///   are responsible for asserting that the requested resource belongs to that x-workspace if x-platform != true).
/// </summary>
public class PlatformAuthorizationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;

        // Skip internal or docs endpoints
        if (request.Path.Value != null && (request.Path.Value.Contains("/internal/") || request.Path.Value.Contains("/docs") || request.Path.Value.Contains("/openapi")))
        {
            await next();
            return;
        }

        var platformHeader = request.Headers["x-platform"].ToString();
        var isPlatform = platformHeader.Equals("true", StringComparison.OrdinalIgnoreCase);

        if (!isPlatform)
        {
            var workspaceHeader = request.Headers["x-workspace"].ToString();
            
            if (string.IsNullOrWhiteSpace(workspaceHeader) || !Guid.TryParse(workspaceHeader, out _))
            {
                var response = ApiResponse<object>.Error(403, "Forbidden: Missing or invalid x-workspace header for tenant request.");
                context.Result = new ObjectResult(response) { StatusCode = 403 };
                return;
            }
        }

        await next();
    }
}
