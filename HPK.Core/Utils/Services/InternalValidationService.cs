using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HPK.Core.Utils.Services;

public interface IInternalValidationService
{
    Task<bool> ValidateWorkspacesAsync(IEnumerable<Guid> workspaceIds, CancellationToken cancellationToken = default);
    Task<bool> ValidateWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<bool> ValidateAccountsAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default);
    Task<bool> ValidateAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}

public class InternalValidationService : IInternalValidationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InternalValidationService> _logger;

    public InternalValidationService(IHttpClientFactory httpClientFactory, ILogger<InternalValidationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> ValidateWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return await ValidateWorkspacesAsync(new[] { workspaceId }, cancellationToken);
    }

    public async Task<bool> ValidateAccountAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await ValidateAccountsAsync(new[] { accountId }, cancellationToken);
    }

    public async Task<bool> ValidateWorkspacesAsync(IEnumerable<Guid> workspaceIds, CancellationToken cancellationToken = default)
    {
        var ids = workspaceIds.Distinct().ToList();
        if (!ids.Any()) return true;

        var baseUrl = Environment.GetEnvironmentVariable("WORKSPACE_SERVICE_URL") ?? "http://localhost:5013";
        var url = $"{baseUrl.TrimEnd('/')}/internal/workspaces/validate";

        return await ValidateIdsAsync(url, ids, cancellationToken);
    }

    public async Task<bool> ValidateAccountsAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        var ids = accountIds.Distinct().ToList();
        if (!ids.Any()) return true;

        var baseUrl = Environment.GetEnvironmentVariable("IDP_SERVICE_URL") ?? "http://localhost:5005";
        var url = $"{baseUrl.TrimEnd('/')}/internal/accounts/validate";

        return await ValidateIdsAsync(url, ids, cancellationToken);
    }

    private async Task<bool> ValidateIdsAsync(string url, List<Guid> ids, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new { Ids = ids };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Internal validation failed. URL: {url}, StatusCode: {response.StatusCode}");
                return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<Dictionary<Guid, bool>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result == null) return false;

            return ids.All(id => result.TryGetValue(id, out var isValid) && isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling internal validation API: {url}");
            return false;
        }
    }
}
