using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfClient
{
    /// <summary>
    /// Loads/saves a config to/from a file.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// ID uniquely identifying this player.
        /// </summary>
        public Guid PlayerId { get; }

        /// <summary>
        /// File Path of the config file.
        /// </summary>
        public string ConfigPath { get; }

        public Config(string path)
        {
            ConfigPath = path;

            if(!File.Exists(ConfigPath))
                CreateDefaultConfig();

            using StreamReader sr = File.OpenText(path);

            PlayerId = Guid.Parse(sr.ReadLine());
        }

        public void CreateDefaultConfig()
        {
            using StreamWriter sw = new StreamWriter(ConfigPath);

            sw.WriteLine(Guid.NewGuid());
        }
    }
}
