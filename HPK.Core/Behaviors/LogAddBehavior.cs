using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Http;
using HPK.Core.Events;
using HPK.Core.Interfaces;
using HPK.Core.Utils.Enums;

namespace HPK.Core.Behaviors;

public class LogAddBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogAddBehavior(IPublishEndpoint publishEndpoint, IHttpContextAccessor httpContextAccessor)
    {
        _publishEndpoint = publishEndpoint;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Execute the handler first
        var response = await next();

        // If request implements ILoggableRequest, publish log event
        if (request is ILoggableRequest loggable)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            Guid? GetHeaderGuid(string headerName)
            {
                if (httpContext?.Request.Headers.TryGetValue(headerName, out var val) == true && Guid.TryParse(val.ToString(), out var guid))
                    return guid;
                return null;
            }

            var logEvent = new LogAddEvent
            {
                Event = "log:add",
                Priority = 1,
                Data = new LogAddEventData
                {
                    RequestId = GetHeaderGuid("x-request-id"),
                    QueueId = GetHeaderGuid("x-queue-id"),
                    WorkspaceId = GetHeaderGuid("x-workspace"),
                    AccountId = GetHeaderGuid("x-account"),
                    TraceId = GetHeaderGuid("x-trace-id"),
                    Service = loggable.LogService.ToString(),
                    Table = loggable.LogTable,
                    Model = loggable.LogModel,
                    Type = loggable.LogType.ToLowerString(),
                    Meta = null // Can be populated if needed
                }
            };

            await _publishEndpoint.Publish(logEvent, cancellationToken);
        }

        return response;
    }
}
