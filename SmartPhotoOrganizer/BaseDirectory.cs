using System.Collections.Generic;

namespace SmartPhotoOrganizer
{
    public class BaseDirectory
    {
        public string Path { get; set; }
        public List<string> Exclusions { get; set; }
        public bool Missing { get; set; }

        public BaseDirectory()
        {
            Exclusions = new List<string>();
        }
    }
}
