using System.Threading;
using Windows.Devices.Gpio;
using Windows.Devices.Spi;


namespace NanoFramework.Driver.Ili9341
{
    public partial class Driver
    {
        const byte lcdPortraitConfig =  0xc8;
        const byte lcdLandscapeConfig = 0x6c; 

        private readonly GpioPin _dataCommandPort;
        private readonly GpioPin _resetPort;
        private readonly GpioPin _backlightPort;
        private readonly SpiDevice _spi;
        private bool _isLandscape;
        private GpioPinValue _backlightOn;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool BacklightOn
        {
            get { return (_backlightOn == GpioPinValue.High) ? true : false; }
            set
            {
                if (_backlightPort != null)
                {
                    GpioPinValue state = value ? GpioPinValue.High : GpioPinValue.Low;
                    _backlightPort.Write(state);
                    _backlightOn = state;
                }
            }
        }

        #region Constructors

        public Driver(bool isLandscape = false,
                      int lcdChipSelectPin = 22,
                      int dataCommandPin = 21,
                      int resetPin = 18,
                      int backlightPin = 5,
                      uint spiClockFrequency = 10000000,
                      string spiModule = "SPI1" )
        {
            GpioController gc = GpioController.GetDefault();

            var Settings = new SpiConnectionSettings(lcdChipSelectPin)
            {
                DataBitLength = 8,
                ClockFrequency = 10000000,
                Mode = SpiMode.Mode0
            };

            _spi = SpiDevice.FromId(spiModule, Settings);

            _dataCommandPort = gc.OpenPin(dataCommandPin);
            _dataCommandPort.SetDriveMode(GpioPinDriveMode.Output);

            if (resetPin != -1)  
            {
                _resetPort = gc.OpenPin(resetPin);
                _resetPort.SetDriveMode(GpioPinDriveMode.Output);
            }
            else
            {
                _resetPort = null;
            }

            if (backlightPin != -1) //Poundy: changed to remove netduino dependencies
            {
                GpioPin _backlightPort = gc.OpenPin(backlightPin);
                _backlightPort.SetDriveMode(GpioPinDriveMode.Output);
            }
            else
            {
                _backlightPort = null;
            }

            InitializeScreen();
            SetOrientation(isLandscape);
        }

        #endregion Constructors

        #region Communication Methods

        protected virtual void Write(byte[] data)
        {
            _spi.Write(data);
        }

        protected virtual void Write(ushort[] data)
        {
            _spi.Write(data);
        }

        protected virtual void SendCommand(Commands command)
        {
            _dataCommandPort.Write(GpioPinValue.Low);
            Write(new[] { (byte)command });
        }

        protected virtual void SendData(params byte[] data)
        {
            _dataCommandPort.Write(GpioPinValue.High);
            Write(data);
        }

        protected virtual void SendData(params ushort[] data)
        {
            _dataCommandPort.Write(GpioPinValue.High);
            Write(data);
        }

        protected virtual void WriteReset(GpioPinValue value)
        {
            lock (this)
            {
                if (_resetPort!= null)
                {
                    _resetPort.Write(value);
                }
            }
        }

        #endregion Communication Methods

        #region Public Methods

        public void FillScreen(int left, int right, int top, int bottom, ushort color)
        {
            lock (this)
            {
                SetWindow(left, right, top, bottom);
                var buffer = new ushort[Width];

                if (color != 0)
                {
                    for (var i = 0; i < Width; i++)
                    {
                        buffer[i] = color;
                    }
                }

                for (int y = 0; y < Height; y++)
                {
                    SendData(buffer);
                }
            }
        }

        public void ClearScreen()
        {
            lock (this)
            {
                FillScreen(0, Width - 1, 0, Height - 1, 0);
            }
        }

        public void SetPixel(int x, int y, ushort color)
        {
            lock (this)
            {
                SetWindow(x, x, y, y);
                SendData(color);
            }
        }

        public void SetOrientation(bool isLandscape)
        {
            lock (this)
            {
                _isLandscape = isLandscape;
                SendCommand(Commands.MemoryAccessControl);

                if (isLandscape)
                {
                    SendData(lcdLandscapeConfig);
                    Width = 320;
                    Height = 240;
                }
                else
                {
                    SendData(lcdPortraitConfig);
                    Width = 240;
                    Height = 320;
                }

                SetWindow(0, Width - 1, 0, Height - 1);
            }
        }

        public void ScrollUp(int pixels)
        {
            lock (this)
            {
                SendCommand(Commands.VerticalScrollingStartAddress);
                SendData((ushort)pixels);

                SendCommand(Commands.MemoryWrite);
            }
        }

        #endregion Public Methods

        protected virtual void InitializeScreen()
        {
            lock (this)
            {
                WriteReset(GpioPinValue.Low);
                Thread.Sleep(10);
                WriteReset(GpioPinValue.High);
                SendCommand(Commands.SoftwareReset);
                Thread.Sleep(10);
                SendCommand(Commands.DisplayOff);

                SendCommand(Commands.MemoryAccessControl);
                SendData(lcdPortraitConfig);

                SendCommand(Commands.PixelFormatSet);
                SendData(0x55);//16-bits per pixel

                SendCommand(Commands.FrameControlNormal);
                SendData(0x00, 0x1B);

                SendCommand(Commands.GammaSet);
                SendData(0x01);

                SendCommand(Commands.ColumnAddressSet); //width of the screen
                SendData(0x00, 0x00, 0x00, 0xEF);

                SendCommand(Commands.PageAddressSet); //height of the screen
                SendData(0x00, 0x00, 0x01, 0x3F);

                SendCommand(Commands.EntryModeSet);
                SendData(0x07);

                SendCommand(Commands.DisplayFunctionControl);
                SendData(0x0A, 0x82, 0x27, 0x00);

                SendCommand(Commands.SleepOut);
                Thread.Sleep(120);

                SendCommand(Commands.DisplayOn);
                Thread.Sleep(100);

                SendCommand(Commands.MemoryWrite);
            }
        }

        public static ushort ColorFromRgb(byte r, byte g, byte b)
        {
            return (ushort)((r << 11) | (g << 5) | b);
        }

        void SetWindow(int left, int right, int top, int bottom)
        {
            lock (this)
            {
                SendCommand(Commands.ColumnAddressSet);
                SendData((byte)((left >> 8) & 0xFF),
                         (byte)(left & 0xFF),
                         (byte)((right >> 8) & 0xFF),
                         (byte)(right & 0xFF));
                SendCommand(Commands.PageAddressSet);
                SendData((byte)((top >> 8) & 0xFF),
                         (byte)(top & 0xFF),
                         (byte)((bottom >> 8) & 0xFF),
                         (byte)(bottom & 0xFF));
                SendCommand(Commands.MemoryWrite);
            }
        }
    }
}
