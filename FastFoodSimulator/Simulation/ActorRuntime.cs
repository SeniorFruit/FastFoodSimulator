namespace FastFoodSimulator.Simulation;

public enum ActorRole
{
    OrderTaker,
    Cook,
    Server
}

public sealed record ActorSnapshot(
    ActorRole Role,
    int Index,
    bool IsBusy,
    int? OrderNumber,
    int? CustomerNumber,
    double Progress);

// Состояние сотрудника, из которого UI делает снимок.
internal sealed class ActorRuntime
{
    private readonly object _sync = new();
    private bool _isBusy;
    private int? _orderNumber;
    private int? _customerNumber;
    private TimeSpan _startedAt;
    private TimeSpan _duration;

    public ActorRuntime(ActorRole role, int index)
    {
        Role = role;
        Index = index;
    }

    public ActorRole Role { get; }

    public int Index { get; }

    public void Begin(int orderNumber, int customerNumber, TimeSpan startedAt, TimeSpan duration)
    {
        lock (_sync)
        {
            _isBusy = true;
            _orderNumber = orderNumber;
            _customerNumber = customerNumber;
            _startedAt = startedAt;
            _duration = duration;
        }
    }

    public void End()
    {
        lock (_sync)
        {
            _isBusy = false;
            _orderNumber = null;
            _customerNumber = null;
            _duration = TimeSpan.Zero;
        }
    }

    public ActorSnapshot Snapshot(TimeSpan now)
    {
        lock (_sync)
        {
            var progress = 0d;
            if (_isBusy && _duration > TimeSpan.Zero)
            {
                progress = Math.Clamp((now - _startedAt) / _duration, 0d, 1d);
            }

            return new ActorSnapshot(Role, Index, _isBusy, _orderNumber, _customerNumber, progress);
        }
    }
}
