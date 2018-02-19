// https://github.com/aseprite/aseprite/blob/14ba0ab411f85fb44b2b52a5ae061f68efbc2bb5/src/doc/blend_funcs.cpp

using System;
using System.Drawing;

namespace Aseprite
{
    internal delegate T Blender<T>(T dst, T src, int opacity);

    internal static class Blending
    {
        #region Single-Channel Blending

        private static int MultiplyNormalizedToByte(int a, int b)
        {
            int t = a * b + 0x80;
            return ((t >> 8) + t) >> 8;
        }

        private static int DivideNormalizedToByte(int a, int b)
        {
            return (a * 0xFF + b / 2) / b;
        }

        public static int BlendAddition(int b, int s)
        {
            return Math.Min(b + s, 255);
        }

        public static int BlendSubtract(int b, int s)
        {
            return Math.Max(b - s, 0);
        }

        public static int BlendMultiply(int b, int s)
        {
            return MultiplyNormalizedToByte(b, s);
        }

        public static int BlendScreen(int b, int s)
        {
            return b + s - MultiplyNormalizedToByte(b, s);
        }

        public static int BlendOverlay(int b, int s)
        {
            return BlendHardLight(s, b);
        }

        public static int BlendDarken(int b, int s)
        {
            return Math.Min(b, s);
        }

        public static int BlendLighten(int b, int s)
        {
            return Math.Max(b, s);
        }

        public static int BlendHardLight(int b, int s)
        {
            if (s < 128)
                return BlendMultiply(b, s << 1);
            else
                return BlendScreen(b, (s << 1) - 255);
        }

        public static int BlendDifference(int b, int s)
        {
            return Math.Abs(b - s);
        }

        public static int BlendExclusion(int b, int s)
        {
            int t = MultiplyNormalizedToByte(b, s);
            return b + s - 2 * t;
        }

        public static int BlendDivide(int b, int s)
        {
            if (b == 0)
                return 0;
            else if (b >= s)
                return 255;
            else
                return DivideNormalizedToByte(b, s); // return b / s
        }

        public static int BlendColorDodge(int b, int s)
        {
            if (b == 0)
                return 0;

            s = (255 - s);
            if (b >= s)
                return 255;
            else
                return DivideNormalizedToByte(b, s); // return b / (1-s)
        }

        public static int BlendColorBurn(int b, int s)
        {
            if (b == 255)
                return 255;

            b = (255 - b);
            if (b >= s)
                return 0;
            else
                return 255 - DivideNormalizedToByte(b, s); // return 1 - ((1-b)/s)
        }

        public static int BlendSoftLight(int _b, int _s)
        {
            double b = _b / 255.0;
            double s = _s / 255.0;
            double r, d;

            if (b <= 0.25)
                d = ((16 * b - 12) * b + 4) * b;
            else
                d = Math.Sqrt(b);

            if (s <= 0.5)
                r = b - (1.0 - 2.0 * s) * b * (1.0 - b);
            else
                r = b + (2.0 * s - 1.0) * (d - b);

            return (int)(r * 255 + 0.5);
        }

        public static T BlendSrc<T>(T backdrop, T src, int opacity)
        {
            return src;
        }

        #endregion Single-Channel Blending

        #region RGB Blending

        private static Color BlendMerge(Color dst, Color src, int opacity)
        {
            int rr, rg, rb, ra;

            if (dst.A == 0)
            {
                rr = src.R;
                rg = src.G;
                rb = src.B;
            }
            else if (src.A == 0)
            {
                rr = dst.R;
                rg = dst.G;
                rb = dst.B;
            }
            else
            {
                rr = dst.R + MultiplyNormalizedToByte(src.R - dst.R, opacity);
                rg = dst.G + MultiplyNormalizedToByte(src.G - dst.G, opacity);
                rb = dst.B + MultiplyNormalizedToByte(src.B - dst.B, opacity);
            }
            ra = dst.A + MultiplyNormalizedToByte(src.A - dst.A, opacity);
            if (ra == 0)
                rr = rg = rb = 0;

            return Color.FromArgb(ra, rr, rg, rb);
        }

