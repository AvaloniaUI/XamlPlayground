using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace XamlToy.Views;

public class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}