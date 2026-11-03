using System.Windows.Media;
using FastFoodSimulator.Simulation;

namespace FastFoodSimulator.ViewModels;

public sealed class ActorViewModel : ObservableObject
{
    private static readonly Brush TakerAccent = CreateBrush("#FFB547");
    private static readonly Brush TakerSoft = CreateBrush("#2B2316");
    private static readonly Brush CookAccent = CreateBrush("#C084FC");
    private static readonly Brush CookSoft = CreateBrush("#271E36");
    private static readonly Brush ServerAccent = CreateBrush("#2DD4BF");
    private static readonly Brush ServerSoft = CreateBrush("#163330");
    private static readonly Brush Green = CreateBrush("#4ADE80");
    private static readonly Brush Muted = CreateBrush("#66728B");

    private string _title = string.Empty;
    private string _badgeText = string.Empty;
    private string _statusText = string.Empty;
    private string _orderDisplay = "—";
    private double _progress;
    private Brush _accentBrush = Muted;
    private Brush _accentSoftBrush = Muted;
    private Brush _statusBrush = Muted;
    private Brush _activityBrush = Muted;
    private Brush _orderBrush = Muted;
    private bool _isBusy;
    private ActorRole? _role;
    private int _index;

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    public string BadgeText
    {
        get => _badgeText;
        private set => SetProperty(ref _badgeText, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string OrderDisplay
    {
        get => _orderDisplay;
        private set => SetProperty(ref _orderDisplay, value);
    }

    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    public Brush AccentBrush
    {
        get => _accentBrush;
        private set => SetProperty(ref _accentBrush, value);
    }

    public Brush AccentSoftBrush
    {
        get => _accentSoftBrush;
        private set => SetProperty(ref _accentSoftBrush, value);
    }

    public Brush StatusBrush
    {
        get => _statusBrush;
        private set => SetProperty(ref _statusBrush, value);
    }

    public Brush ActivityBrush
    {
        get => _activityBrush;
        private set => SetProperty(ref _activityBrush, value);
    }

    public Brush OrderBrush
    {
        get => _orderBrush;
        private set => SetProperty(ref _orderBrush, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public void Update(ActorSnapshot snapshot)
    {
        if (_role != snapshot.Role || _index != snapshot.Index)
        {
            _role = snapshot.Role;
            _index = snapshot.Index;

            (Title, BadgeText, AccentBrush, AccentSoftBrush) = snapshot.Role switch
            {
                ActorRole.OrderTaker => ($"Тейкер {snapshot.Index}", $"T{snapshot.Index}", TakerAccent, TakerSoft),
                ActorRole.Cook => ($"Повар {snapshot.Index}", $"C{snapshot.Index}", CookAccent, CookSoft),
                _ => ($"Сервер {snapshot.Index}", $"S{snapshot.Index}", ServerAccent, ServerSoft)
            };
        }

        IsBusy = snapshot.IsBusy;
        Progress = Math.Round(snapshot.Progress * 100d, 1);

        if (snapshot.IsBusy && snapshot.OrderNumber is { } orderNumber)
        {
            StatusText = snapshot.Role switch
            {
                ActorRole.OrderTaker => $"Принимает заказ #{orderNumber}",
                ActorRole.Cook => $"Готовит заказ #{orderNumber}",
                _ => $"Выдаёт заказ #{orderNumber}"
            };
            OrderDisplay = $"#{orderNumber}";
            OrderBrush = AccentBrush;
            StatusBrush = Green;
            ActivityBrush = Green;
        }
        else
        {
            StatusText = snapshot.Role switch
            {
                ActorRole.OrderTaker => "Ждёт клиента",
                ActorRole.Cook => "Ждёт заказ",
                _ => "Ждёт готовое блюдо"
            };
            OrderDisplay = "—";
            OrderBrush = Muted;
            StatusBrush = Muted;
            ActivityBrush = Muted;
        }
    }

    private static Brush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
