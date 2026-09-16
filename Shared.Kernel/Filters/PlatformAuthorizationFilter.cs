using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Kernel.Responses;

namespace Shared.Kernel.Filters;

/// <summary>
/// Platform authorization filter.
/// Originally enforced strict x-workspace presence. Now acts as a pass-through 
/// leaving the enforcement to specific business logic per user request.
/// </summary>
public class PlatformAuthorizationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Headers are now completely optional at the gateway/filter level.
        // If a specific backend requires them, it will throw an error itself.
        await next();
    }
}
