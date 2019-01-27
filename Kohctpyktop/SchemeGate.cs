using System.Collections.Generic;

namespace Kohctpyktop
{
    public class SchemeGate
    {
        /// <summary>
        /// One or two inputs OR'ed 
        /// </summary>
        public List<SchemeNode> GateInputs { get; set; } 
        /// <summary>
        /// The exactly 2 nodes connected or disconnected by the gate
        /// </summary>
        public List<SchemeNode> GatePowerNodes { get; set; } 
        /// <summary>
        /// true if the gate is open if and only if the input is low (PNP)
        /// otherwise the gate is open if and only if the input is high (NPN)
        /// </summary>
        public bool IsInversionGate { get; set; }
        /// <summary>
        /// The current state of the gate
        /// </summary>
        public bool IsOpen { get; set; }
        /// <summary>
        /// The state of the gate during the previous simulation step
        /// </summary>
        public bool WasOpen { get; set; }
    }
}