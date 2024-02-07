using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfClient
{
    public class Config
    {
        public Guid PlayerId { get; }

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
