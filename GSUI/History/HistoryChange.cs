
namespace Gradientspace.UI
{

    public interface IHistoryChange
    {
		public string Name { get; }
		public string Description { get; }

		public void Apply();
		public void Revert();

		public void SetNameAndDescription(string? name = null, string? description = null) { }
	}



	public abstract class BaseHistoryChange : IHistoryChange
	{
		public string Name { get; set; } = "BaseChange";
		public string Description { get; set; } = "";

		public BaseHistoryChange() { }
		public BaseHistoryChange(string? name, string? description)
		{
			Name = name ?? "BaseChange";
			Description = description ?? "";
		}

		public override string ToString()
		{
			return $"{Name} : {GetType().ToString()}";
		}

		public abstract void Apply();
		public abstract void Revert();

		public virtual void SetNameAndDescription(string? name = null, string? description = null)
		{
			if (name != null) Name = name;
			if (description != null) Description = description;
		}
	}



	public class HistoryChangeSequence : BaseHistoryChange
	{
		public List<IHistoryChange> Changes = new List<IHistoryChange>();

		public HistoryChangeSequence() { }
		public HistoryChangeSequence(string? name, string? description) : base(name, description) { }
		public HistoryChangeSequence(List<IHistoryChange> changes)
		{
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
			for (int i = N - 1; i >= 0; --i)
				Changes[i].Revert();
		}

		public virtual IHistoryChange? Simplify()
		{
			if (Changes.Count == 0)
				return null;
			if (Changes.Count == 1)
			{
				Changes[0].SetNameAndDescription(this.Name, this.Description);
				return Changes[0];
			}
			return this;
		}
	}


}
