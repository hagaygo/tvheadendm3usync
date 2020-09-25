using System;
using System.Collections.Generic;
using System.Text;

namespace TVHeadEndM3USync.TVHeadEnd.Model
{
    class Mux : ModelBase
    {
        public string NetworkName { get; set; }
        public string NetworkUUID { get; set; }
        public string Url { get; set; }
    }
}
