namespace Falah.Domain.Common;

/// <summary>
/// A rule violation an operator can see and act on (in Arabic, matching every other
/// user-facing message in this system) - as opposed to a bug, which throws something else.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
