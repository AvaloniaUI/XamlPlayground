using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
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
        private IDisposable? _selectedSampleXamlDisposable;
        private IControl? _control;
        private bool _enableAutoRun;

        public MainViewModel()
        {
            _samples = GetSamples(".xml");
            _selectedSample = _samples.FirstOrDefault();
            _enableAutoRun = true;

            RunCommand = ReactiveCommand.Create(Run);

            this.WhenAnyValue(x => x.SelectedSample)
                .WhereNotNull()
                .Subscribe(x =>
                {
                    AutoRun(x, _enableAutoRun);
                    Control = null;
                });

            this.WhenAnyValue(x => x.EnableAutoRun)
                .DistinctUntilChanged()
                .Subscribe(x =>
                {
                    if (_selectedSample is { })
                    {
                        AutoRun(_selectedSample, x);
                    }
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

        public IControl? Control
        {
            get => _control;
            set => this.RaiseAndSetIfChanged(ref _control, value);
        }

        public bool EnableAutoRun
        {
            get => _enableAutoRun;
            set => this.RaiseAndSetIfChanged(ref _enableAutoRun, value);
        }

        public ICommand RunCommand { get; }

        private static string? GetSampleName(string resourceName)
        {
            var parts = resourceName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[parts.Length - 2]}";
            }

            return null;
        }

        private static string? LoadResourceString(string name)
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

        private static ObservableCollection<SampleViewModel> GetSamples(string sampleExtension)
        {
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

        private void AutoRun(SampleViewModel sample, bool enableAutoRun)
        {
            _selectedSampleXamlDisposable?.Dispose();

            if (enableAutoRun)
            {
                _selectedSampleXamlDisposable = sample.WhenAnyValue(s => s.Xaml)
                    .WhereNotNull()
                    .Subscribe(_ => Run());
            }
        }

        private void Run()
        {
            try
            {
                if (_selectedSample?.Xaml is { } xaml)
                {
                    var control = AvaloniaRuntimeXamlLoader.Parse<IControl?>(xaml);
                    if (control is { })
                    {
                        Control = control;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
