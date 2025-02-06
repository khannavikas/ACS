using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recording
{
    public class Conversations
    {
        public Conversationitem[] conversationItems { get; set; }
        
    }

    public class Conversationitem
    {
        public string text { get; set; }
        public string id { get; set; }
        public string role { get; set; }
        public string participantId { get; set; }
    }

    public class ChapterRequest
    {
        public string displayName { get; set; }
        public Analysisinput analysisInput { get; set; }
        public ConvTask[] tasks { get; set; }
    }

    public class Analysisinput
    {
        public Conversation[] Conversations { get; set; }
    }

    public class Conversation
    {
        public Conversationitem[] conversationItems { get; set; }
        public string modality { get; set; }
        public string id { get; set; }
        public string language { get; set; }
    }

    public class ConvTask
    {
        public string taskName { get; set; }
        public string kind { get; set; }
        public Parameters parameters { get; set; }
    }

    public class Parameters
    {
        public string[] summaryAspects { get; set; }
    }

}
