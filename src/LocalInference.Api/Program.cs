using LocalInference.Api.Endpoints;
using LocalInference.Application.Services;
using LocalInference.Infrastructure.DependencyInjection;
using LocalInference.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "LocalInference API",
        Version = "v1",
        Description = "A high-performance General Inference API compatible with OpenAI's API specification"
    });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapHealthEndpoints();
app.MapChatCompletionsEndpoints();
app.MapSessionEndpoints();
app.MapInferenceConfigEndpoints();
app.MapRetrievalEndpoints();

app.Run();
