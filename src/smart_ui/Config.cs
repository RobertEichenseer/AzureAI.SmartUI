namespace AzureAI.SmartUI.Shared;

public class Config
{
    public string ApiKey { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string CompletionModelDeployment { get; set; } = "";
    public string StorageConnectionString { get; set; } = "";
    public string StorageContainerName { get; set; } = "";
}
