namespace steel_structures_nodes.Data.Contracts;

public interface IDataAccessFailureNotifier
{
    event EventHandler<DataAccessFailureEventArgs>? FailureOccurred;

    DataAccessFailureEventArgs? LastFailure { get; }

    void Report(string source, string message, Exception? exception = null);
}

public sealed class DataAccessFailureEventArgs : EventArgs
{
    public DataAccessFailureEventArgs(string source, string message, Exception? exception = null)
    {
        Source = source;
        Message = message;
        Exception = exception;
    }

    public string Source { get; }

    public string Message { get; }

    public Exception? Exception { get; }
}

public sealed class DataAccessFailureNotifier : IDataAccessFailureNotifier
{
    public event EventHandler<DataAccessFailureEventArgs>? FailureOccurred;

    public DataAccessFailureEventArgs? LastFailure { get; private set; }

    public void Report(string source, string message, Exception? exception = null)
    {
        LastFailure = new DataAccessFailureEventArgs(source, message, exception);
        FailureOccurred?.Invoke(this, LastFailure);
    }
}
