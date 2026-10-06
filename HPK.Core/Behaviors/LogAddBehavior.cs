using System.Linq;
using System;
using System.Collections.Generic;
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
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LogAddBehavior(ISendEndpointProvider sendEndpointProvider, IHttpContextAccessor httpContextAccessor)
    {
        _sendEndpointProvider = sendEndpointProvider;
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
            
            // Seed Logging Exemption: If there is no HTTP Context (e.g. running via CLI or Startup Seeding), DO NOT generate action logs
            if (httpContext == null)
            {
                return response;
            }
            
            Guid? GetHeaderGuid(string headerName)
            {
                if (httpContext?.Request.Headers.TryGetValue(headerName, out var val) == true && Guid.TryParse(val.ToString(), out var guid))
                    return guid;
                return null;
            }

            // Capture request and response for Meta
            var metaData = new Dictionary<string, object>();
            metaData["request"] = request;
            if (response != null)
            {
                
                var responseType = response.GetType();
                if (!responseType.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>)) && 
                    !typeof(System.IO.Stream).IsAssignableFrom(responseType))
                {
                    metaData["response"] = response;
                }
                else
                {
                    metaData["response"] = "[Unserializable Response Type]";
                }

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
                    Service = loggable.LogService.ToLowerString(),
                    Table = loggable.LogTable,
                    Model = loggable.LogModel.ToLowerInvariant(),
                    Type = loggable.LogType.ToLowerString(),
                    Meta = metaData
                }
            };

            var queueName = Environment.GetEnvironmentVariable("ACTION_SERVICE_QUEUE") ?? "icot_action_queue";
            var sendEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{queueName}"));
            await sendEndpoint.Send(logEvent, cancellationToken);
        }

        return response;
    }
}
