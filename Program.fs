open Azure.Monitor.OpenTelemetry.Exporter
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open OpenTelemetry.Trace
open OpenTelemetry.Logs
open System.Diagnostics
open System

[<EntryPoint>]
let main args =
    let applicationInsightsConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")

    let activitySourceName = "bug44163"

    use host = 
        HostBuilder().ConfigureServices(fun hostContext services ->
            let openTelemetry = services.AddOpenTelemetry()
            openTelemetry.WithTracing(fun tracing ->
                tracing
                    .AddSource(activitySourceName)
                    .AddConsoleExporter()
                    .AddAzureMonitorTraceExporter(fun options ->
                        options.ConnectionString <- applicationInsightsConnectionString
                ) |> ignore
            ) |> ignore
            services.AddLogging(fun logging ->
                logging.AddSimpleConsole() |> ignore
                logging.AddOpenTelemetry(fun otelLogging ->
                    otelLogging.AddAzureMonitorLogExporter(fun options ->
                        options.ConnectionString <- applicationInsightsConnectionString
                    ) |> ignore
                    otelLogging.AddConsoleExporter() |> ignore
                ) |> ignore
            ) |> ignore
        ).Build()
    host.Start()

    use activitySource = new ActivitySource(activitySourceName)
    use activity = activitySource.CreateActivity("main", ActivityKind.Server);

    let loggerFactory = host.Services.GetRequiredService<ILoggerFactory>()
    let logger = loggerFactory.CreateLogger("main")

    let dog = "Barney"
    let cat = "Whiskers"
    logger.Log(LogLevel.Information, "1 Hello {dog} & {cat}!", dog, cat)
    //let msgArgs = [| dog; cat |]
    let msgArgs: Object[] = [| dog; cat |]
    logger.Log(LogLevel.Information, "2 Hello {dog} & {cat}!", msgArgs)
    0