        static private int GetLuma(Color color)
        {
            return (int)(
                color.R * 0.2126 +
                color.G * 0.7152 +
                color.B * 0.0722);
        }

        private static Color BlendNegBW(Color dst, Color src, int opacity)
        {
            if (dst.A == 0)
                return Color.FromArgb(255, 0, 0, 0);
            else if (GetLuma(dst) < 128)
                return Color.FromArgb(255, 255, 255, 255);
            else
                return Color.FromArgb(255, 0, 0, 0);
        }

        private static Color BlendRedTint(Color dst, Color src, int opacity)
        {
            int v = GetLuma(src);
            src = Color.FromArgb(src.A, (255 + v) / 2, v / 2, v / 2);
            return BlendNormal(dst, src, opacity);
        }

        private static Color BlendBlueTint(Color dst, Color src, int opacity)
        {
            int v = GetLuma(src);
            src = Color.FromArgb(src.A, v / 2, v / 2, (255 + v) / 2);
            return BlendNormal(dst, src, opacity);
        }

        private static Color BlendNormal(Color dst, Color src, int opacity)
        {
            if (dst.A == 0)
                return Color.FromArgb(MultiplyNormalizedToByte(src.A, opacity), src.R, src.G, src.B);
            else if (src.A == 0)
                return dst;

            var sa = MultiplyNormalizedToByte(src.A, opacity);

            int ra = sa + dst.A - MultiplyNormalizedToByte(dst.A, sa);
            int rr = dst.R + (src.R - dst.R) * sa / ra;
            int rg = dst.G + (src.G - dst.G) * sa / ra;
            int rb = dst.B + (src.B - dst.B) * sa / ra;

            return Color.FromArgb(ra, rr, rg, rb);
        }

        private static Blender<Color> GetColorBlender(Func<int, int, int> blend)
            => (dst, src, opacity) =>
            {
                int r = blend(dst.R, src.R);
                int g = blend(dst.G, src.G);
                int b = blend(dst.B, src.B);
                src = Color.FromArgb(src.A, r, g, b);
                return BlendNormal(dst, src, opacity);
            };

        #endregion RGB Blending

        #region HSV Blending

        private static void ClipColor(ref double r, ref double g, ref double b)
        {
            double l = GetLum(r, g, b);
            double n = Math.Min(r, Math.Min(g, b));
            double x = Math.Max(r, Math.Max(g, b));

            if (n < 0)
            {
                r = l + (((r - l) * l) / (l - n));
                g = l + (((g - l) * l) / (l - n));
                b = l + (((b - l) * l) / (l - n));
            }

            if (x > 1)
            {
                r = l + (((r - l) * (1 - l)) / (x - l));
                g = l + (((g - l) * (1 - l)) / (x - l));
                b = l + (((b - l) * (1 - l)) / (x - l));
            }
        }

        private static double GetLum(double r, double g, double b)
        {
            return 0.3 * r + 0.59 * g + 0.11 * b;
        }

        private static double GetSat(double r, double g, double b)
        {
            return Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
        }

        private static void SetLum(ref double r, ref double g, ref double b, double l)
        {
            double d = l - GetLum(r, g, b);
            r += d;
            g += d;
            b += d;
            ClipColor(ref r, ref g, ref b);
        }

        private static void SetSat(ref double r, ref double g, ref double b, double s)
        {
            void Sat(ref double min, ref double mid, ref double max)
            {
                if (max > min)
                {
                    mid = ((mid - min) * s) / (max - min);
                    max = s;
                }
                else
                {
                    mid = 0;
                    max = 0;
                }
                min = 0;
            }

            if (r < g)
            {
                if (g < b)
                    Sat(ref r, ref g, ref b);
                else if (r < b)
                    Sat(ref r, ref b, ref g);
                else
                    Sat(ref b, ref r, ref g);
            }
            else
            {
                if (b < g)
                    Sat(ref b, ref g, ref r);
                else if (b < r)
                    Sat(ref g, ref b, ref r);
                else
                    Sat(ref g, ref r, ref b);
            }
        }

