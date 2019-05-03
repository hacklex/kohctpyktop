using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kohctpyktop.Models.Field;

namespace Kohctpyktop
{
    public static class Extensions
    {
        public static bool IsAmong<T>(this T x, params T[] values) =>
            values.Contains(x);
        public static bool IsAmong<T>(this T x, IEnumerable<T> values) =>
            values.Contains(x);

        public static int ManhattanDistance(this (int x, int y) a, (int x, int y) b) => Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

        public static byte AsByte(this string byteHex)
        {
            if(byteHex.Length>2) throw new ArgumentException("Only for bytes");
            if (byteHex.Length < 1) throw new ArgumentException("Empty string!");
            const string alp = "0123456789abcdef";
            var lower = byteHex.ToLower();
            if (lower.Length == 1) return (byte) alp.IndexOf(lower[0]);
            return (byte) ( 16 * alp.IndexOf(lower[0]) +alp.IndexOf(lower[1]));
        }
        
        public static SiliconLayerContent RemoveGate(this SiliconLayerContent layerContent)
        {
            switch (layerContent)
            {
                case SiliconLayerContent.NTypeHGate:
                case SiliconLayerContent.NTypeVGate:
                    return SiliconLayerContent.NType;
                case SiliconLayerContent.PTypeHGate:
                case SiliconLayerContent.PTypeVGate:
                    return SiliconLayerContent.PType;
                default:
                    return layerContent;
            }
        }

        public static (Side, Side) GetPerpendicularSides(this Side side)
        {
            switch (side)
            {
                case Side.Left:
                case Side.Right:
                    return (Side.Top, Side.Bottom);
                case Side.Top:
                case Side.Bottom:
                    return (Side.Left, Side.Right);
                default: throw new ArgumentException(nameof(side));
            }
        }

        public static Side Invert(this Side side) => (Side) (((int) side + 2) % 4);

        public static bool IsVertical(this Side side) => side == Side.Top || side == Side.Bottom;

        public static SiliconLayerContent ConvertToGate(this SiliconType slcBase, bool isVertical)
        {
            switch (slcBase)
            {
                case SiliconType.NType: return isVertical ? SiliconLayerContent.NTypeVGate : SiliconLayerContent.NTypeHGate;
                case SiliconType.PType: return isVertical ? SiliconLayerContent.PTypeVGate : SiliconLayerContent.PTypeHGate;
                default: throw new ArgumentException(nameof(slcBase));
            }
        }
        
        public static SiliconLayerContent AddVia(this SiliconLayerContent silicon)
        {
            switch (silicon)
            {
                case SiliconLayerContent.NType: return SiliconLayerContent.NTypeVia;
                case SiliconLayerContent.PType: return SiliconLayerContent.PTypeVia;
                default: throw new ArgumentException(nameof(silicon));
            }
        }
        
        public static SiliconLayerContent RemoveVia(this SiliconLayerContent silicon)
        {
            switch (silicon)
            {
                case SiliconLayerContent.NTypeVia: return SiliconLayerContent.NType;
                case SiliconLayerContent.PTypeVia: return SiliconLayerContent.PType;
                default: throw new ArgumentException(nameof(silicon));
            }
        }
        
        public static SiliconLink Invert(this SiliconLink link)
        {
            switch (link)
            {
                case SiliconLink.None:
                case SiliconLink.BiDirectional:
                    return link;
                case SiliconLink.Slave:
                    return SiliconLink.Master;
                case SiliconLink.Master:
                    return SiliconLink.Slave;
                default:
                    throw new ArgumentException(nameof(link));
            }
        }

        public static LinkContent Invert(this LinkContent content)
        {
            return new LinkContent(content.SiliconLink.Invert(), content.HasMetalLink);
        }
    }
}