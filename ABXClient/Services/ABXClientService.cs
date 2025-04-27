using ABXClient.Models;
using ABXClient.Utils;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace ABXClient.Services
{
    public class ABXClientService
    {
        private readonly string _server;
        private readonly int _port;

        public ABXClientService(string server, int port)
        {
            _server = server;
            _port = port;
        }

        public void RunClient()
        {
            try
            {
                List<Packet> packets = FetchAllPackets();
                List<int> missing = FindMissingSequences(packets);

                // Fetch all missing packets in one go
                FetchAndAddMissingPackets(packets, missing);

                packets.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

                if (packets.Count == 0)
                {
                    Console.WriteLine("No packets found, nothing to save.");
                }
                else
                {
                    string outputPath = Path.Combine(AppContext.BaseDirectory, "Output");
                    Directory.CreateDirectory(outputPath);
                    TimeZoneInfo indiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    DateTime istDateTime = TimeZoneInfo.ConvertTime(DateTime.Now, indiaTimeZone);
                    string formattedDateTime = istDateTime.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                    string filePath = Path.Combine(outputPath, $"output_{formattedDateTime}.json");
                    File.WriteAllText(filePath, JsonConvert.SerializeObject(packets, Formatting.Indented));

                    Console.WriteLine("Data saved to: " + filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error occurred in RunClient method: ", ex);
                Console.WriteLine("An error occurred. Please check the ErrorLog for details.");
            }
        }

        private List<Packet> FetchAllPackets()
        {
            List<Packet> packets = new();
            try
            {
                using TcpClient client = new(_server, _port);
                using NetworkStream stream = client.GetStream();

                byte[] request = new byte[] { 1, 0 };
                stream.Write(request, 0, request.Length);

                byte[] buffer = new byte[17];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (bytesRead == 17)
                        packets.Add(ParsePacket(buffer));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error occurred in FetchAllPackets method: ", ex);
                throw;
            }

            return packets;
        }

        private void FetchAndAddMissingPackets(List<Packet> packets, List<int> missing)
        {
            if (missing.Count == 0)
                return;

            try
            {
                using TcpClient client = new(_server, _port);
                using NetworkStream stream = client.GetStream();

                foreach (var seq in missing)
                {
                    try
                    {
                        byte[] request = new byte[] { 2, (byte)seq };
                        stream.Write(request, 0, request.Length);

                        byte[] buffer = new byte[17];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);

                        if (bytesRead == 17)
                        {
                            var packet = ParsePacket(buffer);
                            if (packet != null)
                                packets.Add(packet);
                        }
                        else
                        {
                            Logger.LogError($"Warning: Incomplete packet received for missing sequence {seq}.", new Exception("Incomplete packet error"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Error occurred while fetching packet for missing sequence {seq}: ", ex);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error occurred while fetching missing packets: ", ex);
            }
        }

        private Packet ParsePacket(byte[] data)
        {
            string symbol = Encoding.ASCII.GetString(data, 0, 4);
            string side = Encoding.ASCII.GetString(data, 4, 1);
            int quantity = BigEndianUtils.ReadInt32BigEndian(data, 5);
            int price = BigEndianUtils.ReadInt32BigEndian(data, 9);
            int sequence = BigEndianUtils.ReadInt32BigEndian(data, 13);

            // Validation after parsing
            if (string.IsNullOrWhiteSpace(symbol) || symbol.Length != 4)
            {
                throw new Exception($"Invalid Symbol format: '{symbol}'");
            }

            if (side != "B" && side != "S")
            {
                throw new Exception($"Invalid Side value: '{side}', expected 'B' or 'S'");
            }

            if (quantity <= 0)
            {
                throw new Exception($"Invalid Quantity: {quantity}, must be positive.");
            }

            if (price <= 0)
            {
                throw new Exception($"Invalid Price: {price}, must be positive.");
            }

            if (sequence < 0)
            {
                throw new Exception($"Invalid Sequence: {sequence}, must be non-negative.");
            }

            return new Packet
            {
                Symbol = symbol,
                Side = side,
                Quantity = quantity,
                Price = price,
                Sequence = sequence
            };
        }

        private List<int> FindMissingSequences(List<Packet> packets)
        {
            List<int> missing = new();
            try
            {
                packets.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

                int expected = packets[0].Sequence;
                foreach (var packet in packets)
                {
                    while (expected < packet.Sequence)
                    {
                        missing.Add(expected);
                        expected++;
                    }
                    expected++;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error occurred in FindMissingSequences method: ", ex);
                throw;
            }
            return missing;
        }
    }
}
