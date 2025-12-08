// Copyright Gradientspace Corp. All Rights Reserved.
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public static class NodeEditorSecrets
    {
        public static string UserSecretsFilePath {
            get {
                return get_or_init_secrets_file();
            }
        }
        public static string UserSecretsFileName {
            get { return ".editor_secrets.txt"; }
        }

        private static string get_or_init_secrets_file()
        {
            string ConfigFolderPath = NodeEditorConfig.UserEditorConfigFolder;
            string SecretsFile = Path.Combine(ConfigFolderPath, UserSecretsFileName);
            if (File.Exists(SecretsFile) == false) {
                List<string> lines = new List<string>();
                lines.Add($"{ISecretsSource.ANTHROPIC_API_KEY}=");
                File.WriteAllLines(SecretsFile, lines);
            }
            return SecretsFile;
        }





        public static List<(string,string)> GetAllSecrets()
        {
            string SecretsFile = get_or_init_secrets_file();

            List<(string, string)> Secrets = new();
            foreach (string line in File.ReadLines(SecretsFile)) {
                int idx = line.IndexOf('=');
                if (idx == -1)
                    continue;
                string Key = line.Substring(0, idx);
                string Secret = line.Substring(idx+1).Trim();
                Secrets.Add(new(Key, Secret));
            }

            return Secrets;
        }


        public static void StoreAllSecrets(List<(string,string)> Secrets)
        {
            string SecretsFile = get_or_init_secrets_file();

            List<string> Lines = new();
            foreach (var pair in Secrets)
                Lines.Add($"{pair.Item1}={pair.Item2}");

            File.WriteAllLines(SecretsFile, Lines);
        }


        public static bool FindSecret(string SecretName, out string Secret)
        {
            string SecretsFile = get_or_init_secrets_file();
            Secret = "";

            foreach (string line in File.ReadLines(SecretsFile)) {
                int idx = line.IndexOf('=');
                if (idx == -1)
                    continue;
                if ( line.Substring(0, idx).Equals(SecretName, StringComparison.Ordinal) ) {
                    string rest = line.Substring(idx+1);
                    Secret = rest.Trim();
                    break;
                }
            }

            return Secret.Length > 0;
        }

    }


    public class NodeEditorSecretsImpl : Gradientspace.NodeGraph.ISecretsSource
    {
        public bool FindSecret(string SecretName, out string Secret)
        {
            return NodeEditorSecrets.FindSecret(SecretName, out Secret);
        }
    }

}
