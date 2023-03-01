using System;
using System.Collections.Generic;
using System.Text;

namespace FileSharingSystem.Data.Entities
{
    class Peer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string FileName { get; set; }
        public string hashValue{ get; set; }

    }
}
