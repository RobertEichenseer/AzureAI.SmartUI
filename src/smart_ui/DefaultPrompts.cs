using System.Text.Json.Serialization;

namespace AzureAI.SmartUI.Shared;

public static class DefaultPrompts
{
    public static string IntentDetection_SystemMessage = @"
        You detect intent from provided user messages.
        You can detect ""QueryData"" and ""ExportData"".
        Chart equals to data. If you get questions about charts you get a question about data.
        Be careful if data should be exported you identify a data export.
        Be careful if data should be queried or explained or processed you detect a data query.
        You don't add trailing or leading characters.
        You just detect intention of the user text. 
    ";

    private static string intentDetectionResponseSchema = @"{
        ""$schema"": ""http://json-schema.org/draft-07/schema#"",
        ""type"": ""object"",
        ""properties"": {
        ""intent_type"": {
            ""type"": ""string""
        }
        },
        ""required"": [""intent_type""]
    }";

    public static object IntentDetection_ResponseSchema {
        get {
            var responseFormat = new ChatResponseFormat
            {
                JsonSchema = intentDetectionResponseSchema
            };
            return responseFormat.JsonSchema;
        }
    }

    public static string ExtractData_SystemMessage = @"
        You extract data from a provided bar chart.
        The chart has a headline which you also extract and provide.
        You response with a valid JSON string
        You don't add trailing or leading characters.
    ";

    public static string CreateDataCompletion_SystemMessage = @"
        You answer questions based on the attached JSON data.
        You don't answer any questions not related to the data.
        The attached JSON data you receive is often referred to as chart or chart data. 
        You answer questions not related to the data with 'Sorry - Just can talk about the data'
    ";

    public static string TransformDataToExcel_SystemMessage = @"
        You transform JSON data in a format optimized to be shown in an excel worksheet. 
        The excel worksheet is 2-dimensional.
        You provide a valid JSON object.
        Every data point within the output JSON object contains x and y coordinates.
        You don't add trailing or leading characters.
        You just provide the JSON object and you ensure that it is valid JSON.
        You provide all values as strings within the JSON object.
        # Example
        [{
            ""Value"": ""Caption"",
            ""Coordinates"": ""A1""
        }]
    "; 
}

public class ChatResponseFormat
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("response_format")]
    public object? JsonSchema { get; set; }
}