        private static Color BlendHue(Color dst, Color src, int opacity)
        {
            double r = dst.R / 255.0;
            double g = dst.G / 255.0;
            double b = dst.B / 255.0;
            double s = GetSat(r, g, b);
            double l = GetLum(r, g, b);

            r = src.R / 255.0;
            g = src.G / 255.0;
            b = src.B / 255.0;

            SetSat(ref r, ref g, ref b, s);
            SetLum(ref r, ref g, ref b, l);

            src = Color.FromArgb(src.A, (byte)(255.0 * r), (byte)(255.0 * g), (byte)(255.0 * b));
            return BlendNormal(dst, src, opacity);
        }

        private static Color BlendSaturation(Color dst, Color src, int opacity)
        {
            double r = src.R / 255.0;
            double g = src.G / 255.0;
            double b = src.B / 255.0;
            double s = GetSat(r, g, b);

            r = dst.R / 255.0;
            g = dst.G / 255.0;
            b = dst.B / 255.0;
            double l = GetLum(r, g, b);

            SetSat(ref r, ref g, ref b, s);
            SetLum(ref r, ref g, ref b, l);

            src = Color.FromArgb(src.A, (byte)(255.0 * r), (byte)(255.0 * g), (byte)(255.0 * b));
            return BlendNormal(dst, src, opacity);
        }

        private static Color BlendColor(Color dst, Color src, int opacity)
        {
            double r = dst.R / 255.0;
            double g = dst.G / 255.0;
            double b = dst.B / 255.0;
            double l = GetLum(r, g, b);

            r = src.R / 255.0;
            g = src.G / 255.0;
            b = src.B / 255.0;

            SetLum(ref r, ref g, ref b, l);

            src = Color.FromArgb(src.A, (byte)(255.0 * r), (byte)(255.0 * g), (byte)(255.0 * b));
            return BlendNormal(dst, src, opacity);
        }

        private static Color BlendLuminosity(Color dst, Color src, int opacity)
        {
            double r = src.R / 255.0;
            double g = src.G / 255.0;
            double b = src.B / 255.0;
            double l = GetLum(r, g, b);

            r = dst.R / 255.0;
            g = dst.G / 255.0;
            b = dst.B / 255.0;

            SetLum(ref r, ref g, ref b, l);

            src = Color.FromArgb(src.A, (byte)(255.0 * r), (byte)(255.0 * g), (byte)(255.0 * b));
            return BlendNormal(dst, src, opacity);
        }

        #endregion HSV Blending

        #region Gray Blending

        private static GrayColor BlendMerge(GrayColor dst, GrayColor src, int opacity)
        {
            int rk, ra;

            if (dst.A == 0)
            {
                rk = src.V;
            }
            else if (src.A == 0)
            {
                rk = dst.V;
            }
            else
            {
                rk = dst.V + MultiplyNormalizedToByte((src.V - dst.V), opacity);
            }
            ra = dst.A + MultiplyNormalizedToByte((src.A - dst.A), opacity);
            if (ra == 0)
                rk = 0;

            return new GrayColor(rk, ra);
        }

        private static GrayColor BlendNegBW(GrayColor dst, GrayColor src, int opacity)
        {
            if (dst.A == 0)
                return src;
            else if (dst.V < 128)
                return new GrayColor(255, 255);
            else
                return new GrayColor(0, 255);
        }

        private static GrayColor BlendNormal(GrayColor dst, GrayColor src, int opacity)
        {
            if (dst.A == 0)
                return new GrayColor(src.V, MultiplyNormalizedToByte(src.A, opacity));
            else if (src.A == 0)
                return dst;

            var sa = MultiplyNormalizedToByte(src.A, opacity);

            var ra = dst.A + sa - MultiplyNormalizedToByte(dst.A, sa);
            var rg = dst.V + (src.V - dst.V) * sa / ra;

            return new GrayColor(rg, ra);
        }

        private static Blender<GrayColor> GetGrayBlender(Func<int, int, int> blend)
            => (dst, src, opacity) =>
            {
                int v = blend(dst.V, src.V);
                src = new GrayColor(v, src.A);
                return BlendNormal(dst, src, opacity);
            };

        #endregion Gray Blending

        #region Blender Getters

        private static ArgumentException UnknownBlendModeException(BlendMode mode)
            => new ArgumentException($"The {nameof(BlendMode)} {mode} is not known.");

