using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LoudnessMeterUI.Services;
using LoudnessMeterUI.ViewModels;
using LoudnessMeterUI.Views;

namespace LoudnessMeterUI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            //  Initialize the dependencies
            var audioInterface = new BassAudioCaptureService();
            var mainViewModel = new MainViewModel(audioInterface);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainViewModel,
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}