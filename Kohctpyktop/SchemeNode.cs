using System.Collections.Generic;

namespace Kohctpyktop
{
    public class SchemeNode
    { 
        /// <summary>
        /// The previous state of the node in the simulation
        /// </summary>
        public bool WasHigh { get; set; }
        /// <summary>
        /// The current state of the node in the simulation
        /// </summary>
        public bool IsHigh { get; set; }
        public List<ElementaryPlaceLink> AssociatedPlaces { get; set; } = new List<ElementaryPlaceLink>();
    }
}