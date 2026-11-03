namespace FastFoodSimulator.ViewModels;

public sealed record CustomerChipViewModel(int Number)
{
    public string Tooltip => $"Клиент №{Number}";
}

public sealed record OrderChipViewModel(int Number)
{
    public string Label => $"#{Number}";
    public string Tooltip => $"Заказ №{Number} в очереди на кухню";
}

public sealed record OrderAnnouncementViewModel(int OrderNumber, int CustomerNumber)
{
    public string OrderLabel => $"#{OrderNumber}";
    public string CustomerLabel => $"клиент {CustomerNumber}";
    public string Tooltip => $"Заказ №{OrderNumber} выдан клиенту №{CustomerNumber}";
}
