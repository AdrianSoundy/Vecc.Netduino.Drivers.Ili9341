
namespace NanoFramework.Driver.Ili9341
{
    /// <summary>
    /// 16 bit Basic colurs
    //  For IL9341 layout is  R(5 bits) + G(6 bits) + B(5 bits)   
    /// </summary>
    public enum Colour : ushort
    {
            Black =   0b00000_000000_00000,
            White =   0b11111_111111_11111,
            Red =     0b11111_000000_00000,
            Lime =    0b00000_111111_00000,
            Blue =    0b00000_000000_11111,
            Yellow =  0b11111_111111_00000,
            Cyan =    0b00000_111111_11111,
            Magenta = 0b11111_000000_11111,
            Silver =  0b11000_110000_11000,
            Gray =    0b10000_100000_10000,
            Maroon =  0b10000_000000_00000,
            Olive =   0b10000_100000_10000,
            Green =   0b00000_100000_00000,
            Purple =  0b10000_000000_10000,
            Teal =    0b00000_100000_10000,
            Navy =    0b00000_000000_10000
        };
 }
