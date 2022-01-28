using Avalonia.ReactiveUI;
using Avalonia.Web.Blazor;

namespace XamlToy.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        WebAppBuilder.Configure<XamlToy.App>()
            .UseReactiveUI()
            .SetupWithSingleViewLifetime();
    }
}