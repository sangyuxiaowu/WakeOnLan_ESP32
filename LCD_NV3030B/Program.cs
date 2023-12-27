using nanoFramework.Hardware.Esp32;
using System.Device.Spi;
using System.Diagnostics;
using System.Threading;

namespace LCD_NV3030B
{
    public class Program
    {
        // Doc LCD https://www.waveshare.net/wiki/1.5inch_LCD_Module
        // Doc ESP32 https://www.waveshare.net/wiki/ESP32-S3-Zero

        public static void Main()
        {

            // 白色 GND   GND
            // 紫色 VCC   3.3V
            // 绿色 DIN   GP1     MOSI
            // 橙色 CLK   GP2
            // 黄色 CS    GP3     片选
            // 蓝色 D/C   GP4
            // 黑色 RST   GP5
            // 灰色 BLK   GP6

            Debug.WriteLine("LCD test!");

            SpiDevice spiDevice;
            SpiConnectionSettings connectionSettings;

            Configuration.SetPinFunction(1, DeviceFunction.SPI1_MOSI);
            Configuration.SetPinFunction(2, DeviceFunction.SPI1_CLOCK);

            // You can get the values of SpiBus
            SpiBusInfo spiBusInfo = SpiDevice.GetBusInfo(1);
            Debug.WriteLine($"{nameof(spiBusInfo.MaxClockFrequency)}: {spiBusInfo.MaxClockFrequency}");
            Debug.WriteLine($"{nameof(spiBusInfo.MinClockFrequency)}: {spiBusInfo.MinClockFrequency}");

            connectionSettings = new SpiConnectionSettings(1, 3);
            // You can adjust other settings as well in the connection
            connectionSettings.ClockFrequency = 1_000_000;
            connectionSettings.DataBitLength = 8;
            connectionSettings.DataFlow = DataFlow.LsbFirst;
            connectionSettings.Mode = SpiMode.Mode2;

            // Then you create your SPI device by passing your settings
            spiDevice = SpiDevice.Create(connectionSettings);

            Thread.Sleep(Timeout.Infinite);
        }
    }
}
