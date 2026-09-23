using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HPK.Core.Utils.Responses;
using System.Threading.Tasks;

namespace HPK.Core.Filters;

/// <summary>
/// Intercepts successful API responses and wraps them in a standardized <see cref="ApiResponse{T}"/> format.
/// Automatically formats the output as application/json.
/// </summary>
public class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Enforce application/json for all responses
        context.HttpContext.Response.ContentType = "application/json";

        if (context.Result is ObjectResult objectResult)
        {
            // If the controller already returned an ApiResponse (e.g., custom error logic), don't double-wrap
            if (objectResult.Value != null && objectResult.Value.GetType().IsGenericType &&
                objectResult.Value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>))
            {
                await next();
                return;
            }

            if (objectResult.Value is ApiResponse)
            {
                await next();
                return;
            }

            // Detect if a paginated object (e.g. PagedResult<T>) is returned to extract Meta automatically
            object? metaData = null;
            object? payloadData = objectResult.Value;

            if (objectResult.Value != null)
            {
                var valueType = objectResult.Value.GetType();
                if (valueType.IsGenericType && (valueType.GetGenericTypeDefinition().Name.Contains("Paged") || valueType.GetGenericTypeDefinition().Name.Contains("Pagination")))
                {
                    // If it's a Paged collection, extract Data and Meta (TotalCount, etc)
                    var countProp = valueType.GetProperty("TotalCount") ?? valueType.GetProperty("Count");
                    var dataProp = valueType.GetProperty("Data") ?? valueType.GetProperty("Items");
                    
                    if (countProp != null && dataProp != null)
                    {
                        var totalCount = countProp.GetValue(objectResult.Value);
                        payloadData = dataProp.GetValue(objectResult.Value);
                        metaData = new { totalCount };
                    }
                }
            }

            // Apply Dynamic Select Shaping if ?select is provided
            if (context.HttpContext.Request.Query.TryGetValue("select", out var selectValues))
            {
                string selectString = selectValues.ToString();
                if (!string.IsNullOrWhiteSpace(selectString) && payloadData != null)
                {
                    payloadData = HPK.Core.JsonApi.JsonApiExtensions.ShapeData(payloadData, selectString);
                }
            }

            var statusCode = objectResult.StatusCode ?? context.HttpContext.Response.StatusCode;
            if (statusCode == 0) statusCode = 200;
            
            bool isSuccess = statusCode >= 200 && statusCode < 300;
            string defaultMessage = isSuccess ? "Operation completed successfully" : "An error occurred";
            
            // Try to extract a better error message if this is a ProblemDetails object
            if (!isSuccess && payloadData is Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails)
            {
                defaultMessage = problemDetails.Title ?? "Validation failed";
            }

            // Wrap in our standard generic response
            var responseType = typeof(ApiResponse<>).MakeGenericType(payloadData?.GetType() ?? typeof(object));
            var wrappedResponse = Activator.CreateInstance(responseType, new object?[] 
            {
                isSuccess,                             // success flag
                statusCode,                            // code
                defaultMessage,                        // message
                payloadData,                           // data
                metaData,                              // meta
                null                                   // exception
            });

            context.Result = new ObjectResult(wrappedResponse)
            {
                StatusCode = statusCode,
                ContentTypes = new Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection { "application/json" }
            };
        }
        else if (context.Result is EmptyResult || (context.Result is StatusCodeResult statusCodeResult && statusCodeResult.StatusCode >= 200 && statusCodeResult.StatusCode < 300))
        {
            var code = context.Result is StatusCodeResult sc ? sc.StatusCode : 200;
            var emptyResponse = ApiResponse.Ok();
            emptyResponse.Code = code;
            
            context.Result = new ObjectResult(emptyResponse)
            {
                StatusCode = code,
                ContentTypes = new Microsoft.AspNetCore.Mvc.Formatters.MediaTypeCollection { "application/json" }
            };
        }

        await next();
    }
}
