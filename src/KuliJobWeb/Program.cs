using KuliJobWeb;
using KuliJobWeb.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddLogging();

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();

builder.Services.AddKuliJob(v =>
{
    v.MinCronPollingIntervalMs = 1000;
    // v.MinPollingIntervalMs = 2000;
    v.UsePostgreSQL("Host=localhost;Username=postgres;Password=postgres;Database=KuliJobWeb;Include Error Detail=True");
    // v.UseSqlite("bin/kulijob2.db");

    v.AddKuliJob<NotifyJob>("notify_job");

    v.AddCron<NotifyJob>(t => t.CallApi("hi from cron"), new()
    {
        CronName = "notify_job_cron",
        CronExpression = "* * * * *",
    });
});
// builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.AddKuliJobDashboard();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseAuthorization();
app.UseStaticFiles();

app.MapRazorPages();
app.UseKuliJobDashboard();

app.Run();
