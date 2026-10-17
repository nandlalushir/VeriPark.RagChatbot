using VeriPark.RagChatbot.Api.Endpoints;
using VeriPark.RagChatbot.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRagOptions(builder.Configuration)
    .AddAzureOpenAIServices(builder.Configuration)
    .AddVectorStore()
    .AddRagPipeline();

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

// Serves the small chat page in wwwroot/index.html at http://localhost:5080
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapChatEndpoints();

app.Run();
