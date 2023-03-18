using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WopiHost.Abstractions;
using WopiHost.Discovery;
using WopiHost.Discovery.Enumerations;
using WopiHost.FileSystemProvider;
using WopiHost.Url;
using WopiHost.Web.Models;

namespace WopiHost.Web.Controllers;

public class HomeController : Controller
{
    private WopiUrlBuilder _urlGenerator;

    public HomeController(IOptions<WopiOptions> wopiOptions, IWopiStorageProvider storageProvider,
        IDiscoverer discoverer, ILoggerFactory loggerFactory)
    {
        WopiOptions = wopiOptions;
        StorageProvider = storageProvider;
        Discoverer = discoverer;
        LoggerFactory = loggerFactory;
    }

    private IOptions<WopiOptions> WopiOptions { get; }
    private IWopiStorageProvider StorageProvider { get; }
    private IDiscoverer Discoverer { get; }
    private ILoggerFactory LoggerFactory { get; }


    //TODO: remove test culture value and load it from configuration SECTION
    public WopiUrlBuilder UrlGenerator => _urlGenerator ??=
        new WopiUrlBuilder(Discoverer, new WopiUrlSettings { UiLlcc = new CultureInfo("en-US") });

    public async Task<ActionResult> Index()
    {
        try
        {
            var files = StorageProvider.GetWopiFiles(StorageProvider.RootContainerPointer.Identifier);
            var fileViewModels = new List<FileViewModel>();
            foreach (var file in files)
                fileViewModels.Add(new FileViewModel
                {
                    FileId = file.Identifier,
                    FileName = file.Name,
                    SupportsEdit = await Discoverer.SupportsActionAsync(file.Extension, WopiActionEnum.Edit),
                    SupportsView = await Discoverer.SupportsActionAsync(file.Extension, WopiActionEnum.View),
                    IconUri = await Discoverer.GetApplicationFavIconAsync(file.Extension) ??
                              new Uri("file.ico", UriKind.Relative)
                });
            return View(fileViewModels);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public async Task<ActionResult> Detail(string id, string wopiAction)
    {
        var actionEnum = Enum.Parse<WopiActionEnum>(wopiAction);
        var securityHandler = new WopiSecurityHandler(LoggerFactory); //TODO: via DI

        var file = StorageProvider.GetWopiFile(id);
        var token = securityHandler.GenerateAccessToken("Anonymous", file.Identifier);


        ViewData["access_token"] = securityHandler.WriteToken(token);
        //TODO: fix
        //ViewData["access_token_ttl"] = //token.ValidTo

        //http://dotnet-stuff.com/tutorials/aspnet-mvc/how-to-render-different-layout-in-asp-net-mvc


        var extension = file.Extension.TrimStart('.');
        ViewData["urlsrc"] =
            await UrlGenerator.GetFileUrlAsync(extension, new Uri($"http://192.168.0.34:5003/wopi/files/{id}"),
                actionEnum); //TODO: add a test for the URL not to contain double slashes between host and path
        ViewData["favicon"] = await Discoverer.GetApplicationFavIconAsync(extension);
        return View();
    }
}