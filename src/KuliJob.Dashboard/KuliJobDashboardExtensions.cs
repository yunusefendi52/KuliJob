using System.Diagnostics;
using KuliJob.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder;

public static class KuliJobDashboardExtensions
{
    static string GetRootPath(this Hosting.IWebHostEnvironment hostEnvironment)
    {
#if DEBUG
        return Path.Join(hostEnvironment.ContentRootPath, "../", "KuliJob.Dashboard");
#else
        return Path.Join(hostEnvironment.ContentRootPath);
#endif
    }

    public static void AddKuliJobDashboard(this WebApplicationBuilder builder)
    {
        builder.Services.AddSpaStaticFiles(v =>
        {
            v.RootPath = "kulijob-client";
        });
    }

    public static void UseKuliJobDashboard(this WebApplication app)
    {
        app.AddApi();

        static bool isSpaPath(HttpContext httpContext)
        {
            return httpContext.Request.Path.StartsWithSegments("/kulijob") && !httpContext.Request.Path.StartsWithSegments("/kulijob/api");
        }
#if DEBUG
        Task.Run(async () =>
        {
            var process = Process.Start(new ProcessStartInfo()
            {
                FileName = "bun",
                Arguments = "run dev",
                WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "..", "Kulijob.Dashboard", "kulijob-client"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
            })!;
            process.StandardInput.Write($"pid:{process.Id}");
            process.OutputDataReceived += (s, e) =>
            {
                Console.WriteLine(e.Data);
            };
            process.BeginOutputReadLine();
            await process.WaitForExitAsync();
        });
        app.MapWhen(isSpaPath, app =>
        {
            app.UseSpa(v =>
            {
                v.UseProxyToSpaDevelopmentServer("http://localhost:3000");
            });
        });
#else
        var environment = app.Environment;
        app.MapWhen(isSpaPath, app =>
        {
            var clientDistFileProvider = new PhysicalFileProvider(Path.Combine(environment.GetRootPath(), "kulijob-client", ".output", "public"));
            app.UseSpaStaticFiles(new StaticFileOptions
            {
                FileProvider = clientDistFileProvider,
                RequestPath = "/kulijob",
                ServeUnknownFileTypes = true,
            });
            app.UseSpa(spa =>
            {
                spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
                {
                    FileProvider = clientDistFileProvider,
                };
            });
        });
#endif
    }
}
