using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FastFoodSimulator.Simulation;

namespace FastFoodSimulator.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private static readonly Brush RunningBrush = CreateBrush("#4ADE80");
    private static readonly Brush IdleBrush = CreateBrush("#66728B");

    private readonly DispatcherTimer _uiTimer;
    private readonly RelayCommand _startCommand;
    private readonly AsyncRelayCommand _stopCommand;

    private SimulationEngine? _engine;
    private bool _isRunning;
    private bool _isStopping;
    private bool _hasStarted;

    private string _arrivalIntervalText = "2,5";
    private string _preparationIntervalText = "3,0";
    private string _orderTakerCountText = "2";
    private string _cookCountText = "2";
    private string _validationMessage = string.Empty;
    private bool _hasValidationError;

    private string _elapsedText = "00:00.0";
    private int _arrivedCustomers;
    private int _createdOrders;
    private int _completedOrders;
    private int _orderLineCount;
    private int _kitchenWaitingCount;
    private int _kitchenBusyCount;
    private int _kitchenTotalCount;
    private int _servingLineCount;
    private int _takerBusyCount;
    private string _takerCountLabel = "0 в команде";
    private string _takerBusyLabel = "Все свободны";
    private string _cookCountLabel = "0 в команде";
    private string _cookBusyLabel = "Все свободны";
    private string _orderLineStatusText = "Очередь пуста";
    private string _kitchenWaitingLabel = "Очередь пуста";
    private string _servingLineStatusText = "Линия пуста";
    private string _diningCountLabel = "Пока никого";
    private string _lastOrderDisplay = "—";
    private string _lastOrderCaption = "нажмите «Старт»";

    public MainViewModel()
    {
        _uiTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _uiTimer.Tick += OnUiTick;

        _startCommand = new RelayCommand(Start, () => !IsRunning && !IsStopping);
        _stopCommand = new AsyncRelayCommand(StopAsync, () => IsRunning && !IsStopping);
    }

    public ObservableCollection<CustomerChipViewModel> OrderLine { get; } = new();
    public ObservableCollection<ActorViewModel> OrderTakers { get; } = new();
    public ObservableCollection<OrderChipViewModel> KitchenQueue { get; } = new();
    public ObservableCollection<ActorViewModel> Cooks { get; } = new();
    public ObservableCollection<CustomerChipViewModel> ServingLine { get; } = new();
    public ObservableCollection<ActorViewModel> Servers { get; } = new();
    public ObservableCollection<OrderAnnouncementViewModel> RecentAnnouncements { get; } = new();
    public ObservableCollection<CustomerChipViewModel> DiningRoom { get; } = new();

    public ICommand StartCommand => _startCommand;

    public ICommand StopCommand => _stopCommand;

    public string ArrivalIntervalText
    {
        get => _arrivalIntervalText;
        set
        {
            if (SetProperty(ref _arrivalIntervalText, value))
            {
                ClearValidation();
            }
        }
    }

    public string PreparationIntervalText
    {
        get => _preparationIntervalText;
        set
        {
            if (SetProperty(ref _preparationIntervalText, value))
            {
                ClearValidation();
            }
        }
    }

    public string OrderTakerCountText
    {
        get => _orderTakerCountText;
        set
        {
            if (SetProperty(ref _orderTakerCountText, value))
            {
                ClearValidation();
            }
        }
    }

    public string CookCountText
    {
        get => _cookCountText;
        set
        {
            if (SetProperty(ref _cookCountText, value))
            {
                ClearValidation();
            }
        }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public bool HasValidationError
    {
        get => _hasValidationError;
        private set => SetProperty(ref _hasValidationError, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsInputEnabled));
            OnPropertyChanged(nameof(StatusBadgeText));
            OnPropertyChanged(nameof(StatusBrush));
            RaiseCommandStates();
        }
    }

    public bool IsStopping
    {
        get => _isStopping;
        private set
        {
            if (!SetProperty(ref _isStopping, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsInputEnabled));
            RaiseCommandStates();
        }
    }

    public bool IsInputEnabled => !IsRunning && !IsStopping;

    public string StatusBadgeText => IsRunning
        ? "В РАБОТЕ"
        : _hasStarted
            ? "ОСТАНОВЛЕНО"
            : "ОЖИДАНИЕ";

    public Brush StatusBrush => IsRunning ? RunningBrush : IdleBrush;

    public string ElapsedText
    {
        get => _elapsedText;
        private set => SetProperty(ref _elapsedText, value);
    }

    public int ArrivedCustomers
    {
        get => _arrivedCustomers;
        private set => SetProperty(ref _arrivedCustomers, value);
    }

    public int CreatedOrders
    {
        get => _createdOrders;
        private set => SetProperty(ref _createdOrders, value);
    }

    public int CompletedOrders
    {
        get => _completedOrders;
        private set => SetProperty(ref _completedOrders, value);
    }

    public int OrderLineCount
    {
        get => _orderLineCount;
        private set => SetProperty(ref _orderLineCount, value);
    }

    public int KitchenWaitingCount
    {
        get => _kitchenWaitingCount;
        private set => SetProperty(ref _kitchenWaitingCount, value);
    }

    public int KitchenBusyCount
    {
        get => _kitchenBusyCount;
        private set => SetProperty(ref _kitchenBusyCount, value);
    }

    public int KitchenTotalCount
    {
        get => _kitchenTotalCount;
        private set => SetProperty(ref _kitchenTotalCount, value);
    }

    public int ServingLineCount
    {
        get => _servingLineCount;
        private set => SetProperty(ref _servingLineCount, value);
    }

    public int TakerBusyCount
    {
        get => _takerBusyCount;
        private set => SetProperty(ref _takerBusyCount, value);
    }

    public string TakerCountLabel
    {
        get => _takerCountLabel;
        private set => SetProperty(ref _takerCountLabel, value);
    }

    public string TakerBusyLabel
    {
        get => _takerBusyLabel;
        private set => SetProperty(ref _takerBusyLabel, value);
    }

    public string CookCountLabel
    {
        get => _cookCountLabel;
        private set => SetProperty(ref _cookCountLabel, value);
    }

    public string CookBusyLabel
    {
        get => _cookBusyLabel;
        private set => SetProperty(ref _cookBusyLabel, value);
    }

    public string OrderLineStatusText
    {
        get => _orderLineStatusText;
        private set => SetProperty(ref _orderLineStatusText, value);
    }

    public string KitchenWaitingLabel
    {
        get => _kitchenWaitingLabel;
        private set => SetProperty(ref _kitchenWaitingLabel, value);
    }

    public string ServingLineStatusText
    {
        get => _servingLineStatusText;
        private set => SetProperty(ref _servingLineStatusText, value);
    }

    public string DiningCountLabel
    {
        get => _diningCountLabel;
        private set => SetProperty(ref _diningCountLabel, value);
    }

    public string LastOrderDisplay
    {
        get => _lastOrderDisplay;
        private set => SetProperty(ref _lastOrderDisplay, value);
    }

    public string LastOrderCaption
    {
        get => _lastOrderCaption;
        private set => SetProperty(ref _lastOrderCaption, value);
    }

    public bool HasOrderLineCustomers => OrderLine.Count > 0;

    public bool HasKitchenOrders => KitchenQueue.Count > 0;

    public bool HasServingLineCustomers => ServingLine.Count > 0;

    public bool HasRecentAnnouncements => RecentAnnouncements.Count > 0;

    public bool HasDiningRoomCustomers => DiningRoom.Count > 0;

    public bool HasOrderTakers => OrderTakers.Count > 0;

    public bool HasCooks => Cooks.Count > 0;

    public bool HasServers => Servers.Count > 0;

    private void Start()
    {
        if (IsRunning || IsStopping)
        {
            return;
        }

        if (!TryBuildOptions(out var options, out var error))
        {
            ValidationMessage = error;
            HasValidationError = true;
            return;
        }

        ClearValidation();
        ClearCollections();

        _engine = new SimulationEngine(options);
        _hasStarted = true;
        ApplySnapshot(_engine.GetSnapshot());

        _engine.Start();
        IsRunning = true;
        _uiTimer.Start();
        ApplySnapshot(_engine.GetSnapshot());
    }

    private async Task StopAsync()
    {
        if (_engine is null || IsStopping)
        {
            return;
        }

        IsStopping = true;
        try
        {
            await _engine.StopAsync();
            IsRunning = false;
            ApplySnapshot(_engine.GetSnapshot());
        }
        finally
        {
            _uiTimer.Stop();
            IsStopping = false;
        }
    }

    private void OnUiTick(object? sender, EventArgs e)
    {
        if (_engine is null)
        {
            return;
        }

        ApplySnapshot(_engine.GetSnapshot());
    }

    private void ApplySnapshot(SimulationSnapshot snapshot)
    {
        ElapsedText = FormatElapsed(snapshot.Elapsed);
        ArrivedCustomers = snapshot.ArrivedCustomers;
        CreatedOrders = snapshot.CreatedOrders;
        CompletedOrders = snapshot.CompletedOrders;
        OrderLineCount = snapshot.OrderLineCount;
        KitchenWaitingCount = snapshot.KitchenWaitingCount;
        ServingLineCount = snapshot.ServingLineCount;

        var busyTakers = snapshot.OrderTakers.Count(actor => actor.IsBusy);
        var busyCooks = snapshot.Cooks.Count(actor => actor.IsBusy);
        TakerBusyCount = busyTakers;
        KitchenBusyCount = busyCooks;
        KitchenTotalCount = snapshot.KitchenWaitingCount + busyCooks;

        TakerCountLabel = $"{snapshot.OrderTakers.Count} в команде";
        TakerBusyLabel = BusyText(busyTakers, "работает", "работают");
        CookCountLabel = $"{snapshot.Cooks.Count} в команде";
        CookBusyLabel = BusyText(busyCooks, "готовит", "готовят");

        OrderLineStatusText = snapshot.OrderLineCount == 0
            ? "Очередь пуста"
            : $"{snapshot.OrderLineCount} ждут оформления";
        KitchenWaitingLabel = snapshot.KitchenWaitingCount == 0
            ? "Очередь пуста"
            : $"{snapshot.KitchenWaitingCount} в очереди";
        ServingLineStatusText = snapshot.ServingLineCount == 0
            ? "Линия пуста"
            : $"{snapshot.ServingLineCount} ждут выдачи";
        DiningCountLabel = snapshot.DiningRoomCustomerNumbers.Count == 0
            ? "Пока никого"
            : $"Недавно обслужены: {snapshot.DiningRoomCustomerNumbers.Count}";

        if (snapshot.LastCompletedOrderNumber is { } orderNumber)
        {
            LastOrderDisplay = $"#{orderNumber}";
            LastOrderCaption = "последний вызванный заказ";
        }
        else
        {
            LastOrderDisplay = "—";
            LastOrderCaption = snapshot.IsRunning
                ? "первый заказ уже готовится"
                : "нажмите «Старт»";
        }

        SyncByKey(
            OrderLine,
            snapshot.OrderLineCustomerNumbers,
            key => key,
            item => item.Number,
            key => new CustomerChipViewModel(key));

        SyncActors(OrderTakers, snapshot.OrderTakers);

        SyncByKey(
            KitchenQueue,
            snapshot.KitchenQueueOrderNumbers,
            key => key,
            item => item.Number,
            key => new OrderChipViewModel(key));

        SyncActors(Cooks, snapshot.Cooks);

        SyncByKey(
            ServingLine,
            snapshot.ServingLineCustomerNumbers,
            key => key,
            item => item.Number,
            key => new CustomerChipViewModel(key));

        SyncActors(Servers, snapshot.Servers);

        SyncByKey(
            RecentAnnouncements,
            snapshot.RecentAnnouncements,
            announcement => announcement.OrderNumber,
            item => item.OrderNumber,
            announcement => new OrderAnnouncementViewModel(
                announcement.OrderNumber,
                announcement.CustomerNumber));

        SyncByKey(
            DiningRoom,
            snapshot.DiningRoomCustomerNumbers,
            key => key,
            item => item.Number,
            key => new CustomerChipViewModel(key));

        OnPropertyChanged(nameof(HasOrderLineCustomers));
        OnPropertyChanged(nameof(HasKitchenOrders));
        OnPropertyChanged(nameof(HasServingLineCustomers));
        OnPropertyChanged(nameof(HasRecentAnnouncements));
        OnPropertyChanged(nameof(HasDiningRoomCustomers));
        OnPropertyChanged(nameof(HasOrderTakers));
        OnPropertyChanged(nameof(HasCooks));
        OnPropertyChanged(nameof(HasServers));
    }

    private void ClearCollections()
    {
        OrderLine.Clear();
        OrderTakers.Clear();
        KitchenQueue.Clear();
        Cooks.Clear();
        ServingLine.Clear();
        Servers.Clear();
        RecentAnnouncements.Clear();
        DiningRoom.Clear();

        ArrivedCustomers = 0;
        CreatedOrders = 0;
        CompletedOrders = 0;
        OrderLineCount = 0;
        KitchenWaitingCount = 0;
        KitchenBusyCount = 0;
        KitchenTotalCount = 0;
        ServingLineCount = 0;
        TakerBusyCount = 0;
        TakerCountLabel = "0 в команде";
        TakerBusyLabel = "Все свободны";
        CookCountLabel = "0 в команде";
        CookBusyLabel = "Все свободны";
        OrderLineStatusText = "Очередь пуста";
        KitchenWaitingLabel = "Очередь пуста";
        ServingLineStatusText = "Линия пуста";
        DiningCountLabel = "Пока никого";
        LastOrderDisplay = "—";
        LastOrderCaption = "нажмите «Старт»";
        ElapsedText = "00:00.0";

        OnPropertyChanged(nameof(HasOrderLineCustomers));
        OnPropertyChanged(nameof(HasKitchenOrders));
        OnPropertyChanged(nameof(HasServingLineCustomers));
        OnPropertyChanged(nameof(HasRecentAnnouncements));
        OnPropertyChanged(nameof(HasDiningRoomCustomers));
        OnPropertyChanged(nameof(HasOrderTakers));
        OnPropertyChanged(nameof(HasCooks));
        OnPropertyChanged(nameof(HasServers));
    }

    private bool TryBuildOptions(out SimulationOptions options, out string error)
    {
        options = null!;
        error = string.Empty;

        if (!TryParseDouble(ArrivalIntervalText, out var arrival))
        {
            error = "Интервал прихода клиентов должен быть числом. Например: 2,5.";
            return false;
        }

        if (arrival < SimulationOptions.MinIntervalSeconds ||
            arrival > SimulationOptions.MaxIntervalSeconds)
        {
            error = $"Интервал прихода клиентов должен быть от {FormatSeconds(SimulationOptions.MinIntervalSeconds)} " +
                    $"до {FormatSeconds(SimulationOptions.MaxIntervalSeconds)} секунд.";
            return false;
        }

        if (!TryParseDouble(PreparationIntervalText, out var preparation))
        {
            error = "Интервал приготовления заказа должен быть числом. Например: 3,0.";
            return false;
        }

        if (preparation < SimulationOptions.MinIntervalSeconds ||
            preparation > SimulationOptions.MaxIntervalSeconds)
        {
            error = $"Интервал приготовления заказа должен быть от {FormatSeconds(SimulationOptions.MinIntervalSeconds)} " +
                    $"до {FormatSeconds(SimulationOptions.MaxIntervalSeconds)} секунд.";
            return false;
        }

        if (!TryParseInt(OrderTakerCountText, out var takers) ||
            takers < SimulationOptions.MinWorkerCount ||
            takers > SimulationOptions.MaxWorkerCount)
        {
            error = $"Количество ордер-тейкеров должно быть целым числом от {SimulationOptions.MinWorkerCount} " +
                    $"до {SimulationOptions.MaxWorkerCount}.";
            return false;
        }

        if (!TryParseInt(CookCountText, out var cooks) ||
            cooks < SimulationOptions.MinWorkerCount ||
            cooks > SimulationOptions.MaxWorkerCount)
        {
            error = $"Количество поваров должно быть целым числом от {SimulationOptions.MinWorkerCount} " +
                    $"до {SimulationOptions.MaxWorkerCount}.";
            return false;
        }

        options = new SimulationOptions(
            TimeSpan.FromSeconds(arrival),
            TimeSpan.FromSeconds(preparation),
            takers,
            cooks);
        return true;
    }

    private void ClearValidation()
    {
        if (!HasValidationError && string.IsNullOrEmpty(ValidationMessage))
        {
            return;
        }

        HasValidationError = false;
        ValidationMessage = string.Empty;
    }

    private void RaiseCommandStates()
    {
        _startCommand.RaiseCanExecuteChanged();
        _stopCommand.RaiseCanExecuteChanged();
    }

    private static void SyncByKey<TItem, TSource>(
        ObservableCollection<TItem> target,
        IReadOnlyList<TSource> source,
        Func<TSource, int> sourceKey,
        Func<TItem, int> itemKey,
        Func<TSource, TItem> factory)
        where TItem : class
    {
        var desiredKeys = source.Select(sourceKey).ToList();
        var desiredSet = desiredKeys.ToHashSet();

        for (var index = target.Count - 1; index >= 0; index--)
        {
            if (!desiredSet.Contains(itemKey(target[index])))
            {
                target.RemoveAt(index);
            }
        }

        for (var index = 0; index < desiredKeys.Count; index++)
        {
            var key = desiredKeys[index];
            if (index < target.Count && itemKey(target[index]) == key)
            {
                continue;
            }

            var existing = target.FirstOrDefault(item => itemKey(item) == key);
            if (existing is not null)
            {
                target.Move(target.IndexOf(existing), index);
            }
            else
            {
                target.Insert(index, factory(source[index]));
            }
        }
    }

    private static void SyncActors(ObservableCollection<ActorViewModel> target, IReadOnlyList<ActorSnapshot> source)
    {
        while (target.Count > source.Count)
        {
            target.RemoveAt(target.Count - 1);
        }

        while (target.Count < source.Count)
        {
            target.Add(new ActorViewModel());
        }

        for (var index = 0; index < source.Count; index++)
        {
            target[index].Update(source[index]);
        }
    }

    private static string BusyText(int count, string singular, string plural)
    {
        if (count == 0)
        {
            return "Все свободны";
        }

        return $"{count} {(count == 1 ? singular : plural)}";
    }

    private static string FormatElapsed(TimeSpan elapsed)
    {
        var minutes = (int)elapsed.TotalMinutes;
        return $"{minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 100}";
    }

    private static bool TryParseDouble(string? text, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
               double.IsFinite(value);
    }

    private static bool TryParseInt(string? text, out int value)
    {
        value = 0;
        return !string.IsNullOrWhiteSpace(text) &&
               int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatSeconds(double value) =>
        value.ToString("0.#", CultureInfo.GetCultureInfo("ru-RU"));

    private static Brush CreateBrush(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }

    public void Dispose()
    {
        _uiTimer.Stop();
        _uiTimer.Tick -= OnUiTick;

        try
        {
            _engine?.StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // При закрытии окна ошибки останова уже не важны.
        }
    }
}
