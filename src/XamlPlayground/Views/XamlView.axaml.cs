using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace XamlPlayground.Views;

public class XamlView : UserControl
{
    public XamlView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
