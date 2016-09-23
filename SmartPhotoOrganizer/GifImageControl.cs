using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Linq;
using Image = System.Windows.Controls.Image;

namespace SmartPhotoOrganizer
{
    // GifImageControl source from http://www.solidrockstable.com/blogs/PragmaticTSQL/Lists/Posts/Post.aspx?ID=37
    public class GifImageControl : Image
    {
        public static readonly DependencyProperty AllowClickToPauseProperty =
            DependencyProperty.Register("AllowClickToPause", typeof(bool), typeof(GifImageControl),
                new UIPropertyMetadata(true));

        public static readonly DependencyProperty GifSourceProperty =
            DependencyProperty.Register("GifSource", typeof(string), typeof(GifImageControl),
                new UIPropertyMetadata("", GIFSource_Changed));

        public static readonly DependencyProperty PlayAnimationProperty =
            DependencyProperty.Register("PlayAnimation", typeof(bool), typeof(GifImageControl),
                new UIPropertyMetadata(true, PlayAnimation_Changed));

        private Bitmap _bitmap;

        private bool _mouseClickStarted;

        public GifImageControl()
        {
            MouseLeftButtonDown += GIFImageControl_MouseLeftButtonDown;
            MouseLeftButtonUp += GIFImageControl_MouseLeftButtonUp;
            MouseLeave += GIFImageControl_MouseLeave;
            Click += GIFImageControl_Click;
        }

        public bool AllowClickToPause
        {
            get { return (bool) GetValue(AllowClickToPauseProperty); }
            set { SetValue(AllowClickToPauseProperty, value); }
        }

        public bool PlayAnimation
        {
            get { return (bool) GetValue(PlayAnimationProperty); }
            set { SetValue(PlayAnimationProperty, value); }
        }

        public string GifSource
        {
            get { return (string) GetValue(GifSourceProperty); }
            set { SetValue(GifSourceProperty, value); }
        }

        private void GIFImageControl_Click(object sender, RoutedEventArgs e)
        {
            if (AllowClickToPause)
            {
                PlayAnimation = !PlayAnimation;
            }
        }

        private void GIFImageControl_MouseLeave(object sender, MouseEventArgs e)
        {
            _mouseClickStarted = false;
        }

        private void GIFImageControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseClickStarted)
            {
                FireClickEvent(sender, e);
            }

            _mouseClickStarted = false;
        }

        private void GIFImageControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _mouseClickStarted = true;
        }

        private void FireClickEvent(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(sender, e);
        }

        public event RoutedEventHandler Click;

        private static void PlayAnimation_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gic = (GifImageControl) d;
            if ((bool) e.NewValue)
            {
                //StartAnimation if GIFSource is properly set
                if (null != gic._bitmap)
                {
                    ImageAnimator.Animate(gic._bitmap, gic.OnFrameChanged);
                }
            }
            else
            {
                //Pause Animation
                ImageAnimator.StopAnimate(gic._bitmap, gic.OnFrameChanged);
            }
        }


        private void SetImageGifSource()
        {
            if (_bitmap != null)
            {
                ImageAnimator.StopAnimate(_bitmap, OnFrameChanged);
                _bitmap.Dispose();
                _bitmap = null;
            }

            if (string.IsNullOrEmpty(GifSource))
            {
                //Turn off if GIF set to null or empty
                Source = null;
                InvalidateVisual();
                return;
            }

            if (File.Exists(GifSource))
            {
                _bitmap = (Bitmap) System.Drawing.Image.FromFile(GifSource);
            }
            else
            {
                //Support looking for embedded resources
                var assemblyToSearch = Assembly.GetAssembly(GetType());
                _bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);
                if (null == _bitmap)
                {
                    assemblyToSearch = Assembly.GetCallingAssembly();
                    _bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);
                    if (null == _bitmap)
                    {
                        assemblyToSearch = Assembly.GetEntryAssembly();
                        _bitmap = GetBitmapResourceFromAssembly(assemblyToSearch);
                        if (null == _bitmap)
                        {
                            throw new FileNotFoundException("GIF Source was not found.", GifSource);
                        }
                    }
                }
            }

            if (PlayAnimation)
            {
                ImageAnimator.Animate(_bitmap, OnFrameChanged);
            }
        }

        private Bitmap GetBitmapResourceFromAssembly(Assembly assemblyToSearch)
        {
            var resourselist = assemblyToSearch.GetManifestResourceNames();
            {
                var searchName = $"{assemblyToSearch.FullName.Split(',')[0]}.{GifSource}";
                if (resourselist.Contains(searchName))
                {
                    var bitmapStream = assemblyToSearch.GetManifestResourceStream(searchName);
                    if (null != bitmapStream)
                        return (Bitmap) System.Drawing.Image.FromStream(bitmapStream);
                }
            }
            return null;
        }

        private static void GIFSource_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((GifImageControl) d).SetImageGifSource();
        }


        private void OnFrameChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new OnFrameChangedDelegate(OnFrameChangedInMainThread));
        }

        private void OnFrameChangedInMainThread()
        {
            if (PlayAnimation && _bitmap != null)
            {
                ImageAnimator.UpdateFrames(_bitmap);
                Source = GetBitmapSource(_bitmap);
                InvalidateVisual();
            }
        }


        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        public static extern IntPtr DeleteObject(IntPtr hDc);

        private static BitmapSource GetBitmapSource(Bitmap gdiBitmap)
        {
            var hBitmap = gdiBitmap.GetHbitmap();
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(hBitmap);
            return bitmapSource;
        }

        #region Nested type: OnFrameChangedDelegate

        private delegate void OnFrameChangedDelegate();

        #endregion
    }
}