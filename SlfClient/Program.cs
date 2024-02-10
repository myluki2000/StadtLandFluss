using System.Runtime.InteropServices;

namespace SlfClient
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Config config = new("./config.txt");

            ApplicationConfiguration.Initialize();
            Application.Run(new StartForm(config));
        }
    }
}