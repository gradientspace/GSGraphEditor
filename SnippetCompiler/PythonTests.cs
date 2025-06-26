using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Python.Runtime;
using GSPython;
using System.Globalization;

namespace SnippetCompiler
{
	public static class PythonTests
	{

		public static void TestPython1()
		{
			PythonSetup.InitializePython();


			// TODO should be part of PythonSetup somehow??
			// seems like we have to do this for graph evaluation running in background thread to work??
			// do not remove this until we have figured it out...

			// ??? does BeginAllowThreads() block or not-block the GIL thing?
			// see https://github.com/pythonnet/pythonnet/wiki/Threading
			var state = PythonEngine.BeginAllowThreads();



			//using (Py.GIL())
			//{
			//	PythonLibrary PyLibrary = PythonLibrary.ParseModuleFile("C:\\scratch\\TestFunction1.py");

			//	PyObject example_exec = PyLibrary.ModuleScope.Eval("TestFunction1(7,3.0,True)");


			//	PythonFunction execFunc = PyLibrary.FindFunctionByName("TestFunctionSum");

			//	object[] CSharpArgs = { 7, 12 };
			//	PyObject[] PyArgs = new PyObject[CSharpArgs.Length];
			//	for (int j = 0; j < CSharpArgs.Length; ++j)
			//		PyArgs[j] = CSharpArgs[j].ToPython();
			//	PyObject? result = PyLibrary.EvaluateFunction(execFunc, PyArgs);

			//	int intResult = result.ToInt32(CultureInfo.InvariantCulture);
			//	int intResult2 = result.As<int>();

			//	Console.WriteLine("derp");
			//	//}
			//}


			//using (Py.GIL())
			//{
			//	dynamic np = Py.Import("numpy");
			//	Console.WriteLine(np.cos(np.pi * 2));

			//	dynamic sin = np.sin;
			//	Console.WriteLine(sin(5));

			//	double c = (double)(np.cos(5) + sin(5));
			//	Console.WriteLine(c);

			//	dynamic a = np.array(new List<float> { 1, 2, 3 });
			//	Console.WriteLine(a.dtype);

			//	dynamic b = np.array(new List<float> { 6, 5, 4 }, dtype: np.int32);
			//	Console.WriteLine(b.dtype);

			//	Console.WriteLine(a * b);
			//	//Console.ReadKey();


			//	// this is useful to understand...
			//	// https://stackoverflow.com/questions/2220699/whats-the-difference-between-eval-exec-and-compile
			//	// 'Compile' can be used to precompile code (but how to execute?)
			//	// possibly output of Compile could be used to determine arguments/returnvals?

			//	using (PyModule scope = Py.CreateScope())
			//	{
			//		// exec returns local variables in localsOut
			//		var localsOut = new PyDict();
			//		scope.Exec("import numpy as np");
			//		PyObject p1 = scope.Exec("arr1 = np.array([1,2,3,4])", localsOut);

			//		// eval seems useless, eg eval("arr1") is an exception. Not clear what it's for in C# context...
			//		PyObject tmp2s = scope.Eval("np.array([1,2,3,4])");
			//		dynamic tmp = localsOut.GetItem("arr1");

			//		// apparently if the code was just a call to a function that returned a value,
			//		// eval() would return that value, while exec() would discard it (unless it was assigned to a local variable)
			//		// (eval does not allow for any statements, including assignment, etc)

			//	}

			//	//var locals = new PyDict();
			//	//string pythonScript = System.IO.File.ReadAllText("c:\\scratch\\test1.py");
			//	//PythonEngine.Exec(pythonScript, null, locals);
			//	//var array1 = locals.GetItem("arr1");

			//	//// enumerate local variables in locals array
			//	//// however this is dynamic...cannot query result w/o running it?
			//	//foreach (PyObject obj in locals)
			//	//{
			//	//	var value = locals.GetItem(obj);
			//	//	PyType type = value.GetPythonType();
			//	//	System.Console.WriteLine(obj);
			//	//}
			//}

			//PythonEngine.EndAllowThreads(state);

		}

	}
}
