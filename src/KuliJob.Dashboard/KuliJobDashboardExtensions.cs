using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder;

public static class KuliJobDashboardExtensions
{
    public static void AddKuliJobDashboard(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddRazorPages(v =>
        {
            v.Conventions.AddPageRoute("/KuliJob/Jobs", "/kulijob/{*url}");
            v.Conventions.AddPageRoute("/KuliJob/Scheduler", "/kulijob/scheduler/{*url}");
        })
#if DEBUG
            .AddRazorRuntimeCompilation(v =>
            {
                v.FileProviders.Add(new PhysicalFileProvider(Path.Join(applicationBuilder.Environment.ContentRootPath, "../", "KuliJob.Dashboard")));
            })
#endif
            ;
    }

    public static void UseKuliJobDashboard(this WebApplication app)
    {
        app.UseStaticFiles();
        app.MapRazorPages();
    }
}
