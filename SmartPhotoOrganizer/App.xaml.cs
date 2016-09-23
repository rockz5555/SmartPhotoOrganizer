using System.Windows;
using System.Windows.Threading;
using SmartPhotoOrganizer.UIAspects;

namespace SmartPhotoOrganizer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var exceptionWindow = new ExceptionWindow(e.Exception);
            exceptionWindow.ShowDialog();
            e.Handled = true; // This will prevent default unhandled exception processing
        }
    }
}