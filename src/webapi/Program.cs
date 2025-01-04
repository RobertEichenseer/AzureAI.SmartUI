using DotNetEnv;
using AzureAI.SmartUI.Shared; using System.Text.Json;

//Read configuration
string _configurationFile = "../../config/config.env";
Env.Load(_configurationFile);
Config config = new Config(){
    ApiKey = Env.GetString("AOAI_APIKEY"),
    Endpoint = Env.GetString("AOAI_ENDPOINT"),
    CompletionModelDeployment = Env.GetString("AOAI_CHATCOMPLETION_DEPLOYMENTNAME"),
    StorageConnectionString = Env.GetString("STORAGE_CONNECTION_STRING"),
    StorageContainerName = Env.GetString("STORAGE_CONTAINER_NAME")
};

SmartUI smartUI = new SmartUI(config);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder
                .WithOrigins("http://localhost:4200")
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

var app = builder.Build();
app.UseCors("AllowSpecificOrigin");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles(); 


// API endpoints
app.MapGet("/response",  (string text) =>
{
    var response = new {
        tokenPerSec = 10f,
        modelResponse = "some response",
    };
    return response;
})
.WithName("GetResponse")
.WithOpenApi();

//"Copilot" endpoints (streaming & default)
app.MapGet("/responsestream", async (HttpContext context) =>
{
    //Transform request data
    UserRequest? userRequest =
        System.Text.Json.JsonSerializer.Deserialize<UserRequest>(
            context.Request.Query["requestData"].ToString()
        );

    if (userRequest == null || String.IsNullOrEmpty(userRequest.ChartData))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("No data provided. Please 'extract data first'");
        return;
    }

    //Detect intent
    Intent intent = await smartUI.DetectIntent(userRequest.UserMessage);
    
    //Respond to request
    context.Response.ContentType = "text/event-stream";
    await using (StreamWriter streamWriter = new StreamWriter(context.Response.Body))
    {
        if (intent == Intent.QueryData)
        {
            await foreach (string token in smartUI.CreateDataCompletion(userRequest.UserMessage, userRequest.ChartData))
            {
                await streamWriter.WriteLineAsync(token);
                await streamWriter.FlushAsync();
            }
        }
        else if (intent == Intent.ExportData)
        {
            await smartUI.CreateXlsFile(userRequest.ChartData, "./wwwroot/ExportData.xlsx");
            await streamWriter.WriteLineAsync("Point your browser to: http://localhost:5225/ExportData.xlsx");
            await streamWriter.FlushAsync();
        }
    }
});

app.MapGet("/getdata", async (HttpContext context) =>
{
    ChartData chartData = await smartUI.GetData();
    return chartData;
});

app.MapPost("/uploadchart", async (HttpContext context) =>
{
    //store uploaded file
    IFormCollection formCollection = context.Request.Form;
    IFormFile? file = formCollection.Files.GetFile("chartScreenShot");

    if (file == null || file.Length == 0)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsync("{\"message\": \"No file 'chartScreenShot' in form data\"}");
        return;
    }

    if (!Directory.Exists("uploads"))
    {
        Directory.CreateDirectory("uploads");
    }

    string fileName = Path.Combine("uploads", file.FileName);
    await using (var stream = new FileStream(fileName, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    context.Response.StatusCode = 200;
    context.Response.ContentType = "application/json";
    await context.Response.WriteAsync("{\"message\": \"File uploaded successfully.\"}");
});

app.MapGet("/extractdata", async (HttpContext context) =>
{
    string chartFileName = "uploads/ChartScreenShot.png";
    
    context.Response.ContentType = "text/event-stream";
    await using (StreamWriter streamWriter = new StreamWriter(context.Response.Body))
    {
        await foreach (string token in smartUI.ExtractData(DefaultPrompts.ExtractData_SystemMessage, chartFileName))
        {
            await streamWriter.WriteLineAsync(token);
            await streamWriter.FlushAsync();
        }
    }

});

app.MapGet("/ping", (string text) =>
{
    var response = new {
        response = text
    };

    return response;
})
.WithName("Ping")
.WithOpenApi();

// Run app
app.Run();
