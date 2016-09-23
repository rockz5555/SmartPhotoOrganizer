using System.Collections.Generic;
using System.Windows.Controls;

namespace SmartPhotoOrganizer.InputRelated
{
    public class EditKeyRow
    {
        public UserAction Action { get; set; }
        public List<TextBlock> KeyBlocks { get; } = new List<TextBlock>();
    }
}
