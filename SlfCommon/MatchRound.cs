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
            public Answer City { get; }
            public Answer Country { get; }
            public Answer River { get; }

            public Answers(Answer city, Answer country, Answer river)
            {
                City = city;
                Country = country;
                River = river;
            }
        }

        public struct Answer
        {
            public string Text { get; }
            public bool Accepted { get; }

            public Answer(string text, bool accepted)
            {
                Text = text;
                Accepted = accepted;
            }
        }
    }
}
