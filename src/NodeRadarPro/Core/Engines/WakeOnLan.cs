using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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
        if (string.IsNullOrWhiteSpace(macAddress)) return false;

        // Normalize MAC address: AA:BB:CC... -> AABBCC...
        string cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(".", "").Replace(" ", "");
        if (cleanMac.Length != 12) return false;

        byte[] macBytes = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            ReadOnlySpan<char> byteSpan = cleanMac.AsSpan(i * 2, 2);
            if (!byte.TryParse(byteSpan, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte parsedByte))
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

        try
        {
            using var client = new UdpClient();
            client.EnableBroadcast = true;

            var broadcastEndpoints = new List<IPEndPoint>
            {
                new IPEndPoint(IPAddress.Broadcast, 7),
                new IPEndPoint(IPAddress.Broadcast, 9)
            };

            // Calculate and add subnet-specific broadcast endpoints for multi-homed systems
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (var unicast in ipProps.UnicastAddresses)
                    {
                        var subnetBroadcast = GetSubnetBroadcast(unicast);
                        if (subnetBroadcast != null)
                        {
                            broadcastEndpoints.Add(new IPEndPoint(subnetBroadcast, 7));
                            broadcastEndpoints.Add(new IPEndPoint(subnetBroadcast, 9));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Data.LocalDatabase.Instance.Log(LogLevel.Warning, "WoL", $"Subnet broadcast resolution warning: {ex.Message}");
            }

            // Deduplicate endpoints
            var uniqueEndpoints = broadcastEndpoints
                .GroupBy(ep => new { ep.Address, ep.Port })
                .Select(g => g.First())
                .ToList();

            bool sentAny = false;
            string? lastError = null;
            foreach (var endpoint in uniqueEndpoints)
            {
                try
                {
                    await client.SendAsync(packet, packet.Length, endpoint);
                    sentAny = true;
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                }
            }

            // Send multiple times (delay 50ms) for reliability
            if (sentAny)
            {
                await Task.Delay(50);
                foreach (var endpoint in uniqueEndpoints)
                {
                    try
                    {
                        await client.SendAsync(packet, packet.Length, endpoint);
                    }
                    catch { }
                }
            }
            else
            {
                Data.LocalDatabase.Instance.Log(LogLevel.Error, "WoL", $"Failed to send magic packet: {lastError ?? "No valid broadcast endpoints available."}");
            }

            return sentAny;
        }
        catch (Exception ex)
        {
            Data.LocalDatabase.Instance.Log(LogLevel.Error, "WoL", $"Failed to send magic packet: {ex.Message}");
            return false;
        }
    }

    private static IPAddress? GetSubnetBroadcast(UnicastIPAddressInformation unicast)
    {
        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
            return null;

        var ip = unicast.Address;
        var mask = unicast.IPv4Mask;
        if (mask == null || mask.Equals(IPAddress.Any))
            return null;

        byte[] ipBytes = ip.GetAddressBytes();
        byte[] maskBytes = mask.GetAddressBytes();
        byte[] broadcastBytes = new byte[4];
        for (int k = 0; k < 4; k++)
        {
            broadcastBytes[k] = (byte)(ipBytes[k] | ~maskBytes[k]);
        }

        return new IPAddress(broadcastBytes);
    }
}
