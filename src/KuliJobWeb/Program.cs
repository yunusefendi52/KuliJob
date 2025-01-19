using KuliJobWeb.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddKuliJob(v =>
{
    v.UseSqlite("kulijob.db");

    v.AddKuliJob<NotifyJob>("notify_job");
});
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.AddKuliJobDashboard();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.UseKuliJobDashboard();

app.Run();
