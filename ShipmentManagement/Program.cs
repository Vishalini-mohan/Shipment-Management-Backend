using ShipmentManagement.Middleware;
using ShipmentManagement.Services;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Hillebrand client (will use HttpClientFactory)
builder.Services.AddHttpClient<IHillebrandClient, HillebrandClient>();

// Simple in-memory caching for token (singleton service)
builder.Services.AddSingleton<TokenCache>();

// Added CORS for local React dev
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Middlewares
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("LocalDev");
app.MapControllers();

app.Run();
