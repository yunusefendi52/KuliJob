using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KuliJob.Storage.Data;

[Table("job")]
[Index(nameof(JobName))]
// [Index(nameof(JobState))]
[Index(nameof(StartAfter))]
[Index(nameof(CreatedOn))]
[Index(nameof(ThrottleKey))]
internal class JobInputDb
{
    public required Guid Id { get; set; }
    public string JobName { get; set; } = null!;
    public string? JobData { get; set; }
    public required Guid JobStateId { get; set; }
    public JobState JobState { get; set; }
    public DateTimeOffset StartAfter { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public int RetryMaxCount { get; set; }
    public int RetryCount { get; set; }
    public int RetryDelayMs { get; set; }
    public int Priority { get; set; }
    public string? Queue { get; set; }
    public string? ServerName { get; set; }
    public string? ThrottleKey { get; set; }
    public int ThrottleSeconds { get; set; }
}
