using System.Xml.Serialization;
using WopiHost.Discovery.Enumerations;

namespace WopiHost.Discovery;

/// <inheritdoc cref="IDiscoverer" />
public class WopiDiscoverer : IDiscoverer
{
    private AsyncExpiringLazy<List<App>> _apps;
    private readonly XmlSerializer _discoveryDeserializer = new(typeof(Wopidiscovery));

    /// <summary>
    ///     Creates a new instance of the <see cref="WopiDiscoverer" />, a class for examining the capabilities of the WOPI
    ///     client.
    /// </summary>
    /// <param name="discoveryFileProvider">A service that provides the discovery file to examine.</param>
    /// <param name="discoveryOptions"></param>
    public WopiDiscoverer(IDiscoveryFileProvider discoveryFileProvider, DiscoveryOptions discoveryOptions)
    {
        DiscoveryFileProvider = discoveryFileProvider;
        DiscoveryOptions = discoveryOptions;
    }

    private IDiscoveryFileProvider DiscoveryFileProvider { get; }

    private DiscoveryOptions DiscoveryOptions { get; }

    private AsyncExpiringLazy<List<App>> Apps
    {
        get
        {
            return _apps ??= new AsyncExpiringLazy<List<App>>(async metadata =>
            {
                await using var def = await DiscoveryFileProvider.GetDiscoveryStreamAsync();
                var desed = _discoveryDeserializer.Deserialize(def) as Wopidiscovery;
                return new TemporaryValue<List<App>>
                {
                    Result = desed.Netzone.App,
                    ValidUntil = DateTimeOffset.Now.Add(DiscoveryOptions.RefreshInterval)
                };
            });
        }
    }

    ///<inheritdoc />
    public async Task<bool> SupportsExtensionAsync(string extension)
    {
        return await GetAppForextension(extension) != null;
    }


    ///<inheritdoc />
    public async Task<bool> SupportsActionAsync(string extension, WopiActionEnum action)
    {
        var app = await GetAppForextension(extension);
        if (app is null) return false;
        return app.Action.Any(x =>
            x.Name.Equals(action.ToString(), StringComparison.InvariantCultureIgnoreCase));
    }


    ///<inheritdoc />
    public async Task<string> GetUrlTemplateAsync(string extension, WopiActionEnum action)
    {
        return (await GetAppForextension(extension))?.Action.FirstOrDefault(x =>
            x.Name.Equals(action.ToString(), StringComparison.InvariantCultureIgnoreCase))?.Urlsrc;
    }

    ///<inheritdoc />
    public async Task<string> GetApplicationNameAsync(string extension)
    {
        return (await GetAppForextension(extension))?.Name;
    }


    ///<inheritdoc />
    public async Task<Uri> GetApplicationFavIconAsync(string extension)
    {
        var app = await GetAppForextension(extension);
        ;
        return new Uri(app?.FavIconUrl ?? "http://locahost:5000/file.ico");
    }

    private async Task<App> GetAppForextension(string extension)
    {
        var query = (await GetAppsAsync())
            .FirstOrDefault(x =>
                x.Action.Any(x => string.Equals(x.Ext, extension, StringComparison.InvariantCultureIgnoreCase)));

        return query;
    }

    internal async Task<List<App>> GetAppsAsync()
    {
        return await Apps.Value();
    }
}