        public static Blender<Color> GetColorBlender(BlendMode mode)
        {
            switch (mode)
            {
                case BlendMode.Src:
                    return BlendSrc;

                case BlendMode.Merge:
                    return BlendMerge;

                case BlendMode.NegBW:
                    return BlendNegBW;

                case BlendMode.RedTint:
                    return BlendRedTint;

                case BlendMode.BlueTint:
                    return BlendBlueTint;

                case BlendMode.Normal:
                    return BlendNormal;

                case BlendMode.Addition:
                    return GetColorBlender(BlendAddition);

                case BlendMode.Subtract:
                    return GetColorBlender(BlendSubtract);

                case BlendMode.Multiply:
                    return GetColorBlender(BlendMultiply);

                case BlendMode.Divide:
                    return GetColorBlender(BlendDivide);

                case BlendMode.Screen:
                    return GetColorBlender(BlendScreen);

                case BlendMode.Overlay:
                    return GetColorBlender(BlendOverlay);

                case BlendMode.Darken:
                    return GetColorBlender(BlendDarken);

                case BlendMode.Lighten:
                    return GetColorBlender(BlendLighten);

                case BlendMode.ColorDodge:
                    return GetColorBlender(BlendColorDodge);

                case BlendMode.ColorBurn:
                    return GetColorBlender(BlendColorBurn);

                case BlendMode.HardLight:
                    return GetColorBlender(BlendHardLight);

                case BlendMode.SoftLight:
                    return GetColorBlender(BlendSoftLight);

                case BlendMode.Difference:
                    return GetColorBlender(BlendDifference);

                case BlendMode.Exclusion:
                    return GetColorBlender(BlendExclusion);

                case BlendMode.Hue:
                    return BlendHue;

                case BlendMode.Saturation:
                    return BlendSaturation;

                case BlendMode.Color:
                    return BlendColor;

                case BlendMode.Luminosity:
                    return BlendLuminosity;

                default:
                    throw UnknownBlendModeException(mode);
            }
        }

        public static Blender<GrayColor> GetGrayBlender(BlendMode mode)
        {
            switch (mode)
            {
                case BlendMode.Src:
                    return BlendSrc;

                case BlendMode.Merge:
                    return BlendMerge;

                case BlendMode.NegBW:
                    return BlendNegBW;

                case BlendMode.RedTint:
                    return BlendNormal;

                case BlendMode.BlueTint:
                    return BlendNormal;

                case BlendMode.Normal:
                    return BlendNormal;

                case BlendMode.Addition:
                    return GetGrayBlender(BlendAddition);

                case BlendMode.Subtract:
                    return GetGrayBlender(BlendSubtract);

                case BlendMode.Multiply:
                    return GetGrayBlender(BlendMultiply);

                case BlendMode.Divide:
                    return GetGrayBlender(BlendDivide);

                case BlendMode.Screen:
                    return GetGrayBlender(BlendScreen);

                case BlendMode.Overlay:
                    return GetGrayBlender(BlendOverlay);

                case BlendMode.Darken:
                    return GetGrayBlender(BlendDarken);

                case BlendMode.Lighten:
                    return GetGrayBlender(BlendLighten);

                case BlendMode.ColorDodge:
                    return GetGrayBlender(BlendColorDodge);

                case BlendMode.ColorBurn:
                    return GetGrayBlender(BlendColorBurn);

                case BlendMode.HardLight:
                    return GetGrayBlender(BlendHardLight);

                case BlendMode.SoftLight:
                    return GetGrayBlender(BlendSoftLight);

                case BlendMode.Difference:
                    return GetGrayBlender(BlendDifference);

                case BlendMode.Exclusion:
                    return GetGrayBlender(BlendExclusion);

                case BlendMode.Hue:
                    return BlendNormal;

                case BlendMode.Saturation:
                    return BlendNormal;

                case BlendMode.Color:
                    return BlendNormal;

                case BlendMode.Luminosity:
                    return BlendNormal;

                default:
                    throw UnknownBlendModeException(mode);
            }
        }

        public static Blender<byte> GetIndexBlender(BlendMode mode)
        {
            return BlendSrc;
        }

        #endregion Blender Getters
    }
}
