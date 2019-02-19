using System.Collections.Generic;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Topology
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
        /// <summary>
        /// If there are more than 1 pin, user is probably doing it wrong...
        /// </summary>
        public HashSet<Pin> Pins { get; } = new HashSet<Pin>();
    }
}    