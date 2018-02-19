namespace Aseprite
{
    // https://github.com/aseprite/aseprite/blob/14ba0ab411f85fb44b2b52a5ae061f68efbc2bb5/src/doc/blend_mode.h#L15
    public enum BlendMode
    {
        // Special internal/undocumented alpha compositing and blend modes

        Unspecified = -1,
        Src = -2,
        Merge = -3,
        NegBW = -4,
        RedTint = -5,
        BlueTint = -6,

        // Aseprite (.ase files) blend modes

        Normal = 0,
        Multiply,
        Screen,
        Overlay,
        Darken,
        Lighten,
        ColorDodge,
        ColorBurn,
        HardLight,
        SoftLight,
        Difference,
        Exclusion,
        Hue,
        Saturation,
        Color,
        Luminosity,
        Addition,
        Subtract,
        Divide
    };
}
