using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfCommon
{
    public class MatchRound
    {
        public string Letter { get; set; }

        public Dictionary<Guid, Answers> PlayerAnswers { get; } = new();

        public MatchRound(string letter)
        {
            Letter = letter;
        }

        public struct Answers
        {
            public string City { get; }
            public string Country { get; }
            public string River { get; }

            public Answers(string city, string country, string river)
            {
                City = city;
                Country = country;
                River = river;
            }
        }
    }
}
