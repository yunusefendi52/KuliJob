using KuliJobWeb.Jobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation();

builder.Services.AddHttpClient();

builder.Services.AddKuliJob(v =>
{
    v.UseSqlite("kulijob.db");

    v.AddKuliJob<NotifyJob>("notify_job");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

app.UseAuthorization();

app.UseStaticFiles();

// app.MapControllerRoute(
//     name: "default",
//     pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
