using KuliJob;

namespace KuliJobWeb.Jobs;

public class NotifyJob(
    HttpClient httpClient,
    ILogger<NotifyJob> logger) : IJob
{
    public async Task Execute(JobContext context)
    {
        logger.LogInformation("Start sending notification");

        var msg = context.JobData.GetValue<string>("msg");
        
        await httpClient.PostAsJsonAsync("https://webhook.site/631adca8-cc1b-4f8c-b578-1796ece2f377/notify-job", new
        {
            Message = msg,
        }, cancellationToken: cancellationToken);
        
        logger.LogInformation("Notification sent");
    }
}