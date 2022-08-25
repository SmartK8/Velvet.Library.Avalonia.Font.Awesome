using System;

namespace Library.Font.Awesome
{
    [Flags]
    public enum Place
    {
        Left   = 0x0_0_1,
        Right  = 0x0_0_2,
        Top    = 0x0_0_4,
        Bottom = 0x0_0_8,

        Under = 0x0_1_0,

        Quarter = 0x0_2_0,
        Half    = 0x0_4_0,
        Full    = 0x0_8_0,

        X1 = 0x1_0_0, // 1.0
        X2 = 0x2_0_0, // 0.5
        X3 = 0x4_0_0, // 0.33
        X4 = 0x8_0_0, // 0.25
    }
}
