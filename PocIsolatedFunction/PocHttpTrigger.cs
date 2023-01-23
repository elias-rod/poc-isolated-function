using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

static class PocHttpTrigger
{
    /// <summary>
    /// HTTP-triggered function that starts the <see cref="PocOrchestration"/>.
    /// </summary>
    /// <param name="req">The HTTP request that was used to trigger this function.</param>
    /// <param name="durableClientContext">The Durable Functions client binding context object that is used to start and manage orchestration instances.</param>
    /// <param name="executionContext">The Azure Functions execution context, which is available to all function types.</param>
    /// <returns>Returns an HTTP response with more information about the started orchestration instance.</returns>
    [Function(nameof(PocHttpTriggerAsync))]
    public static async Task<HttpResponseData> PocHttpTriggerAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        [DurableClient] DurableClientContext durableClientContext,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(PocHttpTriggerAsync));

        var instanceId = await durableClientContext.Client.ScheduleNewOrchestrationInstanceAsync(nameof(PocOrchestration.PocOrchestrationAsync));

        logger.LogInformation("Created new orchestration {InstanceId}", instanceId);

        return durableClientContext.CreateCheckStatusResponse(req, instanceId);
    }

    [Function(nameof(PocTimerTriggerAsync))]
    public static async Task PocTimerTriggerAsync(
    [TimerTrigger("0 0 13 15 * *", RunOnStartup = true)] TimerInfo timerInfo,//Every 15th at 13hs
    [DurableClient] DurableClientContext durableContext,
    FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(PocHttpTriggerAsync));

        var instanceId = await durableContext.Client.ScheduleNewOrchestrationInstanceAsync(nameof(PocOrchestration.PocOrchestrationAsync));

        logger.LogInformation("Created new orchestration {InstanceId}", instanceId);
    }
}
