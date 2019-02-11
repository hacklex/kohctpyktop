namespace Kohctpyktop.Models.Field
{
    public struct LinkContent
    {
        public LinkContent(SiliconLink siliconLink, bool hasMetalLink)
        {
            SiliconLink = siliconLink;
            HasMetalLink = hasMetalLink;
        }
            
        public SiliconLink SiliconLink { get; }
        public bool HasMetalLink { get; }
    }
}