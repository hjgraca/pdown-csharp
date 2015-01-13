using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluralModule
{
    public class Module
    {
        public bool userMayViewFirstClip { get; set; }
        public string moduleRef { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public string duration { get; set; }
        public bool hasBeenViewed { get; set; }
        public bool isHighlighted { get; set; }
        public string fragmentIdentifier { get; set; }
        public string firstClipLaunchClickHandler { get; set; }
        public bool userMayBookmark { get; set; }
        public bool isBookmarked { get; set; }
        public List<Clip> clips { get; set; }
        public string hasBeenViewedImageUrl { get; set; }
        public string hasBeenViewedAltText { get; set; }
    }
}
