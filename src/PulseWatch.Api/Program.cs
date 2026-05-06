var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "PulseWatch API");
app.MapGet("/healthz", () => new { status = "ok" });

app.Run();
