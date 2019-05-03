using System.Runtime.InteropServices.ComTypes;

namespace Kohctpyktop.Models.Field
{
    public static class CellExts
    {
        public static bool IsBaseN(this ILayerCell cell) => HasN(cell) || HasNGate(cell);
        
        public static bool IsBaseP(this ILayerCell cell) => HasP(cell) || HasPGate(cell);

        public static bool HasGate(this ILayerCell cell) => HasNGate(cell) || HasPGate(cell);
        
        public static bool HasNGate(this ILayerCell cell) =>
            cell.Silicon == SiliconLayerContent.NTypeHGate || cell.Silicon == SiliconLayerContent.NTypeVGate;
        
        public static bool HasPGate(this ILayerCell cell) =>
            cell.Silicon == SiliconLayerContent.PTypeHGate || cell.Silicon == SiliconLayerContent.PTypeVGate;
        
        public static bool HasN(this ILayerCell cell) =>
            cell.Silicon == SiliconLayerContent.NType || cell.Silicon == SiliconLayerContent.NTypeVia;
        
        public static bool HasP(this ILayerCell cell) =>
            cell.Silicon == SiliconLayerContent.PType || cell.Silicon == SiliconLayerContent.PTypeVia;
        
        public static bool HasVia(this ILayerCell cell) =>
            cell.Silicon == SiliconLayerContent.NTypeVia || cell.Silicon == SiliconLayerContent.PTypeVia;
        
        public static bool HasSilicon(this ILayerCell cell) =>
            cell.Silicon != SiliconLayerContent.None;
        
        public static bool IsVerticalGate(this ILayerCell cell) =>
            cell.Silicon == SiliconLayerContent.NTypeVGate || cell.Silicon == SiliconLayerContent.PTypeVGate;
    }
}