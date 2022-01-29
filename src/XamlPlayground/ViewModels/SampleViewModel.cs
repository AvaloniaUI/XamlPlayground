using System;
using System.Windows.Input;
using ReactiveUI;

namespace XamlPlayground.ViewModels;

public class SampleViewModel : ViewModelBase
{
    private string _name;
    private string _xaml;

    public SampleViewModel(string name, string xaml, Action<string> open)
    {
        _name = name;
        _xaml = xaml;
        OpenCommand = ReactiveCommand.Create(() => open(_xaml));
    }

    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public string Xaml
    {
        get => _xaml;
        set => this.RaiseAndSetIfChanged(ref _xaml, value);
    }

    public ICommand OpenCommand { get; }
}
