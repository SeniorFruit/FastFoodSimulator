namespace FastFoodSimulator.Simulation;


public sealed record SimulationOptions(
    TimeSpan CustomerArrivalInterval,
    TimeSpan CookPreparationInterval,
    int OrderTakerCount,
    int CookCount)
{
    public const double MinIntervalSeconds = 0.2;
    public const double MaxIntervalSeconds = 60.0;
    public const int MinWorkerCount = 1;
    public const int MaxWorkerCount = 100;

    // Время на оформление одного заказа.
    public TimeSpan OrderTakingInterval { get; init; } = TimeSpan.FromMilliseconds(4000);

    public TimeSpan ServingInterval { get; init; } = TimeSpan.FromMilliseconds(450);

    public int ServerCount { get; init; } = 1;

    public void Validate()
    {
        ValidateInterval(CustomerArrivalInterval, nameof(CustomerArrivalInterval));
        ValidateInterval(CookPreparationInterval, nameof(CookPreparationInterval));
        ValidateCount(OrderTakerCount, nameof(OrderTakerCount));
        ValidateCount(CookCount, nameof(CookCount));
        ValidateCount(ServerCount, nameof(ServerCount));

        if (OrderTakingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(OrderTakingInterval));
        }

        if (ServingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ServingInterval));
        }
    }

    private static void ValidateInterval(TimeSpan interval, string name)
    {
        if (interval < TimeSpan.FromSeconds(MinIntervalSeconds) ||
            interval > TimeSpan.FromSeconds(MaxIntervalSeconds))
        {
            throw new ArgumentOutOfRangeException(
                name,
                $"Interval must be between {MinIntervalSeconds} and {MaxIntervalSeconds} seconds.");
        }
    }

    private static void ValidateCount(int count, string name)
    {
        if (count < MinWorkerCount || count > MaxWorkerCount)
        {
            throw new ArgumentOutOfRangeException(
                name,
                $"Worker count must be between {MinWorkerCount} and {MaxWorkerCount}.");
        }
    }
}
