namespace NanoFramework.Driver.Ili9341
{
    public abstract class Font
    {
        public abstract byte SpaceWidth { get; }
        public abstract FontCharacter GetFontData(char character);
    }
}
