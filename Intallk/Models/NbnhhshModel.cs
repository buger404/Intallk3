using System.Text.Json.Serialization;

namespace Intallk.Models;

public class NbnhhshRequest
{
    [JsonPropertyName("text")] public string? Text { get; set; }
}

public class NbnhhshRespond
{
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("trans")] public string[]? Trans { get; set; }
    [JsonPropertyName("inputting")] public string[]? Inputting { get; set; }
}