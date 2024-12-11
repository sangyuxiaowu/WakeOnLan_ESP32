#define ESP32_S3_ZERO

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

        # if ESP32_S3_ZERO
        // 灯珠的GPIO引脚
        static int WS2812_Pin = 21;
        // 用户按键引脚
        static int BOOT_Pin = 0;
        #else
        // 灯珠的GPIO引脚
        static int WS2812_Pin = 8;
        // 用户按键引脚
        static int BOOT_Pin = 9;
        #endif
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
                    // 判断是否已经配置但连接失败
                    if (_imp.ErrorState == Improv.ImprovError.unableConnect)
                    {
                        Console.WriteLine("Unable to connect to WiFi");
                        _led.DeviceStatus = RunStatus.ConfigFailed;
                    }
                    Thread.Sleep(500);
                }
                // 等待1秒，让完成后的跳转数据发送出去
                Thread.Sleep(1000);
                _connectSuccess = true;
                _imp.Stop();
            }

            // 释放资源
            _imp = null;

            Console.WriteLine("Device has been provisioned");

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
                "<HTML><HEAD>" +
                "<meta name='viewport' content='width=device-width, initial-scale=1'>" +
                "<style>" +
                "body { font-family: Arial, sans-serif; margin: 20px;text-align: center;}" +
                "h2 { color: #333; }" +
                "form { margin-top: 20px; }" +
                "input[type='text'] { padding: 10px; border: 1px solid #ccc; border-radius:4px 0 0 4px; width: 250px; }" +
                "input[type='submit'] { padding: 10px 20px; border: none; border-radius:0 4px 4px 0; background-color: #4CAF50; color: white; cursor: pointer; }" +
                "input[type='submit']:hover { background-color: #45a049; }" +
                "p { line-height: 1.6; }" +
                "a { color: #1E90FF; text-decoration: none; }" +
                "a:hover { text-decoration: underline; }" +
                "#mac-list { margin: 20px auto; background: white; padding: 20px; border-radius: 8px; box-shadow: 0 0 10px rgba(0, 0, 0, 0.1); width: 100%; max-width: 350px; }" +
                "#mac-list ul { list-style-type: none; padding: 0; }" +
                "#mac-list li { padding: 5px 0; border-bottom: 1px solid #ccc; }" +
                "</style>" +
                "</HEAD><BODY>" +
                "<h2>ESP32 WakeOnLan</h2>" +
                "<p>Click the button to send a Wake On LAN packet.</p>" +
                "<form method='get' action='/wol' onsubmit='return validateAndFormatMac()'>" +
                "<input type='text' id='mac' name='mac' value='' />" +
                "<input type='submit' value='Wake On LAN' />" +
                "</form>" +
                "<div id='mac-list'>" +
                "<h3>Stored MAC Addresses</h3>" +
                "<ul id='list'></ul>" +
                "</div>" +
                "<script>" +
                "function validateAndFormatMac() {" +
                "    var macInput = document.getElementById('mac');" +
                "    var mac = macInput.value;" +
                "    var macRegex = /^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$/;" +
                "    if (!macRegex.test(mac)) {" +
                "        alert('Invalid MAC address format. Please use XX:XX:XX:XX:XX:XX or XX-XX-XX-XX-XX-XX.');" +
                "        return false;" +
                "    }" +
                "    mac = mac.replace(/[:-]/g, '');" +
                "    macInput.value = mac;" +
                "    storeMac(mac);" +
                "    return true;" +
                "}" +
                "function storeMac(mac) {" +
                "    var macs = JSON.parse(localStorage.getItem('macs')) || [];" +
                "    macs.push(mac);" +
                "    localStorage.setItem('macs', JSON.stringify(macs));" +
                "    displayMacs();" +
                "}" +
                "function displayMacs() {" +
                "    var macs = JSON.parse(localStorage.getItem('macs')) || [];" +
                "    var list = document.getElementById('list');" +
                "    list.innerHTML = '';" +
                "    macs.forEach(function(mac) {" +
                "        var li = document.createElement('li');" +
                "        var a = document.createElement('a'); " +
                "        a.href = '/wol?mac=' + mac;" +
                "        a.textContent = mac;" +
                "        li.appendChild(a);" +
                "        list.appendChild(li);" +
                "    });" +
                "}" +
                "document.addEventListener('DOMContentLoaded', displayMacs);" +
                "</script>" +
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

                    var url = context.Request.RawUrl;

                    // Get the response stream
                    HttpListenerResponse response = context.Response;

                    if (url.StartsWith("/wol?mac="))
                    {
                        var macAddress = url.Substring(9);
                        Console.WriteLine($"WOL packet sent to {macAddress}");
                        WakeOnLan.Send(macAddress);

                        // 输出json格式
                        response.ContentType = "application/json";
                        var json = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                        response.ContentLength64 = json.Length;
                        response.OutputStream.Write(json, 0, json.Length);
                    }
                    else
                    {
                        // 输出默认页面
                        response.ContentLength64 = buffer.Length;
                        response.OutputStream.Write(buffer, 0, buffer.Length);
                    }

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
