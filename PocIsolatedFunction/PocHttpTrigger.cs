using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

static class PocHttpTrigger
{
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
