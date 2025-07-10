// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

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


    [GraphNodeFunctionLibrary("AAAATest")]
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




}
