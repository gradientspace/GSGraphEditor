using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using g3;

#if ENABLE_POLYSCOPE
using PolyscopeAdapter;
#endif

namespace GSNodeEditor
{
    public sealed class GeometryViewer
    {
        private static readonly GeometryViewer instance = new GeometryViewer();

        static GeometryViewer() { }
        private GeometryViewer() {
            PendingUpdates = new List<Action>();
        }

        public static GeometryViewer Instance {
            get {
                return instance;
            }
        }



        public void Test()
        {
            System.Diagnostics.Debug.WriteLine("LAUNCHING POLYSCOPE");
            LaunchIfNecessary();
        }

        public void UpdateMesh()
        {
            LaunchIfNecessary();
            test_update_mesh();
        }


        public void DisplayMesh(string mesh_name, DMesh3 mesh, bool bFitToView)
        {
            LaunchIfNecessary();

            DMesh3 useMesh = mesh;
            if (mesh.IsCompact == false)
            {
                useMesh = new DMesh3();
                useMesh.CompactCopy(mesh);
            }

            double[] Vertices = new double[3* useMesh.MaxVertexID];
            int i = 0;
            foreach (Vector3d v in useMesh.Vertices())
            {
                Vertices[i++] = v.x; Vertices[i++] = v.y; Vertices[i++] = v.z;
            }

            int[] Triangles = new int[3* useMesh.MaxTriangleID];
            i = 0;
            foreach (Index3i t in useMesh.Triangles())
            {
                Triangles[i++] = t.a; Triangles[i++] = t.b; Triangles[i++] = t.c;
            }

            Action update = () => {
                lock (polyscopeLock) {
#if ENABLE_POLYSCOPE
                    Polyscope.UpdateTriangleMesh(mesh_name, Vertices, Triangles);
                    if (bFitToView)
                        Polyscope.FitToView();
#endif
				}
			};

            lock (PendingUpdates) {
                PendingUpdates.Add(update);
            }
        }

        public bool IsLaunched { get; private set; } = false;


        public void Shutdown()
        {
            // if background thread is active, this will cause it to terminate
            lock (polyscopeLock)
            {
                ShuttingDown = true;
            }
        }



        //private readonly Lock polyscopeLock = new();
        private Object polyscopeLock = new Object();

        private bool ShuttingDown = false;

        private void LaunchIfNecessary()
        {
            // todo locking

            if (!IsLaunched)
            {
                Thread th = new Thread(new ThreadStart(this.launch_in_thread));
                th.Start();
                IsLaunched = true;
            }
        }


        // For some reason the calls to polyscope have to happen on the same thread it was Init()'d on.
        // Hits exception in C# marshalling, so possibly this is not actually due to polyscope...
        private List<Action> PendingUpdates;


        private void launch_in_thread()
        {
            lock (polyscopeLock)
            {
#if ENABLE_POLYSCOPE
                Polyscope.Init();
#endif

				//List<double> Vertices = new List<double>();
				//Vertices.Add(0); Vertices.Add(0); Vertices.Add(0);
				//Vertices.Add(1); Vertices.Add(0); Vertices.Add(0);
				//Vertices.Add(1); Vertices.Add(1); Vertices.Add(0);
				//Vertices.Add(0); Vertices.Add(1); Vertices.Add(0);

				//List<int> Triangles = new List<int>();
				//Triangles.Add(0); Triangles.Add(1); Triangles.Add(2);
				//Triangles.Add(0); Triangles.Add(2); Triangles.Add(3);

				//Polyscope.UpdateTriangleMesh("my mesh", Vertices.ToArray(), Triangles.ToArray());
			}

			bool bContinue = true;
            while (bContinue)
            {
                lock(polyscopeLock)
                {
#if ENABLE_POLYSCOPE
                    Polyscope.FrameTick();
#endif

					if (ShuttingDown)
                    {
#if ENABLE_POLYSCOPE
                        Polyscope.Unshow();
#endif
						//Polyscope.FrameTick();
						IsLaunched = false;
                        bContinue = false;
                        break;
                    }
                }

                lock (PendingUpdates)
                {
                    foreach (Action action in PendingUpdates) 
                        action.Invoke();
                    PendingUpdates.Clear();
                }

                Thread.Sleep(1);
            }
        }


        private void test_update_mesh()
        {
            Action update = () =>
            {
                List<double> Vertices = new List<double>();
                Vertices.Add(0); Vertices.Add(0); Vertices.Add(0);
                Vertices.Add(1); Vertices.Add(0); Vertices.Add(0);
                Vertices.Add(1); Vertices.Add(1); Vertices.Add(0);
                Vertices.Add(0); Vertices.Add(1); Vertices.Add(0);

                List<int> Triangles = new List<int>();
                Triangles.Add(0); Triangles.Add(1); Triangles.Add(2);

                lock (polyscopeLock)
                {
#if ENABLE_POLYSCOPE
                    Polyscope.UpdateTriangleMesh("my mesh", Vertices.ToArray(), Triangles.ToArray());
#endif
				}
			};

            lock (PendingUpdates)
            {
                PendingUpdates.Add(update);
            }
        }
    }
}
