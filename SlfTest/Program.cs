using System.Net;
using SlfCommon;
using SlfCommon.Networking;
using SlfCommon.Networking.Packets;

namespace SlfTest
{
    internal class Program
    {
        private static readonly NetworkingClient networkingClient = new(IPAddress.Parse("239.0.0.1"));
        private static readonly Guid identity = Guid.NewGuid();

        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            Console.WriteLine("My Identity: " + identity);
            Console.WriteLine("Please select an option:");
            Console.WriteLine("1. Send a test message");
            Console.WriteLine("2. Receive...");

            while (true)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();

                if (input == null || !int.TryParse(input, out int selectedNumber))
                    continue;

                switch (selectedNumber)
                {
                    case 1:
                        Send();
                        break;
                    case 2:
                        Receive();
                        break;
                }
            }
        }

        private static void Send()
        {
            SubmitWordsPacket packet = new(identity, "Stuttgart", "Germany", "Neckar");
            networkingClient.SendToMyGroup(packet);
        }

        private static void Receive()
        {
            while (true)
            {
                (IPAddress sender, SlfPacketBase packet) = networkingClient.Receive();

                Console.WriteLine("Received a packet of type " + packet.GetType().Name);
                string dump = ObjectDumper.Dump(packet);
                Console.WriteLine("  Contents:");
                Console.WriteLine(dump.Replace("\n", "\n    "));
            }
        }
    }
}
