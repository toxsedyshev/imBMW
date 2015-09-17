/// <summary>
/// Should be removed after bug fixed in .NET MF.
/// More info here: https://www.ghielectronics.com/community/forum/topic?id=17454
/// </summary>

namespace System.Diagnostics
{
    public enum DebuggerBrowsableState
    {
        Collapsed,
        Never,
        RootHidden
    }
}