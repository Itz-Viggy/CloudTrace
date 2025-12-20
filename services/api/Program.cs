using CloudTrace.Api.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<FirestoreRepository>();
builder.Services.AddSingleton<BigQueryRepository>();

// Enable CORS for the frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "api" }));

// Task 8.3 - List Incidents
app.MapGet("/incidents", async (FirestoreRepository repo) =>
{
    var incidents = await repo.GetIncidentsAsync();
    return Results.Ok(incidents);
});

// Task 8.4 - Get Incident Detail
app.MapGet("/incidents/{id}", async (string id, FirestoreRepository repo) =>
{
    var incident = await repo.GetIncidentByIdAsync(id);
    return incident != null ? Results.Ok(incident) : Results.NotFound(new { error = "Incident not found" });
});

// Task 8.5 - Overview Metrics
app.MapGet("/metrics/overview", async (BigQueryRepository repo) =>
{
    var metrics = await repo.GetOverviewMetricsAsync();
    return Results.Ok(metrics);
});

app.Run();
