using System.Collections.Generic;
using System.Linq;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Topology
{
    // todo: make object immutable 
    public class SchemeNodeTemplate
    {
        public List<ElementaryPlaceLink> AssociatedPlaces { get; set; } = new List<ElementaryPlaceLink>();
        /// <summary>
        /// If there are more than 1 pin, user is probably doing it wrong...
        /// </summary>
        public HashSet<Pin> Pins { get; } = new HashSet<Pin>();
        
        public SchemeNode Freeze() => new SchemeNode(AssociatedPlaces.ToArray(), Pins.ToArray());
    }

    public class SchemeNode
    {
        public SchemeNode(IReadOnlyList<ElementaryPlaceLink> associatedPlaces, IReadOnlyCollection<Pin> pins)
        {
            AssociatedPlaces = associatedPlaces;
            Pins = pins;
        }

        public IReadOnlyList<ElementaryPlaceLink> AssociatedPlaces { get; }
        public IReadOnlyCollection<Pin> Pins { get; }
    }
}    