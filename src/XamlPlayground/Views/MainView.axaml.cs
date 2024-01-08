using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using XamlPlayground.ViewModels;

namespace XamlPlayground.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ((MainViewModel)DataContext!).StorageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        base.OnAttachedToVisualTree(e);
    }
}
