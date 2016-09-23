using System.Collections.Generic;
using System.Text;

namespace SmartPhotoOrganizer
{
    public class ImageUserData
    {
        public ImageUserData()
        {
            Tags = new List<string>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Tags { get; set; }
        public int Rating { get; set; }

        public string TagsString
        {
            get
            {
                var builder = new StringBuilder();

                for (var i = 0; i < Tags.Count; i++)
                {
                    builder.Append(Tags[i]);

                    if (i != Tags.Count - 1)
                    {
                        builder.Append(" ");
                    }
                }
                return builder.ToString();
            }
        }
    }
}
