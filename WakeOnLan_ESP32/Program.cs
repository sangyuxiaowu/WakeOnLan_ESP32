
using System;
using System.Device.Wifi;
using System.Diagnostics;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Threading;
using System.Device.Gpio;
using ImprovWifi;
using System.Net;
using Iot.Device.Ws28xx.Esp32;

namespace WakeOnLan_ESP32
{


    public class Program
    {
        static Improv _imp;
        // 1 �����飬1����
        static int WS2812_Count = 1;
        // �����GPIO����
        static int WS2812_Pin = 8;
        // �û���������
        static int BOOT_Pin = 9;
        static GpioController gpioController = new();
        static Ws28xx leddev = new XlWs2812b(WS2812_Pin, WS2812_Count);
        static BitmapImage img = leddev.Image;
        public static void Main()
        {

            Debug.WriteLine("�������� Improv Э��ʾ��");

            var userbtn = gpioController.OpenPin(BOOT_Pin, PinMode.InputPullDown);

            _imp = new Improv();

            _imp.OnProvisioningComplete += Imp_OnProvisioningComplete;

            _imp.Start("ESP32 ɣ��Ф��");

            // ������ʶ���¼�
            _imp.OnIdentify += _imp_OnIdentify;
            // �����¼�
            userbtn.ValueChanged += Userbtn_ValueChanged;

            Debug.WriteLine("Waiting for device to be provisioned");

            while (_imp.CurrentState != Improv.ImprovState.provisioned)
            {
                Thread.Sleep(500);
            }

            Debug.WriteLine("Device has been provisioned");

            // �ȴ�1�룬����ɺ����ת���ݷ��ͳ�ȥ
            Thread.Sleep(1000);

            _imp.Stop();

            Debug.WriteLine("Starting WiFi");
            img.SetPixel(0, 0, Color.Blue);
            leddev.Update();

            _imp = null;


            // Start our very simple web page server to pick up the redirect we gave
            Debug.WriteLine("Starting simple web server");
            SimpleWebListener();

            Thread.Sleep(Timeout.Infinite);
        }


        private static void _imp_OnIdentify(object sender, EventArgs e)
        {
            Debug.WriteLine("Identify Required");
            // ��˸����
            for (int i = 0; i < 10; i++)
            {
                img.SetPixel(0, 0, Color.Red);
                leddev.Update();
                Thread.Sleep(100);
                img.SetPixel(0, 0, Color.Black);
                leddev.Update();
                Thread.Sleep(100);
            }
        }
        private static void Userbtn_ValueChanged(object sender, PinValueChangedEventArgs e)
        {
            // ������������δ��ʼ������Ȩ���򲻴���
            if (_imp is null || _imp.CurrentState != Improv.ImprovState.authorizationRequired)
            {
                return;
            }
            if (e.ChangeType == PinEventTypes.Rising)
            {
                Debug.WriteLine("User button pressed");
                _imp.Authorise(true);
                // ��֤�ɹ����������
                img.SetPixel(0, 0, Color.Green);
                leddev.Update();
            }
        }



        private static void Imp_OnProvisioningComplete(object sender, EventArgs e)
        {
            SetProvisioningURL();
        }

        private static void SetProvisioningURL()
        {
            // All good, wifi connected, set up URL for access
            _imp.RedirectUrl = "http://" + _imp.GetCurrentIPAddress() + "/start.htm";
        }

        private static void SimpleWebListener()
        {
            // set-up our HTTP response
            string responseString =
                "<HTML><BODY>" +
                "<h2>Hello from nanoFramework</h2>" +
                "<p>We are a newly provisioned device using <b>Improv</b> over Bluetooth.</p>" +
                "<p>See <a href='https://www.improv-wifi.com'>Improv web site</a> for details" +
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

                    Debug.WriteLine("Web request received");

                    // Get the response stream
                    HttpListenerResponse response = context.Response;

                    // Write reply
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);

                    // output stream must be closed
                    context.Response.Close();

                    Debug.WriteLine("Web response sent");

                    // context must be closed
                    context.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("* Error getting context: " + ex.Message + "\r\nSack = " + ex.StackTrace);
                }
            }
        }
    }

}
