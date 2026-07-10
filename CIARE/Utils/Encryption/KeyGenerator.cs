using System;
using System.Collections.Generic;
using System.Security.Cryptography;


namespace CIARE.Utils.Encryption
{
    public static class KeyGenerator
    {
        // Cryptograhic password generator class.
        // Credits to: mkbmain
        private const string Alphabet = "abcdefghijklmnopqrstuvwxyz";
        private const string Numbers = "0123456789";
        private const string Symbols = "`~!@#$%^&*()-_=+[]{}\\|;:'\\,<.>/?";

        public static string GeneratePassword(int length = 16, bool useUpper = true, bool useLower = true,
            bool useSymbols = true, bool useNumbers = true)
        {
            if (length < 1)
            {
                throw new ArgumentException($"Can not make a string of {length} length");
            }

            var characterGroups = new List<string>();
            if (useLower) characterGroups.Add(Alphabet);
            if (useUpper) characterGroups.Add(Alphabet.ToUpperInvariant());
            if (useNumbers) characterGroups.Add(Numbers);
            if (useSymbols) characterGroups.Add(Symbols);

            if (characterGroups.Count == 0)
                throw new ArgumentException($"Can not make a string of {length} length while not using any chars");
            if (length < characterGroups.Count)
                throw new ArgumentException("Password length must accommodate every requested character group.");

            string allCharacters = string.Concat(characterGroups);
            var result = new char[length];
            int index = 0;
            foreach (string group in characterGroups)
                result[index++] = group[RandomNumberGenerator.GetInt32(group.Length)];

            while (index < result.Length)
                result[index++] = allCharacters[RandomNumberGenerator.GetInt32(allCharacters.Length)];

            for (int i = result.Length - 1; i > 0; i--)
            {
                int swapIndex = RandomNumberGenerator.GetInt32(i + 1);
                (result[i], result[swapIndex]) = (result[swapIndex], result[i]);
            }

            return new string(result);
        }
    }
}
