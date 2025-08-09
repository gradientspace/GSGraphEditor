// Copyright Gradientspace Corp. All Rights Reserved.
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public static class GlobalTypeCache
    {
        public struct NamedType
        {
            public Type type;
            public string name;
            public string systemName;
        }

        public static List<NamedType> AllTypes = new List<NamedType>();
        public static Dictionary<string, string> ShortNameMap = new Dictionary<string, string>();


        public static string TryGetShortName(string s)
        {
            if (ShortNameMap.TryGetValue(s, out string? shortName))
                return shortName;
            return s;
        }


        public static void FindTypesByPrefixMatch(string MatchText, ref List<NamedType> Results, int MaxResults = 10)
        {
            initialize_all_types();

            Results.Clear();
            List<NamedType> useList = FirstCharLists[GetFirstCharIndex(MatchText[0])];
            foreach (NamedType typeInfo in useList) 
            {
                if (typeInfo.name.StartsWith(MatchText, StringComparison.InvariantCultureIgnoreCase)) {
                    Results.Add(typeInfo);
                    continue;
                }

                if (Results.Count > MaxResults)
                    return;
            }
        }


        // lists sorted by first character...
        private static List<NamedType>[] FirstCharLists = new List<NamedType>[0];
        private static int GetFirstCharIndex(Char c)
        {
            return Char.IsLetter(c) ? ((int)Char.ToUpper(c) - 65) : 26;
        }

        private static void initialize_all_types()
        {
            if (AllTypes.Count > 0)
                return;

            AllTypes = new List<NamedType>();
            ShortNameMap = new Dictionary<string, string>();

            // add alternate names for common types
            
            AllTypes.Add(new() { type = typeof(bool), name="bool", systemName = "Boolean" });
            AllTypes.Add(new() { type = typeof(byte), name="byte", systemName = "Byte" });
            AllTypes.Add(new() { type = typeof(sbyte), name="sbyte", systemName = "SByte" });
            AllTypes.Add(new() { type = typeof(int), name="int", systemName = "Int32" });
            AllTypes.Add(new() { type = typeof(long), name="long", systemName = "Int64" });
            AllTypes.Add(new() { type = typeof(short), name="short", systemName = "Int16" });
            AllTypes.Add(new() { type = typeof(uint), name="uint", systemName = "UInt32" });
            AllTypes.Add(new() { type = typeof(ulong), name="ulong", systemName = "UInt64" });
            AllTypes.Add(new() { type = typeof(ushort), name="ushort", systemName = "UInt16" });
            AllTypes.Add(new() { type = typeof(float), name="float", systemName = "Single" });
            AllTypes.Add(new() { type = typeof(double), name="double", systemName = "Double" });
            AllTypes.Add(new() { type = typeof(decimal), name="decimal", systemName = "Decimal" });
            AllTypes.Add(new() { type = typeof(char), name="char", systemName = "Char" });
            AllTypes.Add(new() { type = typeof(string), name="string", systemName = "String" });
            AllTypes.Add(new() { type = typeof(object), name="object", systemName = "Object" });

            // g3 vector math types...

            HashSet<Type> HandledTypes = new HashSet<Type>();
            foreach (var info in AllTypes) 
            {
                HandledTypes.Add(info.type);
                ShortNameMap.Add(info.systemName, info.name);
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) 
            {
                Type[] allTypes = assembly.GetTypes();
                foreach (Type type in allTypes) 
                {
                    if (type.IsPublic == false || type.IsAbstract) continue;
                    if (HandledTypes.Contains(type)) continue;

                    // todo special handling for templates?

                    NamedType info = new NamedType();
                    info.type = type;
                    info.name = type.Name;

                    AllTypes.Add(info);
                }
            }


            FirstCharLists = new List<NamedType>[27];
            for (int i = 0; i < FirstCharLists.Length; ++i)
                FirstCharLists[i] = new List<NamedType>();

            foreach (NamedType typeInfo in AllTypes) {
                int idx = GetFirstCharIndex(typeInfo.name[0]);
                FirstCharLists[idx].Add(typeInfo);
            }

        }




    }
}
