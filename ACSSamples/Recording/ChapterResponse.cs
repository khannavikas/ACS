using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recording
{

    public class ChapterResponse
    {
        public string jobId { get; set; }
        public DateTime lastUpdatedDateTime { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime expirationDateTime { get; set; }
        public string status { get; set; }
        public object[] errors { get; set; }
        public Tasks tasks { get; set; }
    }

    public class Tasks
    {
        public int completed { get; set; }
        public int failed { get; set; }
        public int inProgress { get; set; }
        public int total { get; set; }
        public Item[] items { get; set; }
    }

    public class Item
    {
        public string kind { get; set; }
        public string taskName { get; set; }
        public DateTime lastUpdateDateTime { get; set; }
        public string status { get; set; }
        public Results results { get; set; }
    }

    public class Results
    {
        public ResponseConversation[] conversations { get; set; }
        public object[] errors { get; set; }
        public string modelVersion { get; set; }
    }

    public class ResponseConversation
    {
        public Summary[] summaries { get; set; }
        public string id { get; set; }
        public object[] warnings { get; set; }
    }

    public class Summary
    {
        public string aspect { get; set; }
        public string text { get; set; }
        public Context[] contexts { get; set; }
    }

    public class Context
    {
        public string conversationItemId { get; set; }
        public int offset { get; set; }
        public int length { get; set; }
    }

}
