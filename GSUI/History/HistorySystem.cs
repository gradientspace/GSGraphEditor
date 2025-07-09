using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gradientspace.UI
{
    public interface IHistoryImplementation
    {
        void BeginChanges(string? changeName = null, string? changeDescription = null);
        void AppendChange(IHistoryChange? change);
        bool InActiveChanges { get; }
        void EndChanges();

        void TryStepForward();
        void TryStepBackward();

        bool InUndoRedo { get; }
    }


    public static class HistorySystem
    {
        private static IHistoryImplementation? activeHistory = null;

        public static void SetActiveHistory(IHistoryImplementation? useHistory)
        {
            activeHistory = useHistory;
        }

        public static void ClearActiveHistory()
        {
            activeHistory = null;
        }

        public static IHistoryImplementation? ActiveHistory { 
            get {
                return activeHistory;
            } 
        }

    }
}
