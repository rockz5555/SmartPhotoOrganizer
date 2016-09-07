using System.Windows;
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
			DispatcherUnhandledException += OnDispatcherUnhandledException;
            base.OnStartup(e);
        }
        private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var exceptionWindow = new ExceptionWindow(e.Exception);
            exceptionWindow.ShowDialog();
            e.Handled = true; // This will prevent default unhandled exception processing
        }
    }
}