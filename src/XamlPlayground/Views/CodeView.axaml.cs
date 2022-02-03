using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace XamlPlayground.Views;

public class CodeView : UserControl
{
    public CodeView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
