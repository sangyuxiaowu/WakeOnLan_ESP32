using System;
using System.Diagnostics;
using System.Threading;
using System.Device.Gpio;
using nanoFramework.Hardware.Esp32;
using Iot.Device.Ssd13xx;
using System.Device.I2c;

namespace OLED_SSD13XX
{
    public class Program
    {
        public static void Main()
        {
            // 设置引脚功能
            Configuration.SetPinFunction(1, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(2, DeviceFunction.I2C1_CLOCK);

            using Ssd1306 device = new Ssd1306(I2cDevice.Create(new I2cConnectionSettings(1, Ssd1306.DefaultI2cAddress)), Ssd13xx.DisplayResolution.OLED128x64);

            device.ClearScreen();
            device.Font = new BasicFont();
            device.DrawString(2, 2, "nF IOT!", 2);//large size 2 font
            device.DrawString(2, 32, "nanoFramework", 1, true);//centered text
            device.Display();

            Thread.Sleep(2000);
            device.ClearScreen();
            device.Font = new DoubleByteFont();
            device.DrawString(2, 2, "功夫＄", 2, false);
            device.DrawString(2, 34, "８９ＡＢ功夫＄", 1, true);
            device.Display();


            Thread.Sleep(Timeout.Infinite);

        }
    }
}
