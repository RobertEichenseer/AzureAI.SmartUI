using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OfficeOpenXml; 

namespace AzureAI.SmartUI.Shared;

public class SmartUI
{
    Config _config;
    Kernel _kernel;
    IChatCompletionService _chatCompletionService;

    public SmartUI(Config config)
    {
        _config = config;

        var builder = Kernel.CreateBuilder().AddAzureOpenAIChatCompletion(
            _config.CompletionModelDeployment,
            _config.Endpoint,
            _config.ApiKey
        );
        _kernel = builder.Build();
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

    }

    public async Task<Intent> DetectIntent(string userMessage)
    {
        #pragma warning disable SKEXP0010
        #pragma warning disable CS1634
        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new OpenAIPromptExecutionSettings
        {
            ResponseFormat = DefaultPrompts.IntentDetection_ResponseSchema
        };
        PromptExecutionSettings promptExecutionSettings = new PromptExecutionSettings();
        #pragma warning restore SKEXP0010
        #pragma warning restore CS1634

        ChatHistory chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(DefaultPrompts.IntentDetection_SystemMessage);
        chatHistory.AddSystemMessage("Format the response according to the following JSON schema: " + DefaultPrompts.IntentDetection_ResponseSchema);
        chatHistory.AddUserMessage(userMessage);

        ChatMessageContent chatMessageContent = await _chatCompletionService.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            executionSettings: openAIPromptExecutionSettings,
            kernel: _kernel
        );

        if (chatMessageContent.Content == null)
        {
            return Intent.QueryData;
        } else {
            string detectedIntent = chatMessageContent.Content.Replace('\r', ' ').Replace('\n', ' ');
            try {
                JsonElement intentType = JsonDocument.Parse(detectedIntent)
                    .RootElement
                    .GetProperty("intent_type");

                return (Intent)Enum.Parse(
                    typeof(Intent), 
                    (intentType.GetString() ?? "CreateChart")
                );

            } catch {
                return Intent.QueryData;
            }
        } 
    }

    public async Task<ChartData> GetData()
    {
        return await Task.Run(() => new ChartData(true));
    }

    public async IAsyncEnumerable<string> ExtractData(string userMessage, string fileName)
    {
        // Upload image to Azure Blob Storage - Create SaaS
        BlobServiceClient blobServiceClient = new BlobServiceClient(_config.StorageConnectionString);
        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(_config.StorageContainerName);
        BlobClient blobClient = containerClient.GetBlobClient("chart.png");
        await blobClient.UploadAsync(
            new BinaryData(
                await File.ReadAllBytesAsync(fileName)
            ), 
            true
        );
        UriBuilder sasUri = new UriBuilder(blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)));

        ChatHistory chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(DefaultPrompts.ExtractData_SystemMessage);
        
        chatHistory.Add(
            new() {
                Role = AuthorRole.User,
                Items = {
                    new TextContent { Text = userMessage },
                    new ImageContent { Uri = sasUri.Uri }
                }
            }
        );

        IAsyncEnumerable<StreamingChatMessageContent> streamingChatMessageContent = 
            _chatCompletionService.GetStreamingChatMessageContentsAsync(
                chatHistory: chatHistory,
                kernel: _kernel
            );

        await foreach (StreamingChatMessageContent token in streamingChatMessageContent)
        {
            yield return token.Content??"";
        }
    }


    public async IAsyncEnumerable<string> CreateDataCompletion(string userMessage, string data){
        
        ChatHistory chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(DefaultPrompts.CreateDataCompletion_SystemMessage);
        chatHistory.AddSystemMessage(data);
        chatHistory.AddUserMessage(userMessage);

        IAsyncEnumerable<StreamingChatMessageContent> streamingChatMessageContent = 
            _chatCompletionService.GetStreamingChatMessageContentsAsync(
                chatHistory: chatHistory,
                kernel: _kernel
            );

        await foreach (StreamingChatMessageContent token in streamingChatMessageContent)
        {
            yield return token.Content??"";
        }
    }

    public async Task<bool> CreateXlsFile(string data, string fileName)
    {

        ChatHistory chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(DefaultPrompts.TransformDataToExcel_SystemMessage);
        chatHistory.AddUserMessage($"Transform this data: {data}");

        ChatMessageContent chatMessageContent = await _chatCompletionService.GetChatMessageContentAsync(
            chatHistory: chatHistory,
            kernel: _kernel
        );

        if (chatMessageContent.Content == null)
        {
            return false;   
        }

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using ExcelPackage excelPackage = new ExcelPackage();
        ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.Add("Data Export");

        string jsonXlsData = chatMessageContent.Content.Trim('´');
        JsonDocument jsonDocument = JsonDocument.Parse(jsonXlsData);
        foreach (JsonElement element in jsonDocument.RootElement.EnumerateArray())
        {
            string? value = element.GetProperty("Value").GetString();
            string? coordinates = element.GetProperty("Coordinates").GetString();
        
            worksheet.Cells[coordinates].Value = value;
        }
        await excelPackage.SaveAsAsync(fileName);

        return true;
    }
    

    public string ping(string value)
    {
        return value;
    }

}


