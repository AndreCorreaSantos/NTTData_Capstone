using UnityEngine;

public static class ColorConversion
{
    private static Vector3 RGBToXYZ(Color color)
    {
        float r = PivotRgb(color.r);
        float g = PivotRgb(color.g);
        float b = PivotRgb(color.b);

        float x = r * 0.4124564f + g * 0.3575761f + b * 0.1804375f;
        float y = r * 0.2126729f + g * 0.7151522f + b * 0.0721750f;
        float z = r * 0.0193339f + g * 0.1191920f + b * 0.9503041f;

        return new Vector3(x, y, z);
    }

    public static Vector3 XYZToCIELAB(Vector3 xyz)
    {
        float ref_X = 0.95047f;
        float ref_Y = 1.00000f;
        float ref_Z = 1.08883f;

        float x = xyz.x / ref_X;
        float y = xyz.y / ref_Y;
        float z = xyz.z / ref_Z;

        x = PivotXYZ(x);
        y = PivotXYZ(y);
        z = PivotXYZ(z);

        float l = 116f * y - 16f;
        float a = 500f * (x - y);
        float b = 200f * (y - z);

        return new Vector3(l, a, b);
    }

    private static Vector3 CIELABToXYZ(Vector3 lab)
    {
        float ref_X = 0.95047f;
        float ref_Y = 1.00000f;
        float ref_Z = 1.08883f;

        float y = (lab.x + 16f) / 116f;
        float x = lab.y / 500f + y;
        float z = y - lab.z / 200f;

        float x3 = Mathf.Pow(x, 3);
        float y3 = Mathf.Pow(y, 3);
        float z3 = Mathf.Pow(z, 3);

        x = (x3 > 0.008856f) ? x3 : (x - 16f / 116f) / 7.787f;
        y = (y3 > 0.008856f) ? y3 : (y - 16f / 116f) / 7.787f;
        z = (z3 > 0.008856f) ? z3 : (z - 16f / 116f) / 7.787f;

        x *= ref_X;
        y *= ref_Y;
        z *= ref_Z;

        return new Vector3(x, y, z);
    }

    private static Color XYZToRGB(Vector3 xyz)
    {
        float x = xyz.x;
        float y = xyz.y;
        float z = xyz.z;

        float r = x * 3.2404542f - y * 1.5371385f - z * 0.4985314f;
        float g = -x * 0.9692660f + y * 1.8760108f + z * 0.0415560f;
        float b = x * 0.0556434f - y * 0.2040259f + z * 1.0572252f;

        r = r > 0.0031308f ? 1.055f * Mathf.Pow(r, 1 / 2.4f) - 0.055f : 12.92f * r;
        g = g > 0.0031308f ? 1.055f * Mathf.Pow(g, 1 / 2.4f) - 0.055f : 12.92f * g;
        b = b > 0.0031308f ? 1.055f * Mathf.Pow(b, 1 / 2.4f) - 0.055f : 12.92f * b;

        return new Color(Mathf.Clamp01(r), Mathf.Clamp01(g), Mathf.Clamp01(b));
    }

    public static Color CIELABToRGB(Vector3 lab)
    {
        Vector3 xyz = CIELABToXYZ(lab);
        return XYZToRGB(xyz);
    }

    public static Vector3 RGBToCIELAB(Color color)
    {
        Vector3 xyz = RGBToXYZ(color);
        return XYZToCIELAB(xyz);
    }

    private static float PivotRgb(float n)
    {
        return (n > 0.04045f) ? Mathf.Pow((n + 0.055f) / 1.055f, 2.4f) : n / 12.92f;
    }

    private static float PivotXYZ(float n)
    {
        return (n > 0.008856f) ? Mathf.Pow(n, 1f / 3f) : (7.787f * n) + (16f / 116f);
    }
}
