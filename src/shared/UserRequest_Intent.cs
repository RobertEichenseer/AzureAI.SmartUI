using System.Text.Json.Serialization;

namespace AzureAI.SmartUI.Shared;

//Intent Detection
public enum Intent
{
    QueryData,
    ExportData
}

public class UserRequest{
    [JsonPropertyName("userMessage")]
    public string UserMessage { get; set; } = ""; 
    [JsonPropertyName("chartData")]
    public string ChartData { get; set; } = "";
}

    