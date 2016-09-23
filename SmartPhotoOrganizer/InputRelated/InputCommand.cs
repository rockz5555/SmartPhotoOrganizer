using System;

namespace SmartPhotoOrganizer.InputRelated
{
    public class InputCommand : IEquatable<InputCommand>
    {
        public int InputCode { get; set; }
        public InputType InputType { get; set; }

        public bool Equals(InputCommand other)
        {
            return InputCode == other.InputCode && InputType == other.InputType;
        }
    }
}
