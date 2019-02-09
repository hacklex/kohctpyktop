namespace Kohctpyktop.Input
{
    public enum SelectedTool
    {
        Unknown, // workaround for avalonia bindings (bug or feature?)
        
        Metal,
        Silicon,
        AddOrDeleteVia,
        DeleteMetalOrSilicon,
        Selection,
        TopologyDebug
    }
}