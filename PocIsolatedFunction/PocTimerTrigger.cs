using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

static class PocTimerTrigger
{
    [Function(nameof(PocTimerTriggerAsync))]
    public static async Task PocTimerTriggerAsync(
    [TimerTrigger("0 0 13 15 * *", RunOnStartup = true)] TimerInfo timerInfo,//Every 15th at 13hs
    [DurableClient] DurableClientContext durableContext,
    FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(PocTimerTriggerAsync));

        var instanceId = await durableContext.Client.ScheduleNewOrchestrationInstanceAsync(nameof(PocOrchestration.PocOrchestrationAsync));

        logger.LogInformation("Created new orchestration {InstanceId}", instanceId);
    }
}
