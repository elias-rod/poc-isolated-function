using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

static class PocHttpTrigger
{
    /// <summary>
    /// HTTP-triggered function that starts the <see cref="PocOrchestration"/>.
    /// </summary>
    /// <param name="httpRequestData">The HTTP request that was used to trigger this function.</param>
    /// <param name="durableClientContext">The Durable Functions client binding context object that is used to start and manage orchestration instances.</param>
    /// <param name="functionContext">The Azure Functions execution context, which is available to all function types.</param>
    /// <returns>Returns an HTTP response with more information about the started orchestration instance.</returns>
    [Function(nameof(PocHttpTriggerAsync))]
    public static async Task<HttpResponseData> PocHttpTriggerAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData httpRequestData,
        [DurableClient] DurableClientContext durableClientContext,
        FunctionContext functionContext,
        CancellationToken cancellationToken)//This is planned to be used in the next release with the ScheduleNewOrchestrationInstanceAsync method
    {
        var instanceId = await durableClientContext.Client.ScheduleNewOrchestrationInstanceAsync(nameof(PocOrchestration.PocOrchestrationAsync));
        
        var logger = functionContext.GetLogger(nameof(PocHttpTriggerAsync));
        logger.LogInformation("Created new orchestration {InstanceId}", instanceId);

        return durableClientContext.CreateCheckStatusResponse(httpRequestData, instanceId);
    }

    [Function(nameof(PocTimerTriggerAsync))]
    public static async Task PocTimerTriggerAsync(
    [TimerTrigger("0 0 12 1 * *", RunOnStartup = true)] TimerInfo timerInfo,//Every 1st at 12hs
    [DurableClient] DurableClientContext durableContext,
    FunctionContext functionContext,
    CancellationToken cancellationToken)
    {
        var instanceId = await durableContext.Client.ScheduleNewOrchestrationInstanceAsync(nameof(PocOrchestration.PocOrchestrationAsync));

        var logger = functionContext.GetLogger(nameof(PocTimerTriggerAsync));
        logger.LogInformation("Created new orchestration {InstanceId}", instanceId);
    }
}
