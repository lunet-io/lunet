using System;
using Lunet.Core;

namespace Lunet.Statistics
{
    public class ContentStat
    {
        public ContentStat(ContentObject contentObject)
        {
            if (contentObject == null) throw new ArgumentNullException(nameof(contentObject));
            ContentObject = contentObject;
        }

        public ContentObject ContentObject { get; }

        public bool Static { get; set; }

        public long OutputBytes { get; set; }

        public TimeSpan LoadingParsingTime { get; set; }

        public TimeSpan EvaluateTime { get; set; }

        public TimeSpan OutputTime { get; set; }
    }
}