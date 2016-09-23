using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace SmartPhotoOrganizer
{
    public static class Utilities
    {
        private static readonly Brush LightGreen = new SolidColorBrush(Color.FromArgb(255, 157, 235, 137));
        private static readonly Brush DarkGreen = new SolidColorBrush(Color.FromArgb(255, 67, 224, 0));
        private const string RandomChars = "0123456789abcdefghijklmnopqrstuvwxyz";

        public static Brush GetColorFromRating(int rating)
        {
            switch (rating)
            {
                case 1:
                    return Brushes.Red;
                case 2:
                    return Brushes.Orange;
                case 3:
                    return Brushes.Yellow;
                case 4:
                    return LightGreen;
                case 5:
                    return DarkGreen;
                default:
                    return Brushes.Purple; // Represent an error
            }
        }

        public static string GetRandomString(int length)
        {
            var randomStringBuilder = new StringBuilder();
            var random = new Random();

            for (var i = 0; i < length; i++)
            {
                randomStringBuilder.Append(RandomChars[random.Next(RandomChars.Length)]);
            }
            return randomStringBuilder.ToString();
        }

        public static List<string> ListFromPipeList(string pipeList)
        {
            if (pipeList == null || pipeList.Length < 2)
            {
                return new List<string>();
            }
            var parts = pipeList.Substring(1, pipeList.Length - 2).Split('|');

            return new List<string>(parts);
        }

        public static string PipeListFromList(IEnumerable<string> list)
        {
            var builder = new StringBuilder();
            var tagsAdded = 0;

            foreach (var item in list)
            {
                if (!item.Contains(",") && !item.Contains("|"))
                {
                    builder.Append("|");
                    builder.Append(item.Trim());
                    tagsAdded++;
                }
            }
            if (tagsAdded > 0)
            {
                builder.Append("|");
            }
            return builder.ToString();
        }
    }
}
