// Copyright Gradientspace Corp. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gradientspace.UI;

namespace GSNodeEditor
{

    // todo maybe should be part of Gradientspace.UI? nothing graph-specific here...
    public class GraphEditHistory : IHistoryImplementation
	{
        List<IHistoryChange> Changes = new List<IHistoryChange>();

        int CurrentStateIndex = 0;

        bool inActiveStepForward = false;
        bool inActiveStepBackward = false;

		HistoryChangeSequence? NewChangeSequence = null;
        int BeginChangesStackDepth = 0;


        public bool LogHistoryChanges = true;


        public void BeginChanges(string? changeSetName = null, string? changeSetDescription = null)
        {
            Debug.Assert(inActiveStepBackward == false && inActiveStepForward == false);

            Truncate();

            if (BeginChangesStackDepth == 0)
                Debug.Assert(NewChangeSequence == null);
            if (NewChangeSequence == null)
                NewChangeSequence = new HistoryChangeSequence(changeSetName, changeSetDescription);
            BeginChangesStackDepth++;
		}

		public void BeginChanges(HistoryChangeSequence UseChangeSet)
        {
			Debug.Assert(inActiveStepBackward == false && inActiveStepForward == false);
            // todo: could we support stack of ChangeSequence to allow these to be nested?
            Debug.Assert(BeginChangesStackDepth == 0 && NewChangeSequence == null);

			Truncate();
            NewChangeSequence = UseChangeSet;
			BeginChangesStackDepth++;
		}

		public bool InActiveChanges { get 
            {  return (NewChangeSequence != null); }
        }

        public void AppendChange(IHistoryChange? change)
        {
            if (change != null) {
                Debug.Assert(InActiveChanges && NewChangeSequence != null);
                NewChangeSequence.Changes.Add(change);
            }
        }

        public void EndChanges()
        {
            Debug.Assert(BeginChangesStackDepth > 0);
            if (BeginChangesStackDepth == 0)
                return;
            BeginChangesStackDepth--;
            if (BeginChangesStackDepth == 0)
            {
				Debug.Assert(NewChangeSequence != null);
				IHistoryChange? minChange = NewChangeSequence.Simplify();
				NewChangeSequence = null;
                if ( minChange != null ) {
                    System.Diagnostics.Debug.WriteLine("[History] Pushed change " + minChange.Name + " // " + minChange.Description);
					Changes.Add(minChange);
                    CurrentStateIndex = Changes.Count;
                }
            }
		}


        //! Redo
        public void TryStepForward()
        {
			Debug.Assert(NewChangeSequence == null);
            Debug.Assert(inActiveStepForward == false && inActiveStepBackward == false);

			if (CurrentStateIndex < Changes.Count )
            {
                inActiveStepForward = true;
				if (LogHistoryChanges)
					Debug.WriteLine($"[History:{CurrentStateIndex}] Apply {Changes[CurrentStateIndex]}");
                Changes[CurrentStateIndex].Apply();
                CurrentStateIndex++;
                inActiveStepForward = false;
            }

		}

        //! Undo
		public void TryStepBackward()
		{
			Debug.Assert(NewChangeSequence == null);
			Debug.Assert(inActiveStepForward == false && inActiveStepBackward == false);

			if (CurrentStateIndex > 0)
			{
				inActiveStepBackward = true;
                if (LogHistoryChanges)
				    Debug.WriteLine($"[History:{CurrentStateIndex}] Revert {Changes[CurrentStateIndex-1]}");
				Changes[CurrentStateIndex-1].Revert();
				CurrentStateIndex--;
				inActiveStepBackward = false;
			}
		}


        public bool InUndoRedo { 
            get { return inActiveStepForward || inActiveStepBackward; } 
        }


		public void Truncate()
        {
            Debug.Assert(NewChangeSequence == null);
			Debug.Assert(inActiveStepForward == false && inActiveStepBackward == false);

			if (CurrentStateIndex != Changes.Count) {
                // need to do any freeing here??
                Changes.RemoveRange(CurrentStateIndex, Changes.Count-CurrentStateIndex);
            }
        }

    }
}
