namespace Microsoft.UI.Xaml;

// Native scheduling is excluded from headless view-model tests. Financial and
// navigation decisions still execute the production code linked by the project.
public sealed class DispatcherTimer
{
    public TimeSpan Interval { get; set; }
    public event EventHandler<object?>? Tick;
    public void Start() { }
    public void Stop() { }
    internal void RaiseTick() => Tick?.Invoke(this, null);
}
