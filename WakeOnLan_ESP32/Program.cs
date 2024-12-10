using ImprovWifi;
using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using WakeOnLan_ESP32.WorkLed;

namespace WakeOnLan_ESP32
{


    public class Program
    {
        // 硬件配置信息

        // 灯珠的GPIO引脚
        static int WS2812_Pin = 8;
        // 用户按键引脚
        static int BOOT_Pin = 9;
        // 硬件配网名称
        static string _deviceName = "ESP32 桑榆肖物";


        static Improv _imp;
        static GpioController gpioController = new();

        /// <summary>
        /// 灯光控制
        /// </summary>
        static BoardLedControl _led = new(WS2812_Pin);

        /// <summary>
        /// 是否连接成功
        /// </summary>
        static bool _connectSuccess = false;

        public static void Main()
        {

            Debug.WriteLine("START");

            // 开启工作灯，蓝色引擎启动！
            _led.StartAutoUpdate();

            // 初始化Improv
            _imp = new Improv();

            // 读取配置文件
            var configuration = Wireless80211Configuration.GetAllWireless80211Configurations();
            if (configuration.Length == 0 || string.IsNullOrEmpty(configuration[0].Ssid) || string.IsNullOrEmpty(configuration[0].Password))
            {
                Console.WriteLine("No WiFi configuration found");
            }
            else
            {
                Console.WriteLine($"SSID: {configuration[0].Ssid}, Password: {configuration[0].Password}");
                // 执行连接wifi逻辑;
                _led.DeviceStatus = RunStatus.Connecting;
                var success = _imp.ConnectWiFi(configuration[0].Ssid, configuration[0].Password);
                if (!success)
                {
                    Console.WriteLine("Failed to connect to WiFi");
                    _led.DeviceStatus = RunStatus.ConnectFailed;
                }
                else
                {
                    Console.WriteLine("Connected to WiFi successfully");
                    _led.DeviceStatus = RunStatus.Working;
                    _connectSuccess = true;

                    var ip = _imp.GetCurrentIPAddress();
                    Console.WriteLine($"IP: {ip}");
                }
            }

            // 初始化用户按键
            var userbtn = gpioController.OpenPin(BOOT_Pin, PinMode.InputPullDown);
            // 按键事件
            userbtn.ValueChanged += Userbtn_ValueChanged;

            // 若未连接成功，执行Improv配网
            if (!_connectSuccess)
            {
                // 设置Improv
                _imp.OnProvisioningComplete += Imp_OnProvisioningComplete;
                _imp.Start(_deviceName);
                // 被请求识别事件
                _imp.OnIdentify += _imp_OnIdentify;
                Console.WriteLine("Waiting for device to be provisioned");
                while (_imp.CurrentState != Improv.ImprovState.provisioned)
                {
                    Thread.Sleep(500);
                }
                Console.WriteLine("Device has been provisioned");
                // 等待1秒，让完成后的跳转数据发送出去
                Thread.Sleep(1000);
                _imp.Stop();
            }

            // 释放资源
            _imp = null;

            // 正常工作逻辑
            Console.WriteLine("Starting app");
            _led.DeviceStatus = RunStatus.Working;
            AppRun();
            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        /// 被请求识别
        /// </summary>
        private static void _imp_OnIdentify(object sender, EventArgs e)
        {
            Console.WriteLine("Identify requested");
            if (_imp.CurrentState != Improv.ImprovState.authorizationRequired)
            {
                return;
            }
            _led.DeviceStatus = RunStatus.OnIdentify;
        }


        // 记录上一次按键按下时间
        static DateTime lastClickTime = DateTime.UtcNow;

        /// <summary>
        /// 用户按键事件
        /// </summary>
        private static void Userbtn_ValueChanged(object sender, PinValueChangedEventArgs e)
        {
            // 记录按键按下时间
            if (e.ChangeType == PinEventTypes.Falling)
            {
                lastClickTime = DateTime.UtcNow;
            }
            // 按键松开
            if (e.ChangeType == PinEventTypes.Rising)
            {
                // 按键按下时间大于 5s，重置wifi配置
                if ((DateTime.UtcNow - lastClickTime).TotalSeconds > 5)
                {
                    // 重置wifi配置
                    Console.WriteLine("Reset wifi configuration");
                    var wificonfig = new Wireless80211Configuration(0);
                    wificonfig.Ssid = "";
                    wificonfig.Password = "";
                    wificonfig.SaveConfiguration();
                    _led.DeviceStatus = RunStatus.ClearConfig;
                }
            }


            // 配网结束或者未开始请求授权，则不处理
            if (_imp is null || _imp.CurrentState != Improv.ImprovState.authorizationRequired)
            {
                return;
            }
            if (e.ChangeType == PinEventTypes.Rising)
            {
                Console.WriteLine("User button pressed");
                _imp.Authorise(true);
                // 验证成功，改变灯光状态
                _led.DeviceStatus = RunStatus.AuthSuccess;
            }
        }


        /// <summary>
        /// 配网完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Imp_OnProvisioningComplete(object sender, EventArgs e)
        {
            SetProvisioningURL();
        }

        private static void SetProvisioningURL()
        {
            // All good, wifi connected, set up URL for access
            _imp.RedirectUrl = "http://" + _imp.GetCurrentIPAddress() + "/start.htm";
        }

        private static void AppRun()
        {
            // set-up our HTTP response
            string responseString =
                "<HTML><BODY>" +
                "<h2>Hello ESP32 WakeOnLan</h2>" +
                "<p>We are a newly provisioned device using <b>Improv</b> over Bluetooth.</p>" +
                "<p>See <a href='https://www.improv-wifi.com'>Improv web site</a> for details</p>" +
                "<p>Click the button to send a Wake On LAN packet.</p>" +
                "<form method='post'>" +
                "<input type='text' name='mac' value='' />" +
                "<input type='submit' value='Wake On LAN' />" +
                "</form>" +
                "</BODY></HTML>";
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            // Create a listener.
            HttpListener listener = new("http", 80);

            listener.Start();

            while (true)
            {
                try
                {
                    // Now wait on context for a connection
                    HttpListenerContext context = listener.GetContext();

                    Console.WriteLine("Web request received");

                    if (context.Request.HttpMethod == "POST")
                    {
                        // Handle WOL request
                        var body = new System.IO.StreamReader(context.Request.InputStream).ReadToEnd();
                        var macAddress = System.Web.HttpUtility.ParseQueryString(body).Get("mac");
                        if (!string.IsNullOrEmpty(macAddress))
                        {
                            WakeOnLan.Send(macAddress);
                            Console.WriteLine($"WOL packet sent to {macAddress}");
                        }
                        else
                        {
                            Console.WriteLine("No MAC address provided");
                        }
                    }

                    // Get the response stream
                    HttpListenerResponse response = context.Response;

                    // Write reply
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);

                    // output stream must be closed
                    context.Response.Close();

                    Console.WriteLine("Web response sent");

                    // context must be closed
                    context.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("* Error getting context: " + ex.Message + "\r\nSack = " + ex.StackTrace);
                }
            }
        }
    }

}
