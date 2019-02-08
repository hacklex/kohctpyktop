using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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

        public static void DrawLineEx(this Graphics g, Pen p, float x1, float y1, float x2, float y2)
        {
            var old = g.CompositingQuality;
            g.CompositingQuality = CompositingQuality.AssumeLinear;
            g.DrawLine(p, x1, y1, x2, y2);
            g.DrawLine(p, x2, y2, x1, y1);
            g.CompositingQuality = old;
        }
        public static Color AsDColor(this string hex)
        {
            if (hex.Length == 3) //RGB
            {
                return Color.FromArgb(
                    new string(hex[0], 2).AsByte(), 
                    new string(hex[1], 2).AsByte(),
                    new string(hex[2], 2).AsByte());
            }
            if (hex.Length == 4) //ARGB
            {
                return Color.FromArgb(
                    new string(hex[0], 2).AsByte(), 
                    new string(hex[1], 2).AsByte(), 
                    new string(hex[2], 2).AsByte(),
                    new string(hex[3], 2).AsByte());
            }
            if (hex.Length == 6) //RRGGBB
            {
                return Color.FromArgb(
                    hex.Substring(0, 2).AsByte(), 
                    hex.Substring(2, 2).AsByte(), 
                    hex.Substring(4, 2).AsByte());
            }
            if (hex.Length == 8) //AARRGGBB
            {
                return Color.FromArgb(
                    hex.Substring(0, 2).AsByte(), 
                    hex.Substring(2, 2).AsByte(), 
                    hex.Substring(4, 2).AsByte(), 
                    hex.Substring(6, 2).AsByte());
            }
            throw new ArgumentException("Expected a hex string of length 3, 4, 6 or 8");
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
    }
}