using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Kernel.Messaging.Events;

public class MessengerEventMessage
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    [JsonConverter(typeof(IntPriorityJsonConverter))]
    public int Priority { get; set; } = 3;

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

public class IntPriorityJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            var num = reader.GetInt32();
            return (num >= 1 && num <= 4) ? num : 3;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()?.ToLower() ?? "3";
            return str switch
            {
                "1" or "low" => 1,
                "2" or "medium" => 2,
                "3" or "high" => 3,
                "4" or "urgent" => 4,
                _ => int.TryParse(str, out var parsed) && parsed >= 1 && parsed <= 4 ? parsed : 3
            };
        }

        return 3;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        var num = (value >= 1 && value <= 4) ? value : 3;
        writer.WriteNumberValue(num);
    }
}

public class NotificationMakeData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;

    [JsonPropertyName("workspace_id")]
    public Guid? WorkspaceId { get; set; }

    [JsonPropertyName("account_id")]
    public Guid? AccountId { get; set; }

    [JsonPropertyName("params")]
    public Dictionary<string, string> Params { get; set; } = new();

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;
}

public class NotificationSendData
{
    [JsonPropertyName("notification_id")]
    public Guid NotificationId { get; set; }
}
