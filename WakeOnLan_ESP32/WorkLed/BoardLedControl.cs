﻿using Iot.Device.Ws28xx.Esp32;
using System.Drawing;
using System.Threading;

namespace WakeOnLan_ESP32.WorkLed
{
    internal class BoardLedControl
    {
        /// <summary>
        /// 灯珠的引脚
        /// </summary>
        static int WS2812_Pin;

        /// <summary>
        /// 板载 RGB 灯
        /// </summary>
        static XlWs2812b leddev;

        /// <summary>
        /// 灯光图片
        /// </summary>
        static BitmapImage image;

        /// <summary>
        /// 灯光控制线程
        /// </summary>
        private Thread statusThread;

        /// <summary>
        /// 自动更新灯光
        /// </summary>
        private bool autoUpdate = false;

        /// <summary>
        /// 设备状态
        /// </summary>
        private RunStatus _deviceStatus = RunStatus.Start;


        /// <summary>
        /// 工作灯控制
        /// </summary>
        /// <param name="pin">Gpio Pin</param>
        public BoardLedControl(int pin = 21)
        {
            WS2812_Pin = pin;
            leddev = new XlWs2812b(WS2812_Pin, 1, 1);
            image = leddev.Image;

            statusThread = new Thread(() =>
            {
                while (true)
                {
                    if (autoUpdate)
                    {
                        UpdateLedStatus();
                    }
                }
            });
            statusThread.Start();
        }

        /// <summary>
        /// 设备状态
        /// </summary>
        public RunStatus DeviceStatus
        {
            get { return _deviceStatus; }
            set
            {
                _deviceStatus = value;
            }
        }

        /// <summary>
        /// 开启自动更新灯光
        /// </summary>
        public void StartAutoUpdate()
        {
            autoUpdate = true;
        }

        /// <summary>
        /// 关闭自动更新灯光
        /// </summary>
        public void StopAutoUpdate()
        {
            autoUpdate = false;
        }

        /// <summary>
        /// 更新灯光
        /// </summary>
        public void UpdateLedStatus()
        {
            // 根据设备状态更新灯光
            switch (DeviceStatus)
            {
                case RunStatus.Start:
                    LedSet(Color.Blue);
                    break;
                case RunStatus.OnIdentify:
                    LedBlink(Color.Blue, 200);
                    break;
                case RunStatus.AuthSuccess:
                    LedBlink(Color.Green, 200);
                    LedBlink(Color.Green, 200);
                    Thread.Sleep(700);
                    break;
                case RunStatus.Connecting:
                    LedBlink(Color.Orange, 200);
                    break;
                case RunStatus.ConfigFailed:
                    LedBlink(Color.Red);
                    break;
                case RunStatus.ConnectFailed:
                    LedSet(Color.Red);
                    break;
                case RunStatus.Working:
                    LedBreath(Color.Green);
                    break;
                case RunStatus.ClearConfig:
                    LedSet(Color.Orange);
                    break;
                default:
                    LedSet(Color.Black);
                    break;
            }
        }

        /// <summary>
        /// 灯光效果-闪烁
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="delay">时延</param>
        public void LedBlink(Color color, int delay = 500)
        {
            image.SetPixel(0, 0, color);
            leddev.Update();
            Thread.Sleep(delay);
            image.SetPixel(0, 0, Color.Black);
            leddev.Update();
            Thread.Sleep(delay);
        }

        /// <summary>
        /// 灯光颜色设置
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="sleepDuration">等待</param>
        public void LedSet(Color color, int sleepDuration = 0)
        {
            image.SetPixel(0, 0, color);
            leddev.Update();
            if (sleepDuration > 0)
            {
                Thread.Sleep(sleepDuration);
            }
        }

        /// <summary>
        /// 灯光效果-呼吸
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="duration">时长</param>
        /// <param name="steps">步长</param>
        public void LedBreath(Color color, int duration = 3900, int steps = 20)
        {
            // 限制参数范围
            steps = steps < 10 ? 10 : steps;
            duration = duration < 1000 ? 1000 : duration;

            // 计算每一步的暂停时长 (单位：毫秒)  
            int sleepDuration = duration / (2 * steps);
            for (int i = 1; i <= steps; i++)
            {
                // 计算当前明度  
                float brightness = (float)i / steps;
                // 设置颜色  
                Color currentColor = Color.FromArgb(
                    (int)(color.R * brightness),
                    (int)(color.G * brightness),
                    (int)(color.B * brightness));
                LedSet(currentColor, sleepDuration);
            }
            for (int i = steps; i > 0; i--)
            {
                float brightness = (float)i / steps;
                Color currentColor = Color.FromArgb(
                    (int)(color.R * brightness),
                    (int)(color.G * brightness),
                    (int)(color.B * brightness));
                LedSet(currentColor, sleepDuration);
            }
        }

    }
}
