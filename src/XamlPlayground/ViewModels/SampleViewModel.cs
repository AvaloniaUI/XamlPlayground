using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XamlPlayground.ViewModels;

public partial class SampleViewModel : ViewModelBase
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _xaml;
    [ObservableProperty] private string? _code;

    public SampleViewModel(string name, string xaml, string? code, Func<string, string?, Task> open)
    {
        _name = name;
        _xaml = xaml;
        _code = code;
        OpenCommand = new AsyncRelayCommand(async () => await open(_xaml, _code));
    }

    public ICommand OpenCommand { get; }
}
