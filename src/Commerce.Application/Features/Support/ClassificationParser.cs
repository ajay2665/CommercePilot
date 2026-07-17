using System.Text.Json;
using Commerce.Application.Abstractions;
using Commerce.Domain;

namespace Commerce.Application.Features.Support;

/// <summary>
/// Tolerant parser for the model's JSON classification. Local 7–8B models
/// occasionally wrap output in fences or emit slightly off enum casing —
/// recover instead of failing the pipeline.
/// </summary>
public static class ClassificationParser
{
    public static bool TryParse(string? raw, out TicketClassification result)
    {
        result = Fallback("");
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        string json = Extract(raw);
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            string brand = GetString(root, "brand") ?? "Unknown";
            string summary = GetString(root, "summary") ?? "";
            double confidence = root.TryGetProperty("confidence", out var c) && c.ValueKind is JsonValueKind.Number
                ? Math.Clamp(c.GetDouble(), 0d, 1d)
                : 0d;

            result = new TicketClassification(
                brand,
                ParseEnum(GetString(root, "category"), TicketCategory.Other),
                ParseEnum(GetString(root, "urgency"), TicketUrgency.Medium),
                ParseEnum(GetString(root, "sentiment"), TicketSentiment.Neutral),
                confidence,
                summary);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static TicketClassification Fallback(string subject) => new(
        "Unknown", TicketCategory.Other, TicketUrgency.Medium, TicketSentiment.Neutral, 0d,
        subject.Length > 120 ? subject[..120] : subject);

    /// <summary>Strips markdown fences / prose and returns the outermost JSON object.</summary>
    private static string Extract(string raw)
    {
        int start = raw.IndexOf('{');
        int end = raw.LastIndexOf('}');
        return start >= 0 && end > start ? raw[start..(end + 1)] : raw;
    }

    private static string? GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var el) && el.ValueKind is JsonValueKind.String ? el.GetString() : null;

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct
        => Enum.TryParse(value?.Trim(), ignoreCase: true, out TEnum parsed) ? parsed : fallback;
}
