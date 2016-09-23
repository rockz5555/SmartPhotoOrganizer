using System.Linq;
using SmartPhotoOrganizer.DatabaseOp;
using SmartPhotoOrganizer.DataStructures;

namespace SmartPhotoOrganizer
{
    public static class ImageCache
    {
        public const int NumImagesToCacheAhead = 1;

        private static CachedImage[] _cachedImages;

        static ImageCache()
        {
            Clear();
        }

        public static CachedImage Current
        {
            get
            {
                return _cachedImages[NumImagesToCacheAhead];
            }
            set
            {
                _cachedImages[NumImagesToCacheAhead] = value;
            }
        }

        public static CachedImage Get(int index)
        {
            return _cachedImages[index + NumImagesToCacheAhead];
        }

        public static void Clear()
        {
            _cachedImages = new CachedImage[NumImagesToCacheAhead * 2 + 1];
            for (var i = 0; i < _cachedImages.Length; i++)
            {
                _cachedImages[i] = new CachedImage();
            }
        }

        public static void ShiftLeft()
        {
            for (var i = 0; i < _cachedImages.Length - 1; i++)
            {
                _cachedImages[i] = _cachedImages[i + 1];
            }
            _cachedImages[_cachedImages.Length - 1] = new CachedImage();
        }

        public static void ShiftRight()
        {
            for (var i = _cachedImages.Length - 2; i >= 0; i--)
            {
                _cachedImages[i + 1] = _cachedImages[i];
            }
            _cachedImages[0] = new CachedImage();
        }

        public static void DeleteCurrent(bool lastInList)
        {
            if (lastInList)
            {
                // If this is the last image in the list, move up images behind us.
                for (var i = NumImagesToCacheAhead; i > 0; i--)
                {
                    _cachedImages[i] = _cachedImages[i - 1];
                }
                _cachedImages[0] = new CachedImage();
            }
            else
            {
                // If this is not the last image in the list, move back images ahead of us.
                for (var i = NumImagesToCacheAhead; i < NumImagesToCacheAhead * 2; i++)
                {
                    _cachedImages[i] = _cachedImages[i + 1];
                }
                _cachedImages[NumImagesToCacheAhead * 2] = new CachedImage();
            }
        }

        public static bool ImageIsInCache(string imagePath)
        {
            return (from cachedImage in _cachedImages where cachedImage.Image != null select Database.GetImageName(PhotoManager.Connection, cachedImage.Image.ImageId)).Any(cachedImageName => imagePath.ToLowerInvariant() == cachedImageName.ToLowerInvariant());
        }
    }
}
