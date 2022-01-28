using ReactiveUI;

namespace XamlToy.ViewModels;

public class SampleViewModel : ViewModelBase
{
    private string _name;
    private string _xaml;

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

    public SampleViewModel(string name, string xaml)
    {
        _name = name;
        _xaml = xaml;
    }
}
