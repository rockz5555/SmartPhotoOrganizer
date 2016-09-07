using System;

namespace SmartPhotoOrganizer.UIAspects
{
    /// <summary>
    /// Interaction logic for ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionWindow
    {
        public ExceptionWindow(Exception exception)
        {
            InitializeComponent();

            ExceptionTextBox.Text = exception + Environment.NewLine;
        }
    }
}
