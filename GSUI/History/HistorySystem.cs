
namespace Gradientspace.UI
{
    public interface IHistoryImplementation
    {
        void BeginChanges(string? changeSetName = null, string? changeSetDescription = null);
		void BeginChanges(HistoryChangeSequence UseChangeSet);
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
