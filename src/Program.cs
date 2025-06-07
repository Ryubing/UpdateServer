using Gommon;
using NSwag;
using RyujinxUpdate.Services.GitLab;

const string ApiVersion = "v1";

var builder = WebApplication.CreateBuilder(args);

var disableSwagger = args.ContainsIgnoreCase("--disable-swagger") || args.ContainsIgnoreCase("-ds");

builder.Services.AddSingleton<GitLabService>();

if (!disableSwagger)
{
    builder.Services.AddOpenApi(ApiVersion);
    builder.Services.AddOpenApiDocument(opt =>
    {
        opt.PostProcess = doc =>
        {
            doc.Info = new OpenApiInfo
            {
                Version = ApiVersion,
                Title = "Ryujinx Updates",
                Description = "REST API for Ryubing updates powered by ASP.NET Core."
            };
        };
    });
}

builder.Services.AddControllers();

var app = builder.Build();

app.Services.GetRequiredService<GitLabService>().Client.Version.Get();

if (!disableSwagger)
{
    app.MapOpenApi();
    app.UseSwaggerUi(opt => opt.DocumentPath = $"/openapi/{ApiVersion}.json");
}

app.MapControllers();

app.Run();