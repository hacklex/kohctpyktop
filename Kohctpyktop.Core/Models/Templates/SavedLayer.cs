using Kohctpyktop.Models.Field;

namespace Kohctpyktop.Models.Templates
{
    public class SavedLayer
    {
        public SavedLayer(LayerTemplate template, LayerData data)
        {
            Template = template;
            Data = data;
        }

        public LayerTemplate Template { get; }
        public LayerData Data { get; }
    }
}