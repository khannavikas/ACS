using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recording
{
    public class Transcript
    {
        public TranscriptItem[] TranscriptItemItems { get; set; }
    }

    public class TranscriptItem
    {
        public string id { get; set; }
        public string text { get; set; }
        public float confidence { get; set; }
        public string speakerId { get; set; }
        public string language { get; set; }
        public Instance[] instances { get; set; }
    }

    public class Instance
    {
        public string adjustedStart { get; set; }
        public string adjustedEnd { get; set; }
        public string start { get; set; }
        public string end { get; set; }
    }


}
