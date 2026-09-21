using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using DotNetEnv;
using System;
using System.IO;
using System.Linq;

namespace HPK.Core.Extensions;

public static class PlatformApiExtensions
{
    public static WebApplicationBuilder ConfigurePlatformHost(this WebApplicationBuilder builder, string serviceName)
    {
        LoadEnvFile();

        var mode = Environment.GetEnvironmentVariable("MODE") ?? Env.GetString("MODE") ?? "development";
        bool isProduction = string.Equals(mode.Trim(), "production", StringComparison.OrdinalIgnoreCase);

        var aspnetEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.IsNullOrEmpty(aspnetEnv))
        {
            builder.Environment.EnvironmentName = isProduction ? Environments.Production : Environments.Development;
        }
        else if (string.Equals(aspnetEnv, Environments.Production, StringComparison.OrdinalIgnoreCase))
        {
            isProduction = true;
        }

        var cleanName = serviceName.Replace("Service", "", StringComparison.OrdinalIgnoreCase).Trim('_');
        var upperName = cleanName.ToUpper();
        if (upperName.Contains("GATEWAY") || upperName.Contains("API"))
        {
            upperName = "API_GATEWAY";
        }

        var urlKeys = new[]
        {
            $"{upperName}_SERVICE_URL",
            $"{upperName}_URL",
            $"POSTGRES_{upperName}_SERVICE_URL",
            $"POSTGRES_{upperName}_URL"
        };

        string[] portKeys;
        if (isProduction)
        {
            portKeys = new[]
            {
                $"{upperName}_SERVICE_PRODUCTION_PORT",
                $"{upperName}_PRODUCTION_PORT",
                $"POSTGRES_{upperName}_SERVICE_PRODUCTION_PORT",
                $"{upperName}_SERVICE_PORT",
                $"{upperName}_PORT",
                $"{upperName}_SERVICE_DEVELOPMENT_PORT",
                $"{upperName}_DEVELOPMENT_PORT"
            };
        }
        else
        {
            portKeys = new[]
            {
                $"{upperName}_SERVICE_DEVELOPMENT_PORT",
                $"{upperName}_DEVELOPMENT_PORT",
                $"POSTGRES_{upperName}_SERVICE_DEVELOPMENT_PORT",
                $"{upperName}_SERVICE_PORT",
                $"{upperName}_PORT",
                $"{upperName}_SERVICE_PRODUCTION_PORT",
                $"{upperName}_PRODUCTION_PORT"
            };
        }

        string? rawUrl = null;
        foreach (var key in urlKeys)
        {
            var val = Environment.GetEnvironmentVariable(key) ?? Env.GetString(key);
            if (!string.IsNullOrWhiteSpace(val))
            {
                rawUrl = val.Trim();
                break;
            }
        }

        string? rawPort = null;
        foreach (var key in portKeys)
        {
            var val = Environment.GetEnvironmentVariable(key) ?? Env.GetString(key);
            if (!string.IsNullOrWhiteSpace(val))
            {
                rawPort = val.Trim();
                break;
            }
        }

        if (string.IsNullOrEmpty(rawUrl))
        {
            rawUrl = "http://localhost";
        }

        if (string.IsNullOrEmpty(rawPort))
        {
            rawPort = isProduction ? "6000" : "5000";
        }

