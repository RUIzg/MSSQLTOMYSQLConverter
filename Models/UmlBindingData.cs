using System.Collections.Generic;

namespace RokonoDbManager.Models
{
    public class UmlBindingData
    {
        public List<OutboundTableDto> Tables { get; set; }
        public List<OutboundTableConnection> Connections { get; set; }
    }
}