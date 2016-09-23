namespace SmartPhotoOrganizer.DataStructures
{
    public class OrphanedData
    {
        public int ImageId { get; set; }
        public string Name { get; set; }
        public string Hash { get; set; }
        public string Tags { get; set; }
        public int Rating { get; set; }
        public bool FoundOwner { get; set; }
    }
}
