using System;
using Avalonia;
using Avalonia.Web;
using XamlPlayground;
using XamlPlayground.Services;
using XamlPlayground.Wasm;

internal partial class Program
{
    private static void Main(string[] args)
    {
        CompilerService.BaseUri = Interop.GetBaseUri();
        Console.WriteLine($"BaseUri: {CompilerService.BaseUri}");

        BuildAvaloniaApp().SetupBrowserApp("out");
    }

    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}
