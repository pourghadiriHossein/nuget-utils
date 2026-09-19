# Icot.Shared.Kernel

`Icot.Shared.Kernel` is the core foundation library for the ICOT Microservices ecosystem. It provides the essential building blocks, shared configurations, and cross-cutting concerns required by all ICOT services to maintain consistency, reliability, and standardized API behaviors.

> **Note on Domain Purity**: This package strictly contains **core architectural components**. It does *not* contain application-specific business logic, domain events, or service-specific models. Messaging events (like `AccountPermissionSyncEvent` or `WorkspaceEventMessage`) are managed exclusively within the bounded contexts of their respective microservices.

---

## 📦 Features & Capabilities

### 1. API Standardization (`PlatformApiExtensions`, `ApiResponseFilter`)
Ensures all microservices expose a uniform API surface.
- **Auto-Formatting**: Automatically wraps standard ASP.NET `ObjectResult` outputs into a uniform JSON response structure (`ApiResponse<T>`).
- **Standard Routing**: Applies the standard `/api/v1/{service}/` path base.
- **Swagger/Scalar**: Auto-configures OpenAPI documentation with JWT Bearer support and Scalar UI.
- **Global Error Handling**: Injects `GlobalExceptionHandler` to catch unhandled exceptions and format them as standard HTTP 500 error responses.

### 2. Service Registry & Discovery (`[RegistryEndpoint]`)
Decorate your controllers or methods to automatically register them in the ICOT platform.
- Defines exactly which scopes and permissions are required to access an endpoint.
- Declares HTTP methods and authentication/authorization requirements explicitly.
- Used heavily by `workspace-service` and `idp-service` to dynamically index platform capabilities.

### 3. JSON API & Pagination (`JsonApiQueryOptions`, `JsonApiExtensions`)
A complete set of tools to handle standard JSON API query parameters out of the box.
- Seamlessly parse `page[number]`, `page[size]`, `sort`, `filter[...]`, and `include` from the URL.
- Extension methods for `IQueryable<T>` (`ApplyFilters`, `ApplySort`, `ApplyPagination`) to dramatically reduce boilerplate when writing data endpoints.

### 4. Shared Data Entities (`BaseEntity`)
Provides the standard EF Core entity foundation containing `Id`, `CreatedAt`, `UpdatedAt`, `DeletedAt`, `CreatedBy`, and `UpdatedBy` properties for consistent auditing and soft deletes across all databases.

### 5. Multi-Tenancy & Headers (`TenantValidationMiddleware`)
Middleware that ensures strictly required standard headers (`x-platform`, `x-workspace`, etc.) are respected, securing intra-service communication and tenant context isolation.

### 6. Database & Migration Helpers (`PlatformDbExtensions`)
Provides automatic database registration and seamless startup migrations for PostgreSQL using Entity Framework Core. Ensures that services can initialize and migrate their own isolated databases instantly on boot.

---

## 🚀 Quick Start Guide

### Setup Platform API
In your `Program.cs`, use the extensions to instantly bootstrap a fully compliant ICOT service:

```csharp
using Shared.Kernel.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure the platform host and environment overrides
builder.ConfigurePlatformHost("my-service");

// 2. Add standard platform services (Swagger, Auth, Filters, JSON formatting)
builder.Services.AddPlatformApiStandard();

// 3. Register your EF Core DbContext
builder.Services.AddPlatformDbContext<MyDbContext>("icot_myservice", "MY_SERVICE_DB");

var app = builder.Build();

// 4. Auto-apply migrations on startup
await app.Services.ApplyPlatformMigrationsAsync<MyDbContext>();

// 5. Use standard middleware pipeline (PathBase, OpenAPI, Auth)
app.UsePlatformApiStandard("my-service");

app.MapControllers();
app.Run();
```

### Decorating Endpoints
Always use `[RegistryEndpoint]` for external-facing actions:
```csharp
[HttpGet]
[RegistryEndpoint(
    PlatformHttpMethod.Get,
    PlatformService.MyService,
    "api/v1/my-service/resources",
    permissions: ["myservice.resource.view"],
    scopes: [],
    needAuthentication: true,
    needAuthorization: true
)]
public async Task<IActionResult> Get([FromQuery] JsonApiQueryOptions options)
{
    // Implementation
}
```

### Using JSON API Queries
```csharp
public async Task<IActionResult> Get([FromQuery] JsonApiQueryOptions options)
{
    var query = _dbContext.Resources.AsQueryable();

    // Auto-apply filters, sorting, and pagination
    var result = await query.GetPagedResultAsync(options);
    
    return Ok(result); // Will be automatically wrapped by ApiResponseFilter!
}
```
