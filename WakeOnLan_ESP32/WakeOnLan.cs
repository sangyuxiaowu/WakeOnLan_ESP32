using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace WakeOnLan_ESP32
{
    internal class WakeOnLan
    {

        internal static void Send(string macAddress)
        {
            byte[] magicPacket = CreateMagicPacket(macAddress);
            SendMagicPacket(magicPacket);
        }

        static byte[] CreateMagicPacket(string macAddress)
        {
            byte[] macBytes = ParseMacAddress(macAddress);
            byte[] magicPacket = new byte[6 + (6 * 16)];

            for (int i = 0; i < 6; i++)
            {
                magicPacket[i] = 0xFF;
            }

            for (int i = 6; i < magicPacket.Length; i += 6)
            {
                Array.Copy(macBytes, 0, magicPacket, i, 6);
            }

            return magicPacket;
        }

        static byte[] ParseMacAddress(string macAddress)
        {
            if(macAddress.Length == 17)
            {
                macAddress = macAddress.Substring(0, 2) + macAddress.Substring(3, 2) + macAddress.Substring(6, 2) + macAddress.Substring(9, 2) + macAddress.Substring(12, 2) + macAddress.Substring(15, 2);
            }

            if (macAddress.Length != 12)
            {
                throw new ArgumentException("Invalid MAC address.");
            }

            byte[] macBytes = new byte[6];

            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = Convert.ToByte(macAddress.Substring(i * 2, 2), 16);
            }

            return macBytes;
        }

        static void SendMagicPacket(byte[] magicPacket)
        {
            UdpClient udpClient = new UdpClient();
            udpClient.Connect(IPAddress.Broadcast, 9);
            udpClient.Send(magicPacket);
            udpClient.Close();
            Console.WriteLine("Magic packet sent.");
        }

    }
}
