using Gradientspace.NodeGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
	public class GraphStaticAnalyzer
	{
		public ExecutionGraph Graph { get; protected set; }

		public VariablesTracker Variables { get; protected set; }

		public GraphStaticAnalyzer(ExecutionGraph graph)
		{
			Graph = graph;
			Variables = new VariablesTracker(graph);
		}


		public virtual void RebuildAll()
		{
			Variables.Rebuild();
		}
	}
}