        var inContainer = string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);

        string bindingUrl;
        if (inContainer)
        {
            bindingUrl = $"http://0.0.0.0:{rawPort}";
        }
        else
        {
            rawUrl = rawUrl.TrimEnd('/');
            if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            {
                bindingUrl = $"{uri.Scheme}://0.0.0.0:{rawPort}";
            }
            else
            {
                bindingUrl = $"http://0.0.0.0:{rawPort}";
            }
        }

        builder.WebHost.UseUrls(bindingUrl);
        builder.Configuration["ASPNETCORE_URLS"] = bindingUrl;
        builder.Configuration["ASPNETCORE_HTTP_PORTS"] = rawPort;

        Console.WriteLine($"🌐 [{serviceName.ToUpper()}] Configured to listen on {bindingUrl} (Mode: {(isProduction ? "Production" : "Development")})");

        return builder;
    }

    private static void LoadEnvFile()
    {
        var rootDir = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(rootDir, ".env")) && Directory.GetParent(rootDir) != null)
        {
            rootDir = Directory.GetParent(rootDir)!.FullName;
        }
        var envPath = Path.Combine(rootDir, ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            Env.TraversePath().Load();
        }
    }

    public static WebApplication UsePlatformApiStandard(this WebApplication app, string serviceName)
    {
        var cleanServiceName = serviceName.Replace("Service", "", StringComparison.OrdinalIgnoreCase).ToLower().Trim('_');
        if (cleanServiceName.Contains("gateway") || cleanServiceName.Contains("api"))
        {
            cleanServiceName = "gateway";
        }

        // Add Global Exception Handler Middleware
        app.UseExceptionHandler();

        // Ensure forwarded headers from Reverse Proxies (like YARP/NGINX) are applied
        // This fixes OpenAPI/Scalar generating internal network URLs in the servers list
        var forwardedOptions = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
        {
            ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto | 
                               Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost,
            ForwardLimit = null // Allow multiple hops (e.g. Cloudflare -> Liara Nginx -> API Gateway)
        };
        // By default, ASP.NET Core only trusts proxies from 127.0.0.1. 
        // In Docker, the Gateway has a different internal IP. We must clear these to trust our Gateway.
        forwardedOptions.KnownIPNetworks.Clear();
        forwardedOptions.KnownProxies.Clear();
        app.UseForwardedHeaders(forwardedOptions);

        var pathBase = $"/api/v1/{cleanServiceName}";
        app.UsePathBase(pathBase);

        app.UseRouting();

        // Enforce x-platform / x-workspace tenant boundaries
        app.UseMiddleware<HPK.Core.Middlewares.TenantValidationMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();

        app.MapScalarApiReference("docs", options =>
        {
            options.Title = $"{cleanServiceName.ToUpper()} Service API Documentation";
            options.OpenApiRoutePattern = "openapi/v1.json";
            options.Authentication = new ScalarAuthenticationOptions
            {
                PreferredSecuritySchemes = new[] { "Bearer", "x-workspace", "x-language", "x-account", "x-platform", "x-permission", "x-scopes", "x-authentication", "x-authorization", "x-page" }
            };
        });

            Console.WriteLine($"📖 [{cleanServiceName.ToUpper()}] Docs available at: {pathBase}/docs");
        }

        return app;
    }

    public static IServiceCollection AddPlatformApiStandard(this IServiceCollection services)
    {
        // 1. Add Global Exception Handler
        services.AddExceptionHandler<HPK.Core.Middlewares.GlobalExceptionHandler>();
        services.AddProblemDetails();

        // 2. Configure Controllers to use the ApiResponseFilter and force application/json
        services.AddControllers(options => 
        {
            options.Filters.Add<HPK.Core.Filters.ApiResponseFilter>();
            options.Filters.Add(new Microsoft.AspNetCore.Mvc.ProducesAttribute("application/json"));
        })
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .Select(e => e.Value!.Errors.First().ErrorMessage)
                    .FirstOrDefault();

                var message = errors ?? "One or more validation errors occurred.";
                var response = HPK.Core.Utils.Responses.ApiResponse.Error(422, message);
                return new Microsoft.AspNetCore.Mvc.ObjectResult(response)
                {
                    StatusCode = Microsoft.AspNetCore.Http.StatusCodes.Status422UnprocessableEntity
                };
            };
        });
        
        services.AddHttpContextAccessor();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var request = context.ApplicationServices.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()?.HttpContext?.Request;
                if (request != null)
                {
                    // Fallback to Request.Scheme/Host if X-Forwarded headers are missing
                    var proto = request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? request.Scheme;
                    var host = request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? request.Host.Value;
                    
                    document.Servers ??= [];
                    document.Servers.Clear();
                    document.Servers.Add(new() 
                    { 
                        Url = $"{proto}://{host}{request.PathBase}",
                        Description = "API Gateway"
                    });
                }

                // Add Authentication (Bearer) to OpenAPI Document
                document.Components ??= new Microsoft.OpenApi.OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new Microsoft.OpenApi.OpenApiSecurityScheme
                {
                    Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT token to authenticate."
                };

                var platformHeaders = new[]
                {
                    "x-account", "x-language", "x-workspace", "x-platform",
                    "x-permission", "x-scopes", "x-authentication", "x-authorization", "x-page"
                };

                foreach (var header in platformHeaders)
                {
                    document.Components.SecuritySchemes[header] = new Microsoft.OpenApi.OpenApiSecurityScheme
                    {
                        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
                        In = Microsoft.OpenApi.ParameterLocation.Header,
                        Name = header,
                        Description = $"Global header for {header}"
                    };
                }


                return Task.CompletedTask;
            });

            
        });
        return services;
    }
}
