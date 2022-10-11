using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Web;
using XamlPlayground;
using XamlPlayground.Services;
using XamlPlayground.Wasm;

internal partial class Program
{
    private static void Initialize()
    {
#pragma warning disable CA1416
        _ = JSHost.ImportAsync("interop.js", "./interop.js").ContinueWith(_ =>
        {
            CompilerService.BaseUri = Interop.GetBaseUri();
            Console.WriteLine($"BaseUri: {CompilerService.BaseUri}");
        });
#pragma warning restore CA1416
    }
    
    private static void Main(string[] args) =>
        BuildAvaloniaApp()
            .AfterSetup(_ => Initialize())
            .SetupBrowserApp("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
