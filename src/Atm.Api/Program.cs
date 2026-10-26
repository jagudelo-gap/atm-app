using Atm.Accounts;
using Atm.Api.Endpoints;
using Atm.Api.Errors;
using Atm.Api.Health;
using Atm.Api.Seeding;
using Atm.Common.Data;
using Atm.Transactions;
using Atm.Users;
using Microsoft.AspNetCore.Http.Json;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
var storage = Storage.From(builder.Configuration);

builder.Services.AddSerilog((services, log) => log
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(storage.DataFolder, "logs", "atm-.log"),
        rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

builder.Services
    .AddSharedSqlite(storage.ConnectionString)
    .AddUsersModule()
    .AddAccountsModule()
    .AddTransactionsModule()
    .AddSingleton<DatabaseInitializer>()
    .AddAtmHealthChecks();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Malformed or missing bodies and unparsable route values throw BadHttpRequestException, mapped to a 400 ProblemDetails.
builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = true);
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.Converters.Add(new UtcDateTimeConverter()));

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseExceptionHandler();

app.MapUserEndpoints();
app.MapTransactionEndpoints();
app.MapDemoEndpoints();
app.MapAtmHealth();

// SPA: static files from wwwroot, with index.html for any client route (never /api or /health).
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("{*path:nonfile:regex(^(?!(api|health)(/|$)).*$)}", "index.html");

await app.Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
app.Logger.LogInformation("Database ready in {DataFolder}", storage.DataFolder);

await app.RunAsync();

public partial class Program;
