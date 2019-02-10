namespace Kohctpyktop.Models.Field
{
    public static class CellExts
    {
        public static bool IsBaseN(this ILayerCell cell) =>
            cell.Silicon.IsAmong(SiliconTypes.NType, SiliconTypes.NTypeVia, SiliconTypes.NTypeHGate, SiliconTypes.NTypeVGate);
        
        public static bool IsBaseP(this ILayerCell cell) =>
            cell.Silicon.IsAmong(SiliconTypes.PType, SiliconTypes.PTypeVia, SiliconTypes.PTypeHGate, SiliconTypes.PTypeVGate);
        
        public static bool HasGate(this ILayerCell cell) =>
            cell.Silicon.IsAmong(SiliconTypes.NTypeHGate, SiliconTypes.NTypeVGate, SiliconTypes.PTypeHGate, SiliconTypes.PTypeVGate);
        
        public static bool HasNGate(this ILayerCell cell) =>
            cell.Silicon.IsAmong(SiliconTypes.NTypeHGate, SiliconTypes.NTypeVGate);
        
        public static bool HasPGate(this ILayerCell cell) =>
            cell.Silicon.IsAmong(SiliconTypes.PTypeHGate, SiliconTypes.PTypeVGate);
        
        public static bool HasN(this ILayerCell cell) =>
            cell.Silicon.IsAmong(SiliconTypes.NType, SiliconTypes.NTypeVia);
        
        public static bool HasP(this ILayerCell cell) =>
            cell.Silicon.IsAmong(SiliconTypes.PType, SiliconTypes.PTypeVia);
        
        public static bool HasVia(this ILayerCell cell) =>
            cell.Silicon.IsAmong(SiliconTypes.NTypeVia, SiliconTypes.PTypeVia);
        
        public static bool HasSilicon(this ILayerCell cell) =>
            cell.Silicon != SiliconTypes.None;
        
        public static bool IsVerticalGate(this ILayerCell cell) =>
            cell.Silicon == SiliconTypes.NTypeVGate || cell.Silicon == SiliconTypes.PTypeVGate;
    }
}