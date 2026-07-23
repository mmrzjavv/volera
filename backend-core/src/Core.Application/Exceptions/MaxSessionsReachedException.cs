namespace Core.Application.Exceptions;

public class MaxSessionsReachedException : InvalidOperationException
{
    public MaxSessionsReachedException(string message) : base(message) { }
}
