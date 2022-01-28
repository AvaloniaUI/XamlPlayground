using System;
using System.IO;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using Avalonia.Markup.Xaml;

namespace XamlToy.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string? _xaml;
        private IControl? _control;

        public MainViewModel()
        {
            RunCommand = ReactiveCommand.Create(Run);

            _xaml = LoadResourceString("XamlToy.Samples.BorderPage.txt");
        }

        public string? Xaml
        {
            get => _xaml;
            set => this.RaiseAndSetIfChanged(ref _xaml, value);
        }

        public IControl? Control
        {
            get => _control;
            set => this.RaiseAndSetIfChanged(ref _control, value);
        }

        public ICommand RunCommand { get; }

        private string? LoadResourceString(string name)
        {
            var assembly = typeof(MainViewModel).Assembly;
            using var stream = assembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                return null;
            }
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        public void Run()
        {
            try
            {
                var control = AvaloniaRuntimeXamlLoader.Parse<IControl?>(_xaml);
                if (control is { })
                {
                    Control = control;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
