using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddSignalR();

// DEV CORS (adjust origins as needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
    policy
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .SetIsOriginAllowed(_ => true));
});

var app = builder.Build();

app.UseCors("dev");
app.UseDefaultFiles(); // serve index.html by default
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapHub<ChatServer.Hubs.ChatHub>("/hubs/chat");

app.Run();