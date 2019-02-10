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
    }
}