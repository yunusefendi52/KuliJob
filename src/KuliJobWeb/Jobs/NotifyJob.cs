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

        await CallApi(msg!);

        logger.LogInformation("Notification sent");
    }

    public async Task CallApi(string msg)
    {
        await httpClient.PostAsJsonAsync("https://eo7ux83lmfn8pti.m.pipedream.net/notify-job", new
        {
            Message = msg,
        });
    }
}