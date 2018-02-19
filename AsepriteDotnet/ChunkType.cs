namespace Aseprite
{
    internal enum ChunkType
    {
        OldPalette4 = 0x0004,
        OldPalette11 = 0x0011,
        Layer = 0x2004,
        Cel = 0x2005,
        CelExtra = 0x2006,
        Mask = 0x2016,
        Path = 0x2017,
        FrameTags = 0x2018,
        Palette = 0x2019,
        UserData = 0x2020,
        Slice = 0x2022
    }
}
