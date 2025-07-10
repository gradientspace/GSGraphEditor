// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
	public interface IGraphEditorActions
	{
		public bool TryNewExecutionGraph();
		public bool TrySave();
		public bool TrySaveAs();
		public bool TryOpen();
	}
}
