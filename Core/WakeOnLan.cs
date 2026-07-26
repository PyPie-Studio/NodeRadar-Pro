using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NodeRadarPro.Core;

/// <summary>
/// Implements the Magic Packet protocol for remotely waking up devices.
/// </summary>
public static class WakeOnLan
{
    /// <summary>
    /// Sends a Magic Packet to the specified MAC address.
    /// </summary>
    /// <param name="macAddress">Target MAC address in AA:BB:CC:DD:EE:FF format.</param>
    /// <returns>True if the packet was successfully broadcasted.</returns>
    public static async Task<bool> WakeAsync(string macAddress)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(macAddress)) return false;

            // Normalize MAC address: AA:BB:CC... -> AABBCC...
            string cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(".", "");
            if (cleanMac.Length != 12) return false;

            byte[] macBytes = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                if (!byte.TryParse(cleanMac.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out byte parsedByte))
                {
                    return false;
                }
                macBytes[i] = parsedByte;
            }

            // Create Magic Packet: 6 bytes of 0xFF followed by 16 repetitions of the MAC
            byte[] packet = new byte[102];
            for (int i = 0; i < 6; i++) packet[i] = 0xFF;

            for (int i = 1; i <= 16; i++)
            {
                Array.Copy(macBytes, 0, packet, i * 6, 6);
            }

            // Broadcast packet via UDP on port 9 (standard WoL port)
            using var client = new UdpClient();
            client.EnableBroadcast = true;
            
            // We broadcast to the global broadcast address. 
            // In a more advanced version, we could target specific subnets.
            var endpoint = new IPEndPoint(IPAddress.Broadcast, 9);
            
            await client.SendAsync(packet, packet.Length, endpoint);
            
            // Send multiple times for reliability
            await Task.Delay(50);
            await client.SendAsync(packet, packet.Length, endpoint);

            return true;
        }
        catch (Exception ex)
        {
            Data.LocalDatabase.Instance.Log(LogLevel.Error, "WoL", $"Failed to send magic packet: {ex.Message}");
            return false;
        }
    }
}
