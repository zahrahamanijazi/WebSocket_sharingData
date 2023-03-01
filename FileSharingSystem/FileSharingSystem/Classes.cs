using System;
using System.Collections.Generic;
using System.Text;

namespace FileSharingSystem
{
    public class RequestFile
    {
        public string RequestUrl { get; set; }
        public string FileName { get; set; }
        public int PrimeNumber { get; set; }
        public int PublicKey { get; set; }
        public string FileOwnerUrl { get; set; }
    }

    public class FileResult
    {
        public int PrimeNumber { get; set; }
        public int PublicKey { get; set; }
        public string FileData { get; set; }
    }

    public class ErrorMessage
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class DisconnectedClient
    {
        public string MyUrl { get; set; }
    }

    public class clsRemove
    {
        public Node ND { get; set; }
        public string url { get; set; }
    }

}
