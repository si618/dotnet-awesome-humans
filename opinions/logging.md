---
targets: [net10.0, csharp-14]
last-reviewed: 2026-08-18
last-used: 2026-08-18
sources: [ms-learn, nicholas-blumhardt]
---

# Logging & tracing

One structured pipeline, exported over OTLP. Logs and traces are the same telemetry stream, not two systems.

> **Conflict of interest:** the Serilog-specific guidance below cites Nicholas Blumhardt, who authors Serilog and SerilogTracing and sells Seq (Datalust). The recommendations point at the OSS libraries and the `Activity` APIs in the BCL; no product is being recommended, and the fallback example deliberately uses an OTLP endpoint rather than Seq.

## Opinions

- **Log through `ILogger`/`ILogger<T>` and declare every event as a source-generated `[LoggerMessage]` partial method.** The generator eliminates boxing, temporary allocations, and the `string.Format` work that `logger.LogInformation($"...")` incurs even when the level is disabled, and it preserves the message template so structured sinks get named properties instead of a flattened string. It also beats hand-written `LoggerMessage.Define` calls: no six-parameter ceiling, dynamic level support, and compile-time diagnostics for duplicate event IDs. ([Microsoft Learn: Compile-time logging source generation](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator))

  ```csharp
  public partial class OrderService(ILogger<OrderService> logger)
  {
      [LoggerMessage(EventId = 100, Level = LogLevel.Information,
          Message = "Order {OrderId} accepted for {CustomerId}")]
      public partial void OrderAccepted(Guid orderId, string customerId);
  }
  ```

  Never interpolate into a log call — `LogInformation($"Order {orderId} accepted")` throws away the structure and formats the string whether or not anything is listening.

- **Emit spans with `ActivitySource.StartActivity`, not with log lines that record a duration.** `Activity` is the BCL's span type, ASP.NET Core and `System.Net.Http` already create and propagate W3C `traceparent` context with no code from you, and one instrumented tree gives you both the timing and the parent/child structure that stopwatch logging cannot. ([Microsoft Learn: Distributed tracing concepts](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-concepts)) For work that crosses a process or time boundary, propagate context explicitly — see [aspnet-core.md](aspnet-core.md#observability-propagate-trace-context-across-async-boundaries).

- **Control tracing cost in the `ActivityListener.Sample` callback, not by filtering at the sink.** Sampling is a creation-time decision: the listener can decline the activity outright, create an ID-only activity that still propagates trace context, or populate it fully. A recorded activity costs roughly a microsecond; one rejected at sampling costs under 100ns. Dropping spans at the exporter pays the full construction cost first and breaks trace trees, because children have no way to know their parent was discarded. ([Microsoft Learn: Distributed tracing concepts](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-concepts), [Blumhardt: .NET's `ActivityListener` sampling API](https://nblumhardt.com/2024/10/activity-listener-sampling/))

- **Export over OTLP to a collector; keep vendor SDKs out of application code.** `Microsoft.Extensions.Logging` plus the OpenTelemetry exporter is the default pipeline — one wire format, and the backend becomes a deployment concern rather than a code dependency. Serilog is the alternative worth taking when you want message-template logging with its sink ecosystem, and `SerilogTracing` then feeds `Activity` spans through the same Serilog pipeline so traces and logs share enrichers and sinks; it loses the default only because it adds a second configuration surface next to the one `ILogger` already gives you. ([Blumhardt: SerilogTracing](https://nblumhardt.com/2024/01/serilog-tracing/))

- **Give the log pipeline a durable local fallback, and expect it to lag.** A network collector will be unavailable at some point, and that is exactly when the logs matter. Chain a rolling file behind the network destination so nothing is lost. Budget for the delay: network sinks retry with backoff before reporting failure, so the fallback typically starts receiving events around ten minutes into an outage, not immediately — `BatchingOptions.RetryTimeLimit` is the knob if that window is wrong for you. ([Blumhardt: Serilog fallback sinks](https://nblumhardt.com/2024/10/fallback-logging/), [Blumhardt: Visualizing the Serilog 4.1 batch retry algorithm](https://nblumhardt.com/2024/10/retry-time-limit/))

  ```csharp
  Log.Logger = new LoggerConfiguration()
      .WriteTo.FallbackChain(
          wt => wt.OpenTelemetry(endpoint: "https://otlp.example/v1/logs"),
          wt => wt.File("logs/app-.txt", rollingInterval: RollingInterval.Day))
      .CreateLogger();
  ```

## Levels

- **`Information` is for events an operator would want in production; `Debug` is for developers.** Set the default minimum level to `Information` and raise noisy categories (`Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore.Database.Command`) to `Warning` in configuration rather than deleting the log calls.
- **Log an exception as the `exception` argument, never in the message.** The source generator treats the first `Exception` parameter specially and structured sinks record the type, message, and stack trace as separate fields. ([Microsoft Learn: Compile-time logging source generation](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator))
- **Redact classified data in the pipeline, not at the call site.** `Microsoft.Extensions.Compliance.Redaction` selects redactors by data classification, so a parameter annotated with your taxonomy's classification is masked everywhere it is logged, once `EnableRedaction()` is on the logging builder — a call-site `?? "***"` is one refactor away from leaking. ([Microsoft Learn: Compile-time logging source generation](https://learn.microsoft.com/dotnet/core/extensions/logger-message-generator))
