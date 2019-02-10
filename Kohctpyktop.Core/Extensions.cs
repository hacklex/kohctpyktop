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
        
        public static SiliconTypes RemoveGate(this SiliconTypes type)
        {
            switch (type)
            {
                case SiliconTypes.NTypeHGate:
                case SiliconTypes.NTypeVGate:
                    return SiliconTypes.NType;
                case SiliconTypes.PTypeHGate:
                case SiliconTypes.PTypeVGate:
                    return SiliconTypes.PType;
                default:
                    return type;
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
    }
}