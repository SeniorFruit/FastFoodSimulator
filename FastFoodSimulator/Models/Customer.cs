namespace FastFoodSimulator.Models;

// Этапы клиента: очередь, оформление, ожидание и зал.
public enum CustomerState
{
    InOrderLine,
    PlacingOrder,
    WaitingForOrder,
    Seated
}

public sealed class Customer
{
    public Customer(int number)
    {
        Number = number;
    }

    public int Number { get; }

    public CustomerState State { get; internal set; } = CustomerState.InOrderLine;

    public OrderTicket? Ticket { get; internal set; }

    public override string ToString() => $"Customer #{Number}";
}
