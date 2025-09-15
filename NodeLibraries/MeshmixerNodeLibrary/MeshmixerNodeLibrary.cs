using g3;
using Gradientspace.NodeGraph;
using System.Diagnostics;

namespace Meshmixer.Nodes
{
    public static class MeshmixerNodeLibrary
    {
        public static void Initialize()
        {
        }
    }




    public class MMConnection : IDisposable
    {
        public mm.RemoteControl Connection = null;
        public bool Open()
        {
            Connection = new mm.RemoteControl();
            bool bConnected = Connection.Initialize();
            if (!bConnected)
                Connection = null;
            return bConnected;
        }

        public bool IsConnected => Connection != null;

        public void Close()
        {
            if (IsConnected)
                Connection.Shutdown();
            Connection = null;
        }

        ~MMConnection() {
            Close();
        }
        public void Dispose() {
            Close();
        }
    }


    [NodeFunctionLibrary("Meshmixer")]
    public unsafe static class MeshmixerLibrary
    {
        [NodeFunction]
        public static void ViewInMeshmixer(
            ref DMesh3 Mesh,
            bool NewInstance = false)
        {
            string TempFileName = System.IO.Path.GetTempFileName() + ".obj";
            StandardMeshWriter Writer = new StandardMeshWriter();
            WriteOptions opt = new WriteOptions() { bPerVertexColors = true };
            IOWriteResult result = Writer.Write(TempFileName, [new WriteMesh(Mesh)], opt);
            if (result.code != IOCode.Ok) {
                GlobalGraphOutput.AppendError($"Could not write temp OBJ file to {TempFileName}");
                return;
            }
            bool bRunning = MMUtil.IsMeshmixerRunning();
            if (!bRunning || NewInstance) {
                MMUtil.TryLaunchMeshMixerInstance(TempFileName);
                return;
            }
            MMConnection c = new MMConnection();
            if ( c.Open() == false ) {
                GlobalGraphOutput.AppendError($"Could not connect to Meshmixer");
                return;
            }
            c.Connection.ClearScene();
            c.Connection.ImportFile(TempFileName);

            try {
                File.Delete(TempFileName);
            } catch { }
        }








        [NodeFunction(ReturnName = "Connection")]
        public static MMConnection ConnectToMeshmixer(
            bool StartIfNeeded = true )
        {
            MMConnection c = new MMConnection();

            bool bRunning = MMUtil.IsMeshmixerRunning();
            if ( !bRunning && !StartIfNeeded ) {
                GlobalGraphOutput.AppendError("[ConnectToMeshmixer] Meshmixer is not running and StartIfNeeded=false");
                return c;
            }
            if (!bRunning) {
                MMUtil.TryLaunchMeshMixerInstance();
            }

            bool bOK = c.Open();
            if (!bOK) {
                GlobalGraphOutput.AppendError("[ConnectToMeshmixer] failed to connect");
            }
            return c;
        }


        [NodeFunction(ReturnName = "Connection")]
        public static void MMImportFile(ref MMConnection MM, string FilePath, bool bNewScene = true)
        {
            if (MM == null)
                return;
            if (bNewScene)
                MM.Connection.ClearScene();
            MM.Connection.ImportFile(FilePath);
        }


        [NodeFunction(ReturnName = "Connection")]
        public static void MMPlaneCut(
            ref MMConnection MM,
            Vector3d Location,
            Vector3d Normal )
        {
            if (MM == null)
                return;
            StoredCommands sc = new StoredCommands();
            Vector3f localCoord = MM.Connection.ToScene((Vector3f)Location);
            sc.AppendBeginToolCommand("planeCut");
            sc.AppendToolParameterCommand("origin", Convert.ToVec3f(localCoord));
            sc.AppendToolParameterCommand("normal", Convert.ToVec3f(Normal));
            sc.AppendCompleteToolCommand("accept");
            MM.Connection.ExecuteCommands(sc);
        }


    }


}
