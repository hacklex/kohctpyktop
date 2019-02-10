using System.Runtime.InteropServices.ComTypes;

namespace Kohctpyktop.Models.Field
{
    public static class CellExts
    {
        public static bool IsBaseN(this ILayerCell cell) => HasN(cell) || HasNGate(cell);
        
        public static bool IsBaseP(this ILayerCell cell) => HasP(cell) || HasPGate(cell);

        public static bool HasGate(this ILayerCell cell) => HasNGate(cell) || HasPGate(cell);
        
        public static bool HasNGate(this ILayerCell cell) =>
            cell.Silicon == SiliconTypes.NTypeHGate || cell.Silicon == SiliconTypes.NTypeVGate;
        
        public static bool HasPGate(this ILayerCell cell) =>
            cell.Silicon == SiliconTypes.PTypeHGate || cell.Silicon == SiliconTypes.PTypeVGate;
        
        public static bool HasN(this ILayerCell cell) =>
            cell.Silicon == SiliconTypes.NType || cell.Silicon == SiliconTypes.NTypeVia;
        
        public static bool HasP(this ILayerCell cell) =>
            cell.Silicon == SiliconTypes.PType || cell.Silicon == SiliconTypes.PTypeVia;
        
        public static bool HasVia(this ILayerCell cell) =>
            cell.Silicon == SiliconTypes.NTypeVia || cell.Silicon == SiliconTypes.PTypeVia;
        
        public static bool HasSilicon(this ILayerCell cell) =>
            cell.Silicon != SiliconTypes.None;
        
        public static bool IsVerticalGate(this ILayerCell cell) =>
            cell.Silicon == SiliconTypes.NTypeVGate || cell.Silicon == SiliconTypes.PTypeVGate;
    }
}