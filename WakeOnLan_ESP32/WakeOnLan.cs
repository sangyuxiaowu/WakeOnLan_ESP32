using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using nanoFramework.Networking;

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
            Regex regex = new Regex("^([0-9a-fA-F]{2}[:-]?){5}([0-9a-fA-F]{2})$");

            if (!regex.IsMatch(macAddress))
            {
                throw new ArgumentException("Invalid MAC address.");
            }

            string cleanedMacAddress = new Regex("[:-]").Replace(macAddress, "");

            byte[] macBytes = new byte[6];

            for (int i = 0; i < 6; i++)
            {
                macBytes[i] = Convert.ToByte(cleanedMacAddress.Substring(i * 2, 2), 16);
            }

            return macBytes;
        }

        static void SendMagicPacket(byte[] magicPacket)
        {
            UdpClient udpClient = new UdpClient();
            udpClient.Connect(IPAddress.Broadcast, 9);
            udpClient.Send(magicPacket);
            udpClient.Close();
        }

    }
}
