using System.Net;
using System.Net.Sockets;

namespace SlfServer
{
    internal class Program
    {
        private static UdpClient udpClient;

        static async Task Main(string[] args)
        {
            udpClient = new UdpClient(1337);

            // may need explicit network adapter to work
            udpClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.137"));

            // TODO: make packets reliable


        }
    }
}
