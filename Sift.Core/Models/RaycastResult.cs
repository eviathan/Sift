using Sift.Core.Enums;

namespace Sift.Core.Models
{
    public struct RaycastResult
    {
        public Guid NodeId { get; set; }        

        public Direction PortDirection { get; set; }
        
        public int Distance { get; set; }

        public long ArrivalTicks { get; set; }
    }
}