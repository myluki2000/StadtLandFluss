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

            // TODO: make packets reliable


        }
    }
}
