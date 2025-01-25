using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder;

public static class KuliJobDashboardExtensions
{
    static string GetWWWPath(this Hosting.IWebHostEnvironment hostEnvironment)
    {
#if DEBUG
        return Path.Join(hostEnvironment.ContentRootPath, "../", "KuliJob.Dashboard", "wwwroot");
#else
        return Path.Join(hostEnvironment.ContentRootPath, "wwwroot", "_content", "KuliJob.Dashboard");
#endif
    }

    public static void AddKuliJobDashboard(this WebApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.AddRazorPages()
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
        app.UseStaticFiles(new StaticFileOptions
        {
            RequestPath = "/kulijob",
            FileProvider = new PhysicalFileProvider(Path.Join(app.Environment.GetWWWPath())),
        });
        app.MapRazorPages();
    }
}
