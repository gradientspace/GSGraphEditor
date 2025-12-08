// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Meshmixer.Nodes
{
    internal static class Convert
    {
        public static vec3f ToVec3f(Vector3d v)
        {
            vec3f vec = new vec3f();
            vec.x = (float)v.x; vec.y = (float)v.y; vec.z = (float)v.z;
            return vec;
        }
        public static vec3f ToVec3f(Vector3f v)
        {
            vec3f vec = new vec3f();
            vec.x = (float)v.x; vec.y = (float)v.y; vec.z = (float)v.z;
            return vec;
        }
    }

    internal static class MMUtil
    {
        internal static bool IsMeshmixerRunning()
        {
            Process[] all = Process.GetProcesses();
            bool bFound = false;
            foreach (Process p in all) {
                if (p.ProcessName.Contains("Meshmixer", StringComparison.OrdinalIgnoreCase)) {
                    bFound = true;
                    break;
                }
            }
            return bFound;
        }


        internal static bool TryLaunchMeshMixerInstance(string InitialFilePath = "")
        {
            string MeshmixerPath = "C:\\Program Files\\Autodesk\\Meshmixer\\Meshmixer.exe";
            ProcessStartInfo psi = new ProcessStartInfo {
                FileName = MeshmixerPath,
                Arguments = InitialFilePath,
                UseShellExecute = false, // Crucial for redirecting input/output and hiding the window
                CreateNoWindow = true,   // Hides the process window
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            try {
                Process process = Process.Start(psi);
                if (process != null) {
                    Thread.Sleep(1000);
                    return true;
                }
                return false;
            } catch (Exception ex) {
                GlobalGraphOutput.AppendError(ex.Message);
                return false;
            }
        }
    }
}
