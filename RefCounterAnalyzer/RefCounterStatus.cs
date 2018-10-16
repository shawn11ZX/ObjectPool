using System.Text;
using Microsoft.CodeAnalysis;

public class RefCounterStatus
{
	private int _refCounter;
	private readonly StringBuilder _refInfo = new StringBuilder();
	private readonly StringBuilder _assignInfo = new StringBuilder();
	private bool _skip;
	private string _skipReason;
	private int _assignCounter;
    public int AssignCounter { get { return _assignCounter; } }
    public int RefCounter { get { return _refCounter; } }
	public string RefInfo { get { return _refInfo.ToString(); } }
    public string AssignInfo { get { return _assignInfo.ToString(); } }
	public bool IsSkip
	{
		get { return _skip; }
	}

	public string SkipReason
	{
		get { return _skipReason; }
	}

	

	public void IncAssignCounter(string reason, Location location)
	{
		_assignCounter++;
		_assignInfo.Append(reason + ToString(location) + " +1, ");
	}
	public void AcquireReference(string reason, Location location)
	{
		_refCounter++;
		_refInfo.Append(reason + ToString(location) + " +1, ");
	}

	public void ReleaseReference(string reason, Location location)
	{
		_refCounter--;
		_refInfo.Append(reason + ToString(location) + " -1, ");
	}

	public void RemainRef(string reason, Location location)
	{
		_refInfo.Append(reason + ToString(location) + " +0, ");
	}

	public void Skip(string reason, Location location)
	{
		_skip = true;
		_skipReason = reason + ToString(location);
	}

	public string ToString(Location l)
	{
		if (l == Location.None)
		{
			return " []";
		}
		else
		{
			return " [at line " + l.GetLineSpan().StartLinePosition + "]";
		}
	}
}