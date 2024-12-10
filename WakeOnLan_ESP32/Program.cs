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
        // Ӳ��������Ϣ

        // �����GPIO����
        static int WS2812_Pin = 8;
        // �û���������
        static int BOOT_Pin = 9;
        // Ӳ����������
        static string _deviceName = "ESP32 ɣ��Ф��";


        static Improv _imp;
        static GpioController gpioController = new();

        /// <summary>
        /// �ƹ����
        /// </summary>
        static BoardLedControl _led = new(WS2812_Pin);

        /// <summary>
        /// �Ƿ����ӳɹ�
        /// </summary>
        static bool _connectSuccess = false;

        public static void Main()
        {

            Debug.WriteLine("START");

            // ���������ƣ���ɫ����������
            _led.StartAutoUpdate();

            // ��ʼ��Improv
            _imp = new Improv();

            // ��ȡ�����ļ�
            var configuration = Wireless80211Configuration.GetAllWireless80211Configurations();
            if (configuration.Length == 0 || string.IsNullOrEmpty(configuration[0].Ssid) || string.IsNullOrEmpty(configuration[0].Password))
            {
                Console.WriteLine("No WiFi configuration found");
            }
            else
            {
                Console.WriteLine($"SSID: {configuration[0].Ssid}, Password: {configuration[0].Password}");
                // ִ������wifi�߼�;
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

            // ��ʼ���û�����
            var userbtn = gpioController.OpenPin(BOOT_Pin, PinMode.InputPullDown);
            // �����¼�
            userbtn.ValueChanged += Userbtn_ValueChanged;

            // ��δ���ӳɹ���ִ��Improv����
            if (!_connectSuccess)
            {
                // ����Improv
                _imp.OnProvisioningComplete += Imp_OnProvisioningComplete;
                _imp.Start(_deviceName);
                // ������ʶ���¼�
                _imp.OnIdentify += _imp_OnIdentify;
                Console.WriteLine("Waiting for device to be provisioned");
                while (_imp.CurrentState != Improv.ImprovState.provisioned)
                {
                    Thread.Sleep(500);
                }
                Console.WriteLine("Device has been provisioned");
                // �ȴ�1�룬����ɺ����ת���ݷ��ͳ�ȥ
                Thread.Sleep(1000);
                _imp.Stop();
            }

            // �ͷ���Դ
            _imp = null;

            // ���������߼�
            Console.WriteLine("Starting app");
            _led.DeviceStatus = RunStatus.Working;
            AppRun();
            Thread.Sleep(Timeout.Infinite);
        }

        /// <summary>
        /// ������ʶ��
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


        // ��¼��һ�ΰ�������ʱ��
        static DateTime lastClickTime = DateTime.UtcNow;

        /// <summary>
        /// �û������¼�
        /// </summary>
        private static void Userbtn_ValueChanged(object sender, PinValueChangedEventArgs e)
        {
            // ��¼��������ʱ��
            if (e.ChangeType == PinEventTypes.Falling)
            {
                lastClickTime = DateTime.UtcNow;
            }
            // �����ɿ�
            if (e.ChangeType == PinEventTypes.Rising)
            {
                // ��������ʱ����� 5s������wifi����
                if ((DateTime.UtcNow - lastClickTime).TotalSeconds > 5)
                {
                    // ����wifi����
                    Console.WriteLine("Reset wifi configuration");
                    var wificonfig = new Wireless80211Configuration(0);
                    wificonfig.Ssid = "";
                    wificonfig.Password = "";
                    wificonfig.SaveConfiguration();
                    _led.DeviceStatus = RunStatus.ClearConfig;
                }
            }


            // ������������δ��ʼ������Ȩ���򲻴���
            if (_imp is null || _imp.CurrentState != Improv.ImprovState.authorizationRequired)
            {
                return;
            }
            if (e.ChangeType == PinEventTypes.Rising)
            {
                Console.WriteLine("User button pressed");
                _imp.Authorise(true);
                // ��֤�ɹ����ı�ƹ�״̬
                _led.DeviceStatus = RunStatus.AuthSuccess;
            }
        }


        /// <summary>
        /// �������
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
