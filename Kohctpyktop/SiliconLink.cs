namespace Kohctpyktop
{
    /// <summary>
    /// All types of links for the silicon layer
    /// </summary>
    public enum SiliconLink
    {
        None,
        Master, //the signal of a gate
        Slave, //the power of a gate
        BiDirectional //a regular connection
    }
}