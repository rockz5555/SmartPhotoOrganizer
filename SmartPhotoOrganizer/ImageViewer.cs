using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartPhotoOrganizer
{
    public class ImageViewer
    {
        private const int BgSamplingDepth = 2;
        public BitmapImage Image { get; }
        public int ImageId { get; }
        public int Rotation { get; }
        public Brush BgColor { get; }
        public bool IsAnimated { get; }
        public bool FileExists { get; set; } = true;
        public bool Corrupted { get; set; }

        public ImageViewer(int imageId, double currentAspectRatio, SQLiteConnection connection)
        {
            ImageId = imageId;
            string imageName;
            var getImageCommand = new SQLiteCommand("SELECT name FROM images WHERE id = " + imageId, connection);

            using (var reader = getImageCommand.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return;
                }
                imageName = reader.GetString("name");
            }
            try
            {
                if (imageName.ToLowerInvariant().EndsWith(".gif") && File.Exists(imageName))
                {
                    System.Drawing.Bitmap gifBitmap = null;

                    try
                    {
                        gifBitmap = new System.Drawing.Bitmap(imageName);
                        var dimension = new System.Drawing.Imaging.FrameDimension(gifBitmap.FrameDimensionsList[0]);
                        var frameCount = gifBitmap.GetFrameCount(dimension);

                        if (frameCount > 1)
                        {
                            IsAnimated = true;
                        }
                    }
                    catch (ArgumentException)
                    {
                        // Thrown when file does not exist when creating Bitmap.
                        FileExists = false;
                        return;
                    }
                    finally
                    {
                        if (gifBitmap != null)
                        {
                            gifBitmap.Dispose();
                        }
                    }
                }

                if (IsAnimated) return;
                try
                {
                    // Create a BitmapFrame object for reading metadata.
                    using (var bitmapFrameStream = new FileStream(imageName, FileMode.Open, FileAccess.Read))
                    {
                        var frame = BitmapFrame.Create(bitmapFrameStream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);

                        // If this cannot cache the image, it will not read metadata.
                        // This will lock the image for an indeterminate amount of time.

                        // Determine if the image needs to be rotated
                        if (frame?.Metadata != null)
                        {
                            var metadata = (BitmapMetadata) frame.Metadata;
                            var rotationQuery = "/app1/ifd/{ushort=274}";
                            var orientationCode = 0;

                            if (metadata.ContainsQuery(rotationQuery))
                            {
                                var query = metadata.GetQuery(rotationQuery);
                                if (query != null)
                                    orientationCode = (ushort) query;
                            }

                            switch (orientationCode)
                            {
                                case 1:
                                    break;
                                case 3:
                                    Rotation = 180;
                                    break;
                                case 6:
                                    Rotation = 90;
                                    break;
                                case 8:
                                    Rotation = 270;
                                    break;
                            }
                        }
                    }

                    // Load the image into memory
                    Image = new BitmapImage();
                    Image.BeginInit();
                    Image.CacheOption = BitmapCacheOption.OnLoad;
                    Image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

                    Image.UriSource = new Uri(imageName);
                    Image.EndInit();
                    Image.Freeze();

                    // Find the background color for the image
                    double imageAspectRatio;
                    if (Rotation == 0 || Rotation == 180)
                    {
                        imageAspectRatio = ((double) Image.PixelWidth)/Image.PixelHeight;
                    }
                    else
                    {
                        imageAspectRatio = ((double) Image.PixelHeight)/Image.PixelWidth;
                    }

                    var sideSpace = currentAspectRatio > imageAspectRatio;
                    bool fromSides;

                    // If the image needs to be rotated 90 or 270, we must summarize from the other orientation.
                    if (Rotation == 0 || Rotation == 180)
                    {
                        fromSides = sideSpace;
                    }
                    else
                    {
                        fromSides = !sideSpace;
                    }

                    BgColor = GetBufferColor(Image, fromSides);
                }
                catch (NotSupportedException)
                {
                    Corrupted = true;
                    Image = null;
                }
                catch (FileFormatException)
                {
                    Corrupted = true;
                    Image = null;
                }
                catch (ArgumentException)
                {
                    Corrupted = true;
                    Image = null;
                }
                catch (TargetInvocationException e)
                {
                    MessageBox.Show("imageName: " + imageName + Environment.NewLine + e);
                    Corrupted = true;
                    Image = null;
                }
            }
            catch (FileNotFoundException)
            {
                // If the file isn't here anymore, just return an empty ViewerImage.
                FileExists = false;
            }
        }

        private static Brush GetBufferColor(BitmapImage image, bool fromSides)
        {
            return GetBufferColorSub(image, BgSamplingDepth, fromSides);
        }

        private static Brush GetBufferColorSub(BitmapImage image, int bgSamplingDepth, bool fromSides)
        {
            var totalRed = 0;
            var totalGreen = 0;
            var totalBlue = 0;

            int totalPixels;

            Brush defaultColor = Brushes.Black;

            var pixelFormat = image.Format;
            var bytesPerPixel = pixelFormat.BitsPerPixel/8.0;

            if (fromSides)
            {
                if (image.Width < bgSamplingDepth)
                {
                    return defaultColor;
                }

                var stride = (int) Math.Ceiling(bytesPerPixel*bgSamplingDepth);
                Array buffer = new byte[stride*image.PixelHeight];

                image.CopyPixels(new Int32Rect(0, 0, bgSamplingDepth, image.PixelHeight), buffer, stride, 0);
                GetAverageColor(buffer, stride, bgSamplingDepth, ref totalRed, ref totalGreen, ref totalBlue, image);

                image.CopyPixels(
                    new Int32Rect(image.PixelWidth - bgSamplingDepth, 0, bgSamplingDepth, image.PixelHeight), buffer,
                    stride, 0);
                GetAverageColor(buffer, stride, bgSamplingDepth, ref totalRed, ref totalGreen, ref totalBlue, image);

                totalPixels = bgSamplingDepth*image.PixelHeight*2;
            }
            else
            {
                if (image.Height < bgSamplingDepth)
                {
                    return defaultColor;
                }

                var stride = (int) Math.Ceiling(bytesPerPixel*image.PixelWidth);
                Array buffer = new byte[stride*bgSamplingDepth];
                image.CopyPixels(new Int32Rect(0, 0, image.PixelWidth, bgSamplingDepth), buffer, stride, 0);
                GetAverageColor(buffer, stride, image.PixelWidth, ref totalRed, ref totalGreen, ref totalBlue, image);

                image.CopyPixels(
                    new Int32Rect(0, image.PixelHeight - bgSamplingDepth, image.PixelWidth, bgSamplingDepth), buffer,
                    stride, 0);
                GetAverageColor(buffer, stride, image.PixelWidth, ref totalRed, ref totalGreen, ref totalBlue, image);

                totalPixels = image.PixelWidth*bgSamplingDepth*2;
            }

            var finalColor =
                new SolidColorBrush(Color.FromRgb((byte) (totalRed/totalPixels), (byte) (totalGreen/totalPixels),
                    (byte) (totalBlue/totalPixels)));
            finalColor.Freeze();

            return finalColor;
        }

        private static void GetAverageColor(Array data, int stride, int pixelWidth, ref int totalRed, ref int totalGreen, ref int totalBlue, BitmapImage image)
        {
            var pixelFormat = image.Format;
            var bytes = data as byte[];

            if (pixelFormat == PixelFormats.Bgr32 || pixelFormat == PixelFormats.Bgra32)
            {
                for (var i = 0; i < bytes.Length; i += 4)
                {
                    totalBlue += bytes[i];
                    totalGreen += bytes[i + 1];
                    totalRed += bytes[i + 2];
                }
            }
            else if (pixelFormat == PixelFormats.Gray8)
            {
                for (var i = 0; i < bytes.Length; i++)
                {
                    totalBlue += i;
                    totalGreen += i;
                    totalRed += i;
                }
            }
            else if (pixelFormat == PixelFormats.Indexed8)
            {
                if (bytes == null) return;
                foreach (var t in bytes)
                {
                    var colorIndex = (int) t;

                    if (image.Palette == null) continue;
                    var pixelColor = image.Palette.Colors[colorIndex];
                    totalBlue += pixelColor.B;
                    totalGreen += pixelColor.G;
                    totalRed += pixelColor.R;
                }
            }
            else if (pixelFormat == PixelFormats.Indexed4)
            {
                for (var offset = 0; offset < bytes.Length; offset += stride)
                {
                    for (var i = 0; i < pixelWidth; i++)
                    {
                        var byteIndex = i/2;
                        var firstHalf = i%2 == 0;
                        var indexByte = bytes[offset + byteIndex];
                        int colorIndex;

                        if (firstHalf)
                        {
                            colorIndex = indexByte >> 4;
                        }
                        else
                        {
                            colorIndex = indexByte & 0x0F;
                        }

                        if (image.Palette == null) continue;

                        var pixelColor = image.Palette.Colors[colorIndex];
                        totalBlue += pixelColor.B;
                        totalGreen += pixelColor.G;
                        totalRed += pixelColor.R;
                    }
                }
            }
        }
    }
}