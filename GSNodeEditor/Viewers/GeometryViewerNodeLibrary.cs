// Copyright Gradientspace Corp. All Rights Reserved.
using g3;
using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    [NodeFunctionLibrary("GeometryViewer")]
    public static class GeometryViewerNodeLibrary
    {
        public static void Initialize()
        {
        }


        [NodeFunction]
        //[NodeParameter("Path", DisplayName = "OutputPath", DefaultValue = "c:\\scratch\\AA_FROM_GRAPH.obj")]
        public static void DisplayMesh(DMesh3 Mesh, string Name = "mesh1", bool FitToView = false )
        {
            GeometryViewer.Instance.DisplayMesh(Name, Mesh, FitToView);
        }
    }







}
