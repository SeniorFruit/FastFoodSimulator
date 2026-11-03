using FastFoodSimulator.Models;

namespace FastFoodSimulator.Simulation;

// Снимок состояния для UI: после создания уже не меняется.
public sealed record SimulationSnapshot(
    bool IsRunning,
    TimeSpan Elapsed,
    int ArrivedCustomers,
    int CreatedOrders,
    int CompletedOrders,
    int OrderLineCount,
    int KitchenWaitingCount,
    int ServingLineCount,
    int? LastCompletedOrderNumber,
    IReadOnlyList<int> OrderLineCustomerNumbers,
    IReadOnlyList<ActorSnapshot> OrderTakers,
    IReadOnlyList<ActorSnapshot> Cooks,
    IReadOnlyList<ActorSnapshot> Servers,
    IReadOnlyList<int> KitchenQueueOrderNumbers,
    IReadOnlyList<int> ServingLineCustomerNumbers,
    IReadOnlyList<OrderAnnouncement> RecentAnnouncements,
    IReadOnlyList<int> DiningRoomCustomerNumbers)
{
    public static SimulationSnapshot Empty { get; } = new(
        IsRunning: false,
        Elapsed: TimeSpan.Zero,
        ArrivedCustomers: 0,
        CreatedOrders: 0,
        CompletedOrders: 0,
        OrderLineCount: 0,
        KitchenWaitingCount: 0,
        ServingLineCount: 0,
        LastCompletedOrderNumber: null,
        OrderLineCustomerNumbers: Array.Empty<int>(),
        OrderTakers: Array.Empty<ActorSnapshot>(),
        Cooks: Array.Empty<ActorSnapshot>(),
        Servers: Array.Empty<ActorSnapshot>(),
        KitchenQueueOrderNumbers: Array.Empty<int>(),
        ServingLineCustomerNumbers: Array.Empty<int>(),
        RecentAnnouncements: Array.Empty<OrderAnnouncement>(),
        DiningRoomCustomerNumbers: Array.Empty<int>());
}
