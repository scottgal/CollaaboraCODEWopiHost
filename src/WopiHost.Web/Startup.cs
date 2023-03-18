using System.Net;
using WopiHost.Abstractions;
using WopiHost.Discovery;
using WopiHost.FileSystemProvider;
using WopiHost.Web.Models;

namespace WopiHost.Web;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    /// <summary>
    ///     Sets up the DI container.
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews()
            .AddRazorRuntimeCompilation(); // Add browser link
        services.AddSingleton(Configuration);

        // Configuration
        services.AddOptions();
        services.Configure<WopiOptions>(Configuration.GetSection(WopiConfigurationSections.WOPI_ROOT));

        services.AddHttpClient<IDiscoveryFileProvider, HttpDiscoveryFileProvider>(client =>
        {
            client.BaseAddress =
                new Uri(Configuration[$"{WopiConfigurationSections.WOPI_ROOT}:{nameof(WopiOptions.ClientUrl)}"]);
        }).ConfigurePrimaryHttpMessageHandler(x => new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });
        services.Configure<DiscoveryOptions>(
            Configuration.GetSection($"{WopiConfigurationSections.DISCOEVRY_OPTIONS}"));
        services.AddSingleton<IDiscoverer, WopiDiscoverer>();

        services.AddScoped<IWopiStorageProvider, WopiFileSystemProvider>();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConsole(); //Configuration.GetSection("Logging")
            loggingBuilder.AddDebug();
        });
    }

    /// <summary>
    ///     Configure is called after ConfigureServices is called.
    /// </summary>
    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        //app.UseHttpsRedirection();

        // Add static files to the request pipeline.
        app.UseStaticFiles();

        app.UseRouting();

        // Add MVC to the request pipeline.
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                "default",
                "{controller=Home}/{action=Index}/{id?}");
        });
    }
}