using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace events_forwarding
{
    [DataContract]
    class SensorEvent
    {
        [DataMember]
        public string timestart { get; set; }

        [DataMember]
        public string dsplalert { get; set; }

        [DataMember]
        public string alerttype { get; set; }

        [DataMember]
        public string message { get; set; }

        [DataMember]
        public string targetalarmdevice { get; set; }
    }
}
