using System;

namespace Slack.NetStandard.AsyncEnumerable
{
    public class SerializationProblem
    {
        public SerializationProblem(string input, Exception ex)
        {
            JsonInput = input;
            Exception = ex;
        }

        public Exception Exception { get; set; }
        public string JsonInput { get; set; }
    }
}