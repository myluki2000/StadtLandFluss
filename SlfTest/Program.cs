using System.Net;
using SlfCommon;
using SlfCommon.Networking;
using SlfCommon.Networking.Packets;

namespace SlfTest
{
    internal class Program
    {
        private static readonly NetworkingClient networkingClient;
        private static readonly Guid identity = Guid.NewGuid();

        static Program()
        {
            networkingClient = new NetworkingClient(identity, IPAddress.Parse("239.0.0.1"));
        }

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
            Console.WriteLine("What are your words?");
            Console.Write("City: > ");
            string? city = null;
            while (string.IsNullOrEmpty(city))
                city = Console.ReadLine();

            Console.Write("Country: > ");
            string? country = null;
            while (string.IsNullOrEmpty(country))
                country = Console.ReadLine();

            Console.Write("River: > ");
            string? river = null;
            while (string.IsNullOrEmpty(river))
                river = Console.ReadLine();

            SubmitWordsPacket packet = new(identity, Guid.NewGuid(), city, country, river);

            Console.WriteLine("Drop packet on purpose? [y/n]");
            Console.Write("> ");

            bool drop;
            while (true)
            {
                string? input = Console.ReadLine();

                if (input?.ToLower() is not ("y" or "n"))
                    continue;

                drop = input.ToLower() == "y";
                break;
            }

            networkingClient.SendOrderedReliableToGroup(packet, drop);
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
