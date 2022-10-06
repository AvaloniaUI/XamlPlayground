using Avalonia;
using Avalonia.Web;
using XamlPlayground;

internal partial class Program
{
    private static void Main(string[] args)
    {
        Emscripten.Log(EM_LOG.INFO, "Call from Main");

        BuildAvaloniaApp()
            .AfterSetup(_ =>
            {
                //
            }).SetupBrowserApp("out");

        BuildAvaloniaApp().SetupBrowserApp("out");
    }

    public static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>();
}
