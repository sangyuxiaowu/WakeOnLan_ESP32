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
        // 1 �����飬1����
        const int WS2812_Count = 1;
        // ���������
        const int WS2812_Pin = 21;
        // �ƹ������
        private static DeviceController deviceController = new DeviceController(new Ws2812c(WS2812_Pin, WS2812_Count));
        /// <summary>
        /// �豸����״̬
        /// </summary>
        private static DeviceController.RunStatus _currentStatus;
        public static void Main()
        {

            Debug.WriteLine("WakeOnLan Start!");


            // ��ȡ�������ã�����wifi
            ConnectWifi();

            deviceController.DeviceStatus = DeviceController.RunStatus.Connecting;
            // ģ���豸״̬�ı�  
            Thread statusChangeThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(5000); // �ȴ�5��  
                    //deviceController.DeviceStatus = DeviceController.RunStatus.ConfigFailed;
                    Thread.Sleep(5000);
                    //deviceController.DeviceStatus = DeviceController.RunStatus.ConnectFailed;
                    Thread.Sleep(5000);
                    deviceController.DeviceStatus = DeviceController.RunStatus.Working;
                }
            });
            statusChangeThread.Start();



            // ��ѯ�豸״̬������LED  
            while (true)
            {
                deviceController.UpdateLedStatus();
            }
        }


        private static void ConnectWifi()
        {
            // ��ȡ�����ļ�
            var configuration = Wireless80211Configuration.GetAllWireless80211Configurations();
            if (configuration.Length == 0)
            {
                Debug.WriteLine("û���ҵ�wifi�����ļ�");
                return;
            }
            Debug.WriteLine($"SSID: {configuration[0].Ssid}, Password: {configuration[0].Password}");
            return;
            // ����wifi��ʱʱ��60��
            CancellationTokenSource cs = new(60000);
            var success = WifiNetworkHelper.ConnectDhcp(configuration[0].Ssid, configuration[0].Password, requiresDateTime: true, token: cs.Token);
            if (!success)
            {
                // ��������쳣�����ͨ�� ConnectionError ��ȡ�쳣��������Ϣ��
                Debug.WriteLine($"�޷����ӵ����磬����: {WifiNetworkHelper.Status}");
                if (WifiNetworkHelper.HelperException != null)
                {
                    Debug.WriteLine($"ex: {WifiNetworkHelper.HelperException}");
                }
            }
            //���� ���ӳɹ������Ѿ�ӵ����Ч��IP��ʱ��
        }




    }

    public class DeviceController
    {

        private Ws28xx ws2812_neo;
        public DeviceController(Ws28xx _ws2812_neo)
        {
            // ��ʼ���豸״̬  
            DeviceStatus = RunStatus.Connecting;
            ws2812_neo = _ws2812_neo;
        }

        /// <summary>
        /// �����豸״̬
        /// </summary>
        public enum RunStatus
        {
            /// <summary>  
            /// wifi������,�Ƴ�ɫ��˸  
            /// </summary>  
            Connecting,
            /// <summary>  
            /// wifi �������⣬�ƺ�ɫ��˸  
            /// </summary>  
            ConfigFailed,
            /// <summary>  
            /// wifi ����ʧ�ܣ��ƺ�ɫ����  
            /// </summary>  
            ConnectFailed,
            /// <summary>  
            /// ����������,��ɫ������  
            /// </summary>  
            Working
        }

        // �����豸״̬����  
        public RunStatus DeviceStatus { get; set; }

        public void UpdateLedStatus() {

            BitmapImage img = ws2812_neo.Image;

            switch (DeviceStatus)
            {
                case RunStatus.Connecting:
                    // ���õƳ�ɫ��˸  
                    
                    img.SetPixel(0,0, Color.Orange);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    img.SetPixel(0,0, Color.Black);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    break;
                case RunStatus.ConfigFailed:
                    // ���õƺ�ɫ��˸  
                    img.SetPixel(0, 0, Color.Red);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    img.SetPixel(0, 0, Color.Black);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    break;
                case RunStatus.ConnectFailed:
                    // ���õƺ�ɫ����  
                    img.SetPixel(0, 0, Color.Red);
                    ws2812_neo.Update();
                    Thread.Sleep(500);
                    break;
                case RunStatus.Working:
                    // ������ɫ������  
                    Breathe(Color.Green, 5000, 100);
                    break;
            }
        }

        /// <summary>
        /// ������Ч��
        /// </summary>
        /// <param name="color">ʹ��ָ������ɫ</param>
        /// <param name="duration">ʱ��</param>
        /// <param name="steps">����</param>
        public void Breathe(Color color, int duration, int steps)
        {
            // ����ÿһ������ͣʱ�� (��λ������)  
            int sleepDuration = duration / (2 * steps);
            for (int i = 0; i < steps; i++)
            {
                // ���㵱ǰ����  
                float brightness = (float)i / steps;
                // ������ɫ  
                Color currentColor = Color.FromArgb((int)(color.A * brightness), color.R, color.G, color.B);
                SetColorAndSleep(currentColor, sleepDuration);
            }
            for (int i = steps; i > 0; i--)
            {
                // ���㵱ǰ����  
                float brightness = (float)i / steps;
                // ������ɫ  
                Color currentColor = Color.FromArgb((int)(color.A * brightness), color.R, color.G, color.B);
                SetColorAndSleep(currentColor, sleepDuration);
            }
        }

        private void SetColorAndSleep(Color color, int sleepDuration)
        {
            BitmapImage img = ws2812_neo.Image;
            // ����������ɫ  
            img.SetPixel(0, 0, color);
            // ����LED��  
            ws2812_neo.Update();
            // ��ָͣ����ʱ��  
            Thread.Sleep(sleepDuration);
        }


    }

}
