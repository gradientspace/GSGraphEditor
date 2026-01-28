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

        // these have to return a task because they may spawn a filesystem dialog
        // and blocking on them can hang the UI thread in some UI frameworks...
        public Task<bool> TrySave();
        public Task<bool> TrySaveAs();
        public Task<bool> TryOpen();
        public Task<bool> TryImport();
    }
}
