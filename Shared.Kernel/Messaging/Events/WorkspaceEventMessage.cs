using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Kernel.Messaging.Events;

public class WorkspaceEventMessage
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    [JsonConverter(typeof(IntPriorityJsonConverter))]
    public int Priority { get; set; } = 4;

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

public class CustomerAddEventData
{
    [JsonPropertyName("workspace_id")]
    public Guid WorkspaceId { get; set; }

    [JsonPropertyName("account_id")]
    public Guid AccountId { get; set; }
}
