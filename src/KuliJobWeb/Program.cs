using KuliJobWeb;
using KuliJobWeb.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();

builder.Services.AddHttpClient();

builder.Services.AddKuliJob(v =>
{
    v.MinCronPollingIntervalMs = 500;
    // v.UsePostgreSQL("Host=localhost;Username=postgres;Password=postgres;Database=KuliJobWeb;Include Error Detail=True");
    v.UseSqlite("kulijob.db");

    v.AddKuliJob<NotifyJob>("notify_job");
});
// builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.AddKuliJobDashboard();

builder.Services.AddHostedService<MyHostedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseAuthorization();
app.UseStaticFiles();

// app.MapRazorPages();
app.UseKuliJobDashboard();

app.Run();
