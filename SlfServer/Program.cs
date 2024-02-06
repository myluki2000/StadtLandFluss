using System.Net;
using System.Net.Sockets;
using System.Reflection;
using SlfCommon.Networking.Packets;

namespace SlfServer
{
    internal class Program
    {
        private static Server server;

        static async Task Main(string[] args)
        {
            Type[] packetTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.BaseType == typeof(SlfPacketBase)
                            && x is { IsClass: true, IsAbstract: false, Namespace: "SlfServer.Networking.Packets" })
                .ToArray();

            SlfPacketBase.RegisterTypes(packetTypes);

            packetTypes = SlfPacketBase.RegisteredPacketTypes;

            server = new Server();
            Console.WriteLine("Starting Server... Server ID: " + server.ServerId);
            Console.WriteLine("Starting leader election because server was just started...");
            server.StartElection();
        }
    }
}
