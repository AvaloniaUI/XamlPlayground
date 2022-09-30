using System;
using System.Threading.Tasks;
using System.Windows.Input;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XamlPlayground.ViewModels;

public partial class SampleViewModel : ViewModelBase
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private TextDocument _xaml;
    [ObservableProperty] private TextDocument _code;

    public SampleViewModel(string name, string xaml, string code, Func<SampleViewModel, Task> open, Func<SampleViewModel, Task> autoRun)
    {
        _name = name;

        _xaml = new TextDocument { Text = xaml };
        _xaml.TextChanged += async (_, _) => await autoRun(this);

        _code = new TextDocument { Text = code };
        _code.TextChanged += async (_, _) => await autoRun(this);

        OpenCommand = new AsyncRelayCommand(async () => await open(this));
    }

    public ICommand OpenCommand { get; }
}
