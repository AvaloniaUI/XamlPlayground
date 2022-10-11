using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Web;
using XamlPlayground;
using XamlPlayground.Services;
using XamlPlayground.Wasm;

[assembly:SupportedOSPlatform("browser")]

internal partial class Program
{
    private static void Initialize()
    {
        _ = JSHost.ImportAsync("interop.js", "./interop.js").ContinueWith(_ =>
        {
            CompilerService.BaseUri = Interop.GetBaseUri();
            Console.WriteLine($"BaseUri: {CompilerService.BaseUri}");
        });
    }
    
    private static void Main(string[] args) =>
        BuildAvaloniaApp()
            .AfterSetup(_ => Initialize())
            .SetupBrowserApp("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
