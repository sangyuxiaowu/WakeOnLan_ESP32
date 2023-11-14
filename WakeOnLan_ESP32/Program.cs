using Iot.Device.Ws28xx.Esp32;
using nanoFramework.Networking;
using System.Diagnostics;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Threading;

namespace WakeOnLan_ESP32
{
    public class Program
    {
        // 1 个灯珠，1像素
        const int WS2812_Count = 1;
        // 灯珠的引脚
        const int WS2812_Pin = 21;
        // 灯光控制器
        private static DeviceController deviceController = new DeviceController(new Ws2812c(WS2812_Pin, WS2812_Count));
        /// <summary>
        /// 设备运行状态
        /// </summary>
        private static DeviceController.RunStatus _currentStatus;
        public static void Main()
        {

            Debug.WriteLine("WakeOnLan Start!");


            // 读取网络配置，连接wifi
            ConnectWifi();

            deviceController.DeviceStatus = DeviceController.RunStatus.Connecting;
            // 模拟设备状态改变  
            Thread statusChangeThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000); // 等待5秒  
                    //deviceController.DeviceStatus = DeviceController.RunStatus.ConfigFailed;
                    Thread.Sleep(5000);
                    //deviceController.DeviceStatus = DeviceController.RunStatus.ConnectFailed;
                    Thread.Sleep(5000);
                    deviceController.DeviceStatus = DeviceController.RunStatus.Working;
                }
            });
            statusChangeThread.Start();



            // 轮询设备状态并更新LED  
            while (true)
            {
                deviceController.UpdateLedStatus();
            }
        }


        private static void ConnectWifi()
        {
            // 读取配置文件
            var configuration = Wireless80211Configuration.GetAllWireless80211Configurations();
            if (configuration.Length == 0)
            {
                Debug.WriteLine("没有找到wifi配置文件");
                return;
            }
            Debug.WriteLine($"SSID: {configuration[0].Ssid}, Password: {configuration[0].Password}");
            return;
            // 连接wifi超时时间60秒
            CancellationTokenSource cs = new(60000);
            var success = WifiNetworkHelper.ConnectDhcp(configuration[0].Ssid, configuration[0].Password, requiresDateTime: true, token: cs.Token);
            if (!success)
            {
                // 如果出现异常你可以通过 ConnectionError 获取异常的详情信息：
                Debug.WriteLine($"无法连接到网络，错误: {WifiNetworkHelper.Status}");
                if (WifiNetworkHelper.HelperException != null)
                {
                    Debug.WriteLine($"ex: {WifiNetworkHelper.HelperException}");
                }
            }
            //否则 连接成功，您已经拥有有效的IP和时间
        }




    }

    public class DeviceController
    {

        private Ws28xx ws2812_neo;
        public DeviceController(Ws28xx _ws2812_neo)
        {
            // 初始化设备状态  
            DeviceStatus = RunStatus.Connecting;
            ws2812_neo = _ws2812_neo;
        }

        /// <summary>
        /// 定义设备状态
        /// </summary>
        public enum RunStatus
        {
            /// <summary>  
            /// wifi连接中,灯橙色闪烁  
            /// </summary>  
            Connecting,
            /// <summary>  
            /// wifi 配置问题，灯红色闪烁  
            /// </summary>  
            ConfigFailed,
            /// <summary>  
            /// wifi 连接失败，灯红色常亮  
            /// </summary>  
            ConnectFailed,
            /// <summary>  
            /// 正常工作中,绿色呼吸灯  
            /// </summary>  
            Working
        }

        // 定义设备状态属性  
        public RunStatus DeviceStatus { get; set; }

        public void UpdateLedStatus() {

            BitmapImage img = ws2812_neo.Image;

            switch (DeviceStatus)
            {
                case RunStatus.Connecting:
                    // 设置灯橙色闪烁  
                    
                    img.SetPixel(0,0, Color.Orange);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    img.SetPixel(0,0, Color.Black);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    break;
                case RunStatus.ConfigFailed:
                    // 设置灯红色闪烁  
                    img.SetPixel(0, 0, Color.Red);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    img.SetPixel(0, 0, Color.Black);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    break;
                case RunStatus.ConnectFailed:
                    // 设置灯红色常亮  
                    img.SetPixel(0, 0, Color.Red);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    break;
                case RunStatus.Working:
                    // 设置绿色呼吸灯  
                    Breathe(Color.Green, 5000, 100);
                    break;
            }
        }

        /// <summary>
        /// 呼吸灯效果
        /// </summary>
        /// <param name="color">使用指定的颜色</param>
        /// <param name="duration">时长</param>
        /// <param name="steps">步长</param>
        public void Breathe(Color color, int duration, int steps)
        {
            // 计算每一步的暂停时长 (单位：毫秒)  
            int sleepDuration = duration / (2 * steps);
            for (int i = 0; i < steps; i++)
            {
                // 计算当前明度  
                float brightness = (float)i / steps;
                // 设置颜色  
                Color currentColor = Color.FromArgb((int)(color.A * brightness), color.R, color.G, color.B);
                SetColorAndSleep(currentColor, sleepDuration);
            }
            for (int i = steps; i > 0; i--)
            {
                // 计算当前明度  
                float brightness = (float)i / steps;
                // 设置颜色  
                Color currentColor = Color.FromArgb((int)(color.A * brightness), color.R, color.G, color.B);
                SetColorAndSleep(currentColor, sleepDuration);
            }
        }

        private void SetColorAndSleep(Color color, int sleepDuration)
        {
            BitmapImage img = ws2812_neo.Image;
            // 设置像素颜色  
            img.SetPixel(0, 0, color);
            // 更新LED灯  
            ws2812_neo.Update();
            // 暂停指定的时长  
            Thread.Sleep(sleepDuration);
        }


    }

}
