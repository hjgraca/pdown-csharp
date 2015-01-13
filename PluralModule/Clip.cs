using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluralModule
{
    public class Clip
    {
        public int clipIndex { get; set; }
        public string title { get; set; }
        public bool hasBeenViewed { get; set; }
        public string duration { get; set; }
        public string playerParameters { get; set; }
        public bool userMayViewClip { get; set; }
        public string clickActionDescription { get; set; }
        public bool isHighlighted { get; set; }
        public string name { get; set; }
        public bool isBookmarked { get; set; }
        public string hasBeenViewedImageUrl { get; set; }
        public string hasBeenViewedAltText { get; set; }
    }
}
