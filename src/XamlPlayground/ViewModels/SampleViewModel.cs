using System;
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

    public SampleViewModel(string name, string xaml, string code, Action<SampleViewModel> open, Action<SampleViewModel> autoRun)
    {
        _name = name;

        _xaml = new TextDocument { Text = xaml };
        _xaml.TextChanged += (_, _) => autoRun(this);

        _code = new TextDocument { Text = code };
        _code.TextChanged += (_, _) => autoRun(this);

        OpenCommand = new RelayCommand( () => open(this));
    }

    public ICommand OpenCommand { get; }
}
