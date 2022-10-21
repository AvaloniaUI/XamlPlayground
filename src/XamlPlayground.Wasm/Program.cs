using System;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Web;
using XamlPlayground;
using XamlPlayground.Services;
using XamlPlayground.ViewModels;
using XamlPlayground.Views;
using XamlPlayground.Wasm;

[assembly:SupportedOSPlatform("browser")]

internal partial class Program
{
    private static void Initialize(string? id)
    {
        CompilerService.BaseUri = Interop.GetBaseUri();
        
        id = id?.Replace("XamlPlayground/","").Replace("gist/","").Replace("?gist=", "").Replace("/", "");

        if (Application.Current is App app)
        {
            app.InitialGist = id;
        }
    }
    
    private static void Main(string[] args) =>
        BuildAvaloniaApp()
            .AfterSetup(_ => Initialize(args.FirstOrDefault()))
            .SetupBrowserApp("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
