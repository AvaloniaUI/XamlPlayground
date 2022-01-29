using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using Avalonia.Markup.Xaml;

namespace XamlToy.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private ObservableCollection<SampleViewModel> _samples;
        private SampleViewModel? _selectedSample;
        private string? _xaml;
        private IControl? _control;

        public MainViewModel()
        {
            _samples = GetSamples();
            _selectedSample = _samples.FirstOrDefault();
            _xaml = _selectedSample?.Xaml;

            RunCommand = ReactiveCommand.Create(Run);

            this.WhenAnyValue(x => x.SelectedSample)
                .WhereNotNull()
                .Subscribe(x =>
                {
                    Xaml = x.Xaml;
                    Control = null;
                });
        }

        public ObservableCollection<SampleViewModel> Samples
        {
            get => _samples;
            set => this.RaiseAndSetIfChanged(ref _samples, value);
        }

        public SampleViewModel? SelectedSample
        {
            get => _selectedSample;
            set => this.RaiseAndSetIfChanged(ref _selectedSample, value);
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

        private string? GetSampleName(string resourceName)
        {
            var parts = resourceName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[parts.Length - 2]}";
            }

            return null;
        }

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

        private ObservableCollection<SampleViewModel> GetSamples()
        {
            var sampleExtension = ".txt";
            var samples = new ObservableCollection<SampleViewModel>();
            var assembly = typeof(MainViewModel).Assembly;
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                if (!resourceName.EndsWith(sampleExtension, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (LoadResourceString(resourceName) is { } xaml)
                {
                    if (GetSampleName(resourceName) is { } name)
                    {
                        samples.Add(new SampleViewModel(name, xaml));
                    }
                }
            }

            return samples;
        }

        private void Run()
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
