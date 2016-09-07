using System.Collections.Generic;

namespace SmartPhotoOrganizer
{
    public class DirectorySet
    {
        public List<BaseDirectory> BaseDirectories { get; set; }

        public DirectorySet()
        {
            BaseDirectories = new List<BaseDirectory>();
        }
    }
}
