using System;
using System.Windows.Input;
using ReactiveUI;

namespace XamlPlayground.ViewModels;

public class SampleViewModel : ViewModelBase
{
    private string _name;
    private string _xaml;
    private string? _code;

    public SampleViewModel(string name, string xaml, string? code, Action<string, string?> open)
    {
        _name = name;
        _xaml = xaml;
        _code = code;
        OpenCommand = ReactiveCommand.Create(() => open(_xaml, _code));
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

    public string? Code
    {
        get => _code;
        set => this.RaiseAndSetIfChanged(ref _code, value);
    }

    public ICommand OpenCommand { get; }
}
