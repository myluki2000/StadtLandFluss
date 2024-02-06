using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlfServer.Game
{
    /// <summary>
    /// Used to validate words of the three categories (city/Stadt, country/Land, river/Fluss) by validating them
    /// against a list of valid solutions provided by txt files.
    /// </summary>
    public class WordValidator
    {
        private readonly HashSet<string> Cities;
        private readonly HashSet<string> Countries;
        private readonly HashSet<string> Rivers;

        public WordValidator()
        {
            Cities = File.ReadAllLines("./stadt.txt").Select(x => x.ToLower()).ToHashSet();
            Countries = File.ReadAllLines("./land.txt").Select(x => x.ToLower()).ToHashSet();
            Rivers = File.ReadAllLines("./fluss.txt").Select(x => x.ToLower()).ToHashSet();
        }

        /// <summary>
        /// Checks whether the specified city is contained in the list of valid cities and (if provided) whether it starts
        /// with the specified prefix.
        /// </summary>
        /// <param name="input">The input to validate.</param>
        /// <param name="prefix">If provided, the prefix with which the input should start to not fail validation.</param>
        /// <returns>True if input is valid, false otherwise.</returns>
        public bool ValidateCity(string input, string prefix = "")
        {
            return ValidateAgainstHashSet(input, prefix, Cities);
        }

        /// <summary>
        /// Checks whether the specified country is contained in the list of valid countries and (if provided) whether it starts
        /// with the specified prefix.
        /// </summary>
        /// <param name="input">The input to validate.</param>
        /// <param name="prefix">If provided, the prefix with which the input should start to not fail validation.</param>
        /// <returns>True if input is valid, false otherwise.</returns>
        public bool ValidateCountry(string input, string prefix = "")
        {
            return ValidateAgainstHashSet(input, prefix, Countries);
        }

        /// <summary>
        /// Checks whether the specified country is contained in the list of valid countries and (if provided) whether it starts
        /// with the specified prefix.
        /// </summary>
        /// <param name="input">The input to validate.</param>
        /// <param name="prefix">If provided, the prefix with which the input should start to not fail validation.</param>
        /// <returns>True if input is valid, false otherwise.</returns>
        public bool ValidateRiver(string input, string prefix = "")
        {
            return ValidateAgainstHashSet(input, prefix, Rivers);
        }

        /// <summary>
        /// Helper method.Checks whether the specified input is contained in the specified HashSet and (if a prefix is provided) whether
        /// the input starts with the specified prefix.
        /// </summary>
        /// <param name="input">The input to validate.</param>
        /// <param name="prefix">Prefix with which the input should start to not fail validation.</param>
        /// <param name="validAnswers">HashSet which contains all valid answers.</param>
        /// <returns>True if input is valid, false otherwise.</returns>
        private bool ValidateAgainstHashSet(string input, string prefix, HashSet<string> validAnswers)
        {
            return input.ToLower().StartsWith(prefix.ToLower()) && validAnswers.Contains(input.ToLower());
        }
    }
}
