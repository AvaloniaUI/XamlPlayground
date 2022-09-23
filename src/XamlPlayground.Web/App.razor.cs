using Avalonia;
using Avalonia.Web.Blazor;

namespace XamlPlayground.Web;

public partial class App
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        WebAppBuilder.Configure<XamlPlayground.App>()
            .With(new SkiaOptions
            {
                CustomGpuFactory = null // Disable GPU
            })
            .SetupWithSingleViewLifetime();
    }
}
