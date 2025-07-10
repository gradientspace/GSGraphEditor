// Copyright Gradientspace Corp. All Rights Reserved.
using PopupWindow;
using System.Net;
using System.Net.Sockets;
using System.Text;

using System.Diagnostics;
using System.Reflection;
using g3;

namespace SkiaDrawTest
{
    internal class Program
    {

        // this will find implicit/explicit conversion but not constructors...
        public static MethodInfo? FindTypeConversionMethod(Type fromType, Type toType)
        {
            // can do similar search here but have to return ConstructorInfo? so it would be a separate function...
            //ConstructorInfo[] constructors = toType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

            for (int j = 0; j < 2; ++j)
            {
                Type searchType = (j == 0) ? fromType : toType;

                MethodInfo[] FromMethods = searchType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                foreach (MethodInfo implicitCast in FromMethods.Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == toType))
                {
                    ParameterInfo? pi = implicitCast.GetParameters().FirstOrDefault();
                    if (pi != null && pi.ParameterType == fromType)
                        return implicitCast;
                }
                foreach (MethodInfo explicitCast in FromMethods.Where(mi => mi.Name == "op_Explicit" && mi.ReturnType == toType))
                {
                    ParameterInfo? pi = explicitCast.GetParameters().FirstOrDefault();
                    if (pi != null && pi.ParameterType == fromType)
                        return explicitCast;
                }
            }
            return null;
        }


        static bool compare(Vector3d a, Vector3f b)
        {
            return a.x < (double)b.x;
        }

        static void Main(string[] args)
        {
            SnippetCompiler.PythonTests.TestPython1();

            Vector3d b = new Vector3d(1, 1, 1);
            //bool bok = compare(Vector3d.Zero, (Vector3f)(object)b );
            bool bAssignable = typeof(Vector3f).IsAssignableTo(typeof(Vector3d));

            MethodInfo? FoundConversionfd = FindTypeConversionMethod(typeof(Vector3f), typeof(Vector3d));
            MethodInfo? FoundConversiondf = FindTypeConversionMethod(typeof(Vector3d), typeof(Vector3f));

            object tmp = b;
            if (FoundConversiondf != null)
                tmp = FoundConversiondf.Invoke(null, new object[] { b } )!;

            //bool bConversion1 = HasImplicitConversion(typeof(Vector3f), typeof(Vector3d));
            bool bok = compare(Vector3d.Zero, (Vector3f)tmp);
            Debug.Assert(bok);

            //SnippetCompiler.CompileTests.TestLiveCompile();

            // todo investigate this repo: https://github.com/mathume/ResolveDependencies
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            // obviously bad, but do need to figure out dependencies somehow...
            if (File.Exists("D:\\git\\DerivativeGraph\\NodePacks\\LibIGLNodes\\LibIGLNodes.dll"))
            {
                Assembly IGLNodesAssembly = Assembly.LoadFile("D:\\git\\DerivativeGraph\\NodePacks\\LibIGLNodes\\LibIGLNodes.dll");
                Assembly IGLSharpAssembly = Assembly.LoadFile("D:\\git\\DerivativeGraph\\NodePacks\\LibIGLNodes\\LibIGLSharp.dll");
            }

            //AssemblyName[] refs = IGLNodesAssembly.GetReferencedAssemblies();
            //Type[] allTypes = IGLNodesAssembly.GetTypes();
            //MethodInfo[] methods = allTypes[0].GetMethods();
            //DMesh3 Tmp = new CappedCylinderGenerator().Generate().MakeDMesh();
            //object[] Parameters = new object[] { Tmp };
            //methods[0].Invoke(null, Parameters);

            GraphEditorPopupWindow TestWindow = new GraphEditorPopupWindow();
            TestWindow.Launch();

            //System.Console.WriteLine("ok I launched it...");
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
        {
            var assembly = ((AppDomain)sender!).GetAssemblies().FirstOrDefault(x => x.FullName == args.Name);
            return assembly;
        }
    }


}
