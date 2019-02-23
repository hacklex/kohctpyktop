using System.Collections.Generic;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Topology
{
    // todo: make object immutable 
    public class SchemeNode
    {
        public List<ElementaryPlaceLink> AssociatedPlaces { get; set; } = new List<ElementaryPlaceLink>();
        /// <summary>
        /// If there are more than 1 pin, user is probably doing it wrong...
        /// </summary>
        public HashSet<Pin> Pins { get; } = new HashSet<Pin>();
    }
}    