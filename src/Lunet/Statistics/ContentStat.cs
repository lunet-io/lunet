using System;
using Lunet.Runtime;

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

        public TimeSpan LoadingParsingDuration { get; set; }

        public TimeSpan EvaluateDuration { get; set; }

        public TimeSpan OutputDuration { get; set; }
    }
}