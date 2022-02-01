using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace XamlPlayground.Views;

public class PreviewView : UserControl
{
    public PreviewView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
