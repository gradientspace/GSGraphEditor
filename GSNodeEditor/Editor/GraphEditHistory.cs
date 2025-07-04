using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSNodeEditor
{
    public interface IGraphEditChange
    {
        public string Name { get; }
		public string Description { get; }

		public void Apply();
        public void Revert();

        public void SetNameAndDescription(string? name = null, string? description = null) { }
    }

    public abstract class BaseGraphEditChange : IGraphEditChange
    {
        public string Name { get; set; } = "BaseChange";
        public string Description { get; set; } = "";

        public BaseGraphEditChange() { }
		public BaseGraphEditChange(string? name, string? description) {
            Name = name ?? "BaseChange";
            Description = description ?? "";
		}

		public override string ToString()
		{
            return $"{Name} : {GetType().ToString()}";
		}

		public abstract void Apply();
		public abstract void Revert();

		public virtual void SetNameAndDescription(string? name = null, string? description = null) {
            if (name != null) Name = name; 
            if (description != null) Description = description;
        }
	}

    public class GraphEditChangeSet : BaseGraphEditChange
	{
        public List<IGraphEditChange> Changes = new List<IGraphEditChange>();

        public GraphEditChangeSet() { }
        public GraphEditChangeSet(string? name, string? description) : base(name, description) { }
		public GraphEditChangeSet(List<IGraphEditChange> changes) {
            Changes = changes;
        }

        public override void Apply()
        {
			int N = Changes.Count;
			for (int i = 0; i < N; ++i)
				Changes[i].Apply();
		}

		public override void Revert()
        {
            int N = Changes.Count;
            for (int i = N-1; i >= 0; --i)
                Changes[i].Revert();
		}

        public IGraphEditChange? Simplify()
        {
            if (Changes.Count == 0)
                return null;
            if (Changes.Count == 1) {
                Changes[0].SetNameAndDescription(this.Name, this.Description);
                return Changes[0];
			}
            return this;
        }
    }


    public class GraphEditHistory
    {
        List<IGraphEditChange> Changes = new List<IGraphEditChange>();

        int CurrentStateIndex = 0;

        bool inActiveStepForward = false;
        bool inActiveStepBackward = false;

		GraphEditChangeSet? NewChangeSequence = null;
        int BeginChangesStackDepth = 0;


        public bool LogHistoryChanges = true;


        public void BeginChanges(string? changeName = null, string? changeDescription = null)
        {
            Debug.Assert(inActiveStepBackward == false && inActiveStepForward == false);

            Truncate();

            if (BeginChangesStackDepth == 0)
                Debug.Assert(NewChangeSequence == null);
            if (NewChangeSequence == null)
                NewChangeSequence = new GraphEditChangeSet(changeName, changeDescription);
            BeginChangesStackDepth++;
		}

        public bool InActiveChanges { get 
            {  return (NewChangeSequence != null); }
        }

        public void AppendChange(IGraphEditChange? change)
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
				IGraphEditChange? minChange = NewChangeSequence.Simplify();
				NewChangeSequence = null;
                if ( minChange != null ) {
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
