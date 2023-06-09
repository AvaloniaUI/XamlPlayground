using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using XamlPlayground;
using XamlPlayground.Services;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    private static void Initialize(string id, string baseUri)
    {
        CompilerService.BaseUri = baseUri;

        id = id.Replace("XamlPlayground/", "").Replace("gist/", "").Replace("?gist=", "").Replace("/", "");

        if (Application.Current is App app)
        {
            app.InitialGist = id;
        }
    }

    private static Task Main(string[] args) =>
        BuildAvaloniaApp()
            .AfterSetup(_ => Initialize(args[0], args[1]))
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .WithInterFont();
}
