using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace XamlPlayground.Views;

public class HeaderView : UserControl
{
    public HeaderView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
