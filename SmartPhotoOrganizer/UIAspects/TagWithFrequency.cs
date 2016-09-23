namespace SmartPhotoOrganizer.UIAspects
{
    public class TagWithFrequency
    {
        public string Tag { get; set; }
        public int Frequency { get; set; }

        public string TagText => Tag + " (" + Frequency + ")";
    }
}
