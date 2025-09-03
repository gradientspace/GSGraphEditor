// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public sealed class StringValidators
    {
        private StringValidators() { }


        public static bool IsIntegerCharacter(char c)
        {
            return Char.IsAsciiDigit(c) || c == '-';
        }
        public static bool IsPositiveIntegerCharacter(char c)
        {
            return Char.IsAsciiDigit(c);
        }

        public static bool IsRealCharacter(char c)
        {
            return Char.IsAsciiDigit(c) || c == '-' || c == '.';
        }
        public static bool IsPositiveRealCharacter(char c)
        {
            return Char.IsAsciiDigit(c) || c == '.';
        }

        public static bool IsVectorRealCharacter(char c)
        {
            return Char.IsAsciiDigit(c) || c == '-' || c == '.' || c == ' ' || c == ',';
        }

        public static bool IsIntegerString(string s)
        {
            if (s.Length == 0
                || (s.Length == 1 && (s[0] == '-')) )
                return true;
            return int.TryParse(s, out var IntValue);
        }

        public static bool IsRealString(string s)
        {
            return double.TryParse(s, out var RealValue);
        }
        public static bool IsRealString_TextEntry(string s)
        {
            if (s.Length == 0 
                || (s.Length == 1 && (s[0] == '-' || s[0] == '.'))
                || (s.Length == 2 && s[0] == '-' && s[1] == '.') )
                return true;
            return double.TryParse(s, out var RealValue);
        }

        public static bool IsVectorRealString_TextEntry(string s)
        {
            // note this doesn't prevent typing invalid text, unlike other functions above
            for (int k = 0; k < s.Length; ++k)
                if (IsVectorRealCharacter(s[k]) == false)
                    return false;
            return true;
        }

    }
}
