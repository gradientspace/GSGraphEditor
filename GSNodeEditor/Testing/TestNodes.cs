// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.NodeGraph.Nodes
{

    public class TestGraphData
    {
        public float FloatVal = 0.5f;
        public bool BoolVal = false;

        float _floatprop = 1.0f;
        public float FloatProp
        {
            get { return _floatprop; }
            set { _floatprop = value; }
        }

        public float FloatValWrap
        {
            get { return _floatprop; }
        }

        public readonly int ReadOnlyInt = 7;

        public float InitFloat { get; init; } = 13;
        public float PrivateFloat { get; private set; } = 14;
    }


    [NodeFunctionLibrary("AAAATest")]
    public static class TestNodeFunctionLibrary
    {
        [NodeFunction]
        public static TestGraphData MakeTestData()
        {
            return new TestGraphData();
        }

        [NodeFunction]
        public static void PrintTestData(TestGraphData Object)
        { 
            Debug.WriteLine("PrintTestData: FloatVal {0}  BoolVal {1}  FloatProp {2}", Object.FloatVal, Object.BoolVal, Object.FloatProp);
        }

        [NodeFunction]
        public static float[] ArrayTest()
        {
            return new float[7] { 1, 2, 3, 4, 5, 6, 7 };
        }

        [NodeFunction]
        public static List<float> ListTest()
        {
            return new List<float> { 1, 2, 3 };
        }

        [NodeFunction]
        public static IEnumerable<float> EnumerableTest()
        {
            for (int k = 0; k < 12; ++k)
                yield return k;
        }
    }




    [NodeFunctionLibrary("Geometry3.Test")]
    public static class G3TestFunctions
    {
        [NodeFunction]
        [NodeParameter("vecParam", DisplayName = "vec3vec", DefaultRealVec = [2.0,3.0,4.2])]
        public static Vector3d TestVec3Default(Vector3d vecParam)
        {
            return vecParam;
        }


        [NodeFunction(ReturnName ="monkey")]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? TestDMesh3Attribs()
        {
            GridBox3Generator BoxGen = new GridBox3Generator() { EdgeVertices = 3 };
            DMesh3 Mesh = BoxGen.Generate().MakeDMesh();

            Mesh.Attribs.EnableTriNormals();
            Mesh.Attribs.EnableTriUVs(4);

            foreach (int tid in Mesh.TriangleIndices())
                Mesh.Attribs.MaterialID.SetValue(tid, tid);

            Mesh.CheckValidity();
            foreach (int tid in Mesh.TriangleIndices())
                g3.Util.gDevAssert(Mesh.Attribs.MaterialID.GetValue(tid) == tid);

            int a = Mesh.AppendVertex(Vector3d.Zero), b = Mesh.AppendVertex(Vector3d.Zero), c = Mesh.AppendVertex(Vector3d.Zero);
            int newtid = Mesh.AppendTriangle(a, b, c);
            Mesh.CheckValidity();
            Mesh.Attribs.MaterialID.SetValue(newtid, newtid);

            foreach (int tid in Mesh.TriangleIndices())
                g3.Util.gDevAssert(Mesh.Attribs.MaterialID.GetValue(tid) == tid);

            int[] remove = [0, 4];
            foreach (int rid in remove)
                Mesh.RemoveTriangle(rid);
            Mesh.CheckValidity();
            foreach (int tid in Mesh.TriangleIndices())
                g3.Util.gDevAssert(Mesh.Attribs.MaterialID.GetValue(tid) == tid);

            Mesh.CompactInPlace();
            Mesh.CheckValidity();
            foreach (int tid in Mesh.TriangleIndices())
                g3.Util.gDevAssert(remove.Contains(Mesh.Attribs.MaterialID.GetValue(tid)) == false);

            int eid = Mesh.GetTriEdges(0).a;
            MeshResult splitResult = Mesh.SplitEdge(eid, out DMesh3.EdgeSplitInfo splitInfo);
            g3.Util.gDevAssert(splitResult == MeshResult.Ok);
            Mesh.CheckValidity();

            MeshResult pokeResult = Mesh.PokeTriangle(5, out DMesh3.PokeTriangleInfo pokeInfo);
            g3.Util.gDevAssert(pokeResult == MeshResult.Ok);
            Mesh.CheckValidity();

            Index2i edgev = Mesh.GetEdgeV(pokeInfo.new_edges.c);
            MeshResult collapseResult = Mesh.CollapseEdge(edgev.a, edgev.b, out DMesh3.EdgeCollapseInfo collapseInfo);
            g3.Util.gDevAssert(collapseResult == MeshResult.Ok);
            Mesh.CheckValidity();

            MeshEditor editor = new MeshEditor(Mesh);
            editor.SeparateTriangles([collapseInfo.eKept0], true, out List<Index2i> EdgePairs);
            Mesh.CheckValidity();

            MeshResult mergeResult = Mesh.MergeEdges(EdgePairs[0].a, EdgePairs[0].b, out DMesh3.MergeEdgesInfo merge_info);
            g3.Util.gDevAssert(mergeResult == MeshResult.Ok);
            Mesh.CheckValidity();

            int flipedge = Mesh.EdgeCount/2;
            while (Mesh.IsEdge(flipedge) == false)
                flipedge++;
            MeshResult flipResult = Mesh.FlipEdge(flipedge, out DMesh3.EdgeFlipInfo flipInfo);
            g3.Util.gDevAssert(flipResult == MeshResult.Ok);
            Mesh.CheckValidity();


            Mesh.Attribs.SetNumUVChannels(2);
            Mesh.CheckValidity();
            Mesh.Attribs.DisableTriUVs();
            Mesh.Attribs.DisableTriNormals();
            Mesh.Attribs.DisableMaterialID();
            Mesh.CheckValidity();

            return Mesh;
        }


        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? TestDMesh3TriUVs()
        {
            StandardMeshWriter writer = new StandardMeshWriter();
            WriteOptions writeOpt = new WriteOptions() { bPerVertexColors = true };

            GriddedRectGenerator RectGen = new GriddedRectGenerator();
            DMesh3 Mesh = RectGen.Generate().MakeDMesh();

            TriUVsGeoAttribute uvSet = Mesh.Attribs.TriUVChannel(0);
            foreach (int tid in Mesh.TriangleIndices()) {
                Mesh.GetTriVertices(tid, out Triangle3d tri);
                Vector3d c = Mesh.GetTriCentroid(tid);
                Vector2f tx = (c.x > 0) ? new Vector2f(0.1, 0) : Vector2f.Zero;

                uvSet.SetValue(tid, new TriUVs() { A = (Vector2f)tri.V0.xz+tx, B = (Vector2f)tri.V1.xz+tx, C = (Vector2f)tri.V2.xz+tx });
            }
            Mesh.CheckValidity();

            Mesh.EnableVertexColors(new Vector3f(1, 0, 0));
            VertexColorsGeoAttribute vertexColors = Mesh.Attribs.VertexColor;
            for (int vid = 0; vid < Mesh.MaxVertexID; vid += 2)
                vertexColors.SetValue(vid, new Vector3f(0, 0, 1));

            writer.Write("C:\\scratch\\AAA_UV_TEST_0_orig.obj", [new WriteMesh(Mesh)], writeOpt);

            Mesh.PokeTriangle(7, out DMesh3.PokeTriangleInfo pokeInfo);
            Mesh.CheckValidity();

            writer.Write("C:\\scratch\\AAA_UV_TEST_1_poked.obj", [new WriteMesh(Mesh)], writeOpt);

            List<int> testEdges = new();
            for (int i = 0; i < Mesh.MaxEdgeID; i += 3) testEdges.Add(i);
            foreach (int eid in testEdges) {
                if (Mesh.IsEdge(eid))
                    Mesh.SplitEdge(eid, out DMesh3.EdgeSplitInfo splitInfo, 0.25);
            }
            Mesh.CheckValidity();

            writer.Write("C:\\scratch\\AAA_UV_TEST_2_split.obj", [new WriteMesh(Mesh)], writeOpt);

            IndexedUVMesh uvMesh = new IndexedUVMesh(Mesh, uvSet);
            WriteMesh writeMesh = new WriteMesh() { Mesh = Mesh, UVs = uvMesh };
            writer.Write("C:\\scratch\\AAA_UV_TEST_3_withtriuv.obj", [writeMesh], writeOpt);

            DMesh3 converted = uvMesh.ToDMesh3();
            writer.Write("C:\\scratch\\AAA_UV_TEST_3_uvmesh.obj", [new WriteMesh(converted)], writeOpt);

            return Mesh;
        }



        [NodeFunction]
        [NodeReturnValue(DisplayName = "Mesh")]
        public static DMesh3? TestDMesh3PerVertAttribs()
        {
            StandardMeshWriter writer = new StandardMeshWriter();
            WriteOptions writeOpt = new WriteOptions() { bPerVertexColors = true, bPerVertexUVs = true };

            GriddedRectGenerator RectGen = new GriddedRectGenerator();
            RectGen.Height = 1.5f;
            RectGen.WantUVs = true;
            RectGen.UVMode = TrivialRectGenerator.UVModes.FullUVSquare;
            DMesh3 Mesh = RectGen.Generate().MakeDMesh();

            Mesh.EnableVertexColors(new Vector3f(1, 0, 0));
            VertexColorsGeoAttribute vertexColors = Mesh.Attribs.VertexColor;
            for (int vid = 0; vid < Mesh.MaxVertexID; vid += 2)
                vertexColors.SetValue(vid, new Vector3f(0, 0, 1));

            writer.Write("C:\\scratch\\AAA_TEST_PerVert_0_orig.obj", [new WriteMesh(Mesh)], writeOpt);

            writer.Write("C:\\scratch\\AAA_TEST_PerVert_0_uvmesh.obj",
                [new WriteMesh(IndexedUVMesh.FromPerVertexUVs(Mesh).ToDMesh3())], writeOpt);


            Mesh.PokeTriangle(7, out DMesh3.PokeTriangleInfo pokeInfo);
            Mesh.CheckValidity();

            writer.Write("C:\\scratch\\AAA_TEST_PerVert_1_poked.obj", [new WriteMesh(Mesh)], writeOpt);

            List<int> testEdges = new();
            for (int i = 0; i < Mesh.MaxEdgeID; i += 3) testEdges.Add(i);
            foreach (int eid in testEdges) {
                if (Mesh.IsEdge(eid))
                    Mesh.SplitEdge(eid, out DMesh3.EdgeSplitInfo splitInfo, 0.25);
            }
            Mesh.CheckValidity();

            writer.Write("C:\\scratch\\AAA_TEST_PerVert_2_split.obj", [new WriteMesh(Mesh)], writeOpt);

            writer.Write("C:\\scratch\\AAA_TEST_PerVert_2_uvmesh.obj",
                [new WriteMesh(IndexedUVMesh.FromPerVertexUVs(Mesh).ToDMesh3())], writeOpt);

            Mesh.RemoveVertex(37);
            DMesh3 CompactedMesh = new DMesh3(Mesh, true);
            CompactedMesh.CheckValidity();
            Mesh.CompactInPlace();
            Mesh.CheckValidity();

            writer.Write("C:\\scratch\\AAA_TEST_PerVert_3_compact.obj", [new WriteMesh(CompactedMesh)], writeOpt);

            return Mesh;
        }

    }



}
