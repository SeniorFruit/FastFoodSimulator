namespace FastFoodSimulator.Models;

public enum OrderStatus
{
    Taking,
    WaitingForCook,
    Preparing,
    WaitingForServer,
    Announced,
    PickedUp
}

// Два Promise: сервер ждёт готовности заказа, клиент — вызова.
public sealed class OrderTicket
{
    private readonly TaskCompletionSource<OrderTicket> _readyForService =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly TaskCompletionSource<OrderTicket> _readyForPickup =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private int _status = (int)OrderStatus.Taking;

    public OrderTicket(int number, Customer customer)
    {
        Number = number;
        Customer = customer;
    }

    public int Number { get; }

    public Customer Customer { get; }

    public OrderStatus Status => (OrderStatus)Volatile.Read(ref _status);

    // Завершает повар, когда заказ готов.
    public Task<OrderTicket> ReadyForService => _readyForService.Task;

    // Завершает сервер после вызова клиента.
    public Task<OrderTicket> ReadyForPickup => _readyForPickup.Task;

    internal void SetStatus(OrderStatus status) => Volatile.Write(ref _status, (int)status);

    internal void MarkReadyForService() => _readyForService.TrySetResult(this);

    internal void MarkReadyForPickup() => _readyForPickup.TrySetResult(this);

    public override string ToString() => $"Order #{Number}";
}
