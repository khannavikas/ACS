namespace Recording
{
    public class RecordingFileStatusUpdatedDataPayload
    {
       public Data Data { get; set; }
    }

    public class Data
    {
        public RecordingStorageInfo RecordingStorageInfo { get; set; }
        public string RecordingStartTime { get; set; }
    }

    public class RecordingChunk
    {
        public string DocumentId { get; set; }
        public int Index { get; set; }
        public string ContentLocation { get; set; }
    }

    public class RecordingStorageInfo
    {
        public RecordingChunk[] RecordingChunks { get; set; }
    }
}
