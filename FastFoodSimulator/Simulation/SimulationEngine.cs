using System.Diagnostics;
using FastFoodSimulator.Models;

namespace FastFoodSimulator.Simulation;

// Запускает клиентов, тейкеров, поваров и сервер. UI только читает снимки.
public sealed class SimulationEngine
{
    private const int MaxAnnouncements = 8;
    private const int MaxDiningRoomSeats = 12;

    private readonly SimulationOptions _options;
    private readonly object _gate = new();

    private readonly AsyncQueue<Customer> _orderLine = new();
    private readonly AsyncQueue<OrderTicket> _kitchenQueue = new();
    private readonly AsyncQueue<OrderTicket> _serviceQueue = new();

    private readonly List<ActorRuntime> _orderTakers;
    private readonly List<ActorRuntime> _cooks;
    private readonly List<ActorRuntime> _servers;

    private readonly List<Customer> _servingLine = new();
    private readonly List<OrderAnnouncement> _announcements = new();
    private readonly List<int> _diningRoom = new();
    private readonly List<Task> _workerTasks = new();

    private readonly Stopwatch _clock = new();

    private CancellationTokenSource? _cancellation;
    private bool _isRunning;
    private bool _hasStarted;

    private int _nextCustomerNumber;
    private int _nextOrderNumber;
    private int _arrivedCustomers;
    private int _createdOrders;
    private int _completedOrders;
    private int? _lastCompletedOrderNumber;

    public SimulationEngine(SimulationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _options = options;

        _orderTakers = CreateActors(ActorRole.OrderTaker, options.OrderTakerCount);
        _cooks = CreateActors(ActorRole.Cook, options.CookCount);
        _servers = CreateActors(ActorRole.Server, options.ServerCount);
    }

    // Запускает всех сразу; первый клиент приходит в момент старта.
    public void Start()
    {
        lock (_gate)
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("Create a new engine for each simulation run.");
            }

            _hasStarted = true;
            _isRunning = true;
            _clock.Restart();
            _cancellation = new CancellationTokenSource();
            var token = _cancellation.Token;

            _workerTasks.Add(Task.Run(() => RunCustomerArrivalsAsync(token), token));

            foreach (var taker in _orderTakers)
            {
                _workerTasks.Add(Task.Run(() => RunOrderTakerAsync(taker, token), token));
            }

            foreach (var cook in _cooks)
            {
                _workerTasks.Add(Task.Run(() => RunCookAsync(cook, token), token));
            }

            foreach (var server in _servers)
            {
                _workerTasks.Add(Task.Run(() => RunServerAsync(server, token), token));
            }
        }
    }

    // Останавливает задачи и ждёт, пока они завершатся.
    public async Task StopAsync()
    {
        CancellationTokenSource? cancellation;
        Task[] workers;

        lock (_gate)
        {
            if (!_isRunning && _cancellation is null)
            {
                return;
            }

            _isRunning = false;
            cancellation = _cancellation;
            _cancellation = null;
            workers = _workerTasks.ToArray();
        }

        if (cancellation is not null)
        {
            cancellation.Cancel();
            try
            {
                await Task.WhenAll(workers).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Пользователь нажал «Стоп» — это не ошибка.
            }
            catch
            {
                // Одна упавшая задача не должна мешать остановке.
            }
            finally
            {
                cancellation.Dispose();
            }
        }

        _clock.Stop();
    }

    public SimulationSnapshot GetSnapshot()
    {
        var elapsed = _clock.Elapsed;
        var orderLine = _orderLine.Snapshot().Select(customer => customer.Number).ToArray();
        var kitchenQueue = _kitchenQueue.Snapshot().Select(ticket => ticket.Number).ToArray();
        var orderTakers = _orderTakers.Select(actor => actor.Snapshot(elapsed)).ToArray();
        var cooks = _cooks.Select(actor => actor.Snapshot(elapsed)).ToArray();
        var servers = _servers.Select(actor => actor.Snapshot(elapsed)).ToArray();

        lock (_gate)
        {
            var servingLine = _servingLine.Select(customer => customer.Number).ToArray();
            var announcements = _announcements
                .AsEnumerable()
                .Reverse()
                .Take(MaxAnnouncements)
                .ToArray();
            var diningRoom = _diningRoom
                .AsEnumerable()
                .Reverse()
                .Take(MaxDiningRoomSeats)
                .ToArray();

            return new SimulationSnapshot(
                IsRunning: _isRunning,
                Elapsed: elapsed,
                ArrivedCustomers: _arrivedCustomers,
                CreatedOrders: _createdOrders,
                CompletedOrders: _completedOrders,
                OrderLineCount: orderLine.Length,
                KitchenWaitingCount: kitchenQueue.Length,
                ServingLineCount: servingLine.Length,
                LastCompletedOrderNumber: _lastCompletedOrderNumber,
                OrderLineCustomerNumbers: orderLine,
                OrderTakers: orderTakers,
                Cooks: cooks,
                Servers: servers,
                KitchenQueueOrderNumbers: kitchenQueue,
                ServingLineCustomerNumbers: servingLine,
                RecentAnnouncements: announcements,
                DiningRoomCustomerNumbers: diningRoom);
        }
    }

    private async Task RunCustomerArrivalsAsync(CancellationToken cancellationToken)
    {
        try
        {
            RegisterCustomer();

            using var timer = new PeriodicTimer(_options.CustomerArrivalInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                RegisterCustomer();
            }
        }
        catch (OperationCanceledException)
        {
            // Штатная остановка.
        }
    }

    private void RegisterCustomer()
    {
        Customer customer;
        lock (_gate)
        {
            customer = new Customer(++_nextCustomerNumber);
            _arrivedCustomers++;
        }

        _orderLine.Enqueue(customer);
    }

    private async Task RunOrderTakerAsync(ActorRuntime actor, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var customer = await _orderLine.DequeueAsync(cancellationToken).ConfigureAwait(false);

                OrderTicket ticket;
                lock (_gate)
                {
                    ticket = new OrderTicket(++_nextOrderNumber, customer);
                    ticket.SetStatus(OrderStatus.Taking);
                    _createdOrders++;
                    customer.State = CustomerState.PlacingOrder;
                    customer.Ticket = ticket;
                }

                actor.Begin(
                    ticket.Number,
                    customer.Number,
                    _clock.Elapsed,
                    _options.OrderTakingInterval);

                try
                {
                    await Task.Delay(_options.OrderTakingInterval, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    actor.End();
                }

                ticket.SetStatus(OrderStatus.WaitingForCook);

                lock (_gate)
                {
                    customer.State = CustomerState.WaitingForOrder;
                    _servingLine.Add(customer);
                }

                _kitchenQueue.Enqueue(ticket);

                // Клиент ждёт вызова, пока стоит на линии выдачи.
                _ = WaitForOrderAsync(ticket, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            actor.End();
        }
    }

    private async Task RunCookAsync(ActorRuntime actor, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var ticket = await _kitchenQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                ticket.SetStatus(OrderStatus.Preparing);

                actor.Begin(
                    ticket.Number,
                    ticket.Customer.Number,
                    _clock.Elapsed,
                    _options.CookPreparationInterval);

                try
                {
                    await Task.Delay(_options.CookPreparationInterval, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    actor.End();
                }

                ticket.SetStatus(OrderStatus.WaitingForServer);
                ticket.MarkReadyForService();
                _serviceQueue.Enqueue(ticket);
            }
        }
        catch (OperationCanceledException)
        {
            actor.End();
        }
    }

    private async Task RunServerAsync(ActorRuntime actor, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                var ticket = await _serviceQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);

                // Ждём, пока повар закончит заказ.
                await ticket.ReadyForService.ConfigureAwait(false);

                actor.Begin(
                    ticket.Number,
                    ticket.Customer.Number,
                    _clock.Elapsed,
                    _options.ServingInterval);

                try
                {
                    await Task.Delay(_options.ServingInterval, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    actor.End();
                }

                ticket.SetStatus(OrderStatus.Announced);

                lock (_gate)
                {
                    ticket.Customer.State = CustomerState.Seated;
                    _servingLine.Remove(ticket.Customer);
                    _diningRoom.Add(ticket.Customer.Number);
                    if (_diningRoom.Count > MaxDiningRoomSeats)
                    {
                        _diningRoom.RemoveAt(0);
                    }

                    _announcements.Add(new OrderAnnouncement(
                        ticket.Number,
                        ticket.Customer.Number,
                        _clock.Elapsed));
                    if (_announcements.Count > MaxAnnouncements)
                    {
                        _announcements.RemoveAt(0);
                    }

                    _completedOrders++;
                    _lastCompletedOrderNumber = ticket.Number;
                }

                // Вызываем клиента и будим его по второму промису.
                ticket.SetStatus(OrderStatus.PickedUp);
                ticket.MarkReadyForPickup();
            }
        }
        catch (OperationCanceledException)
        {
            actor.End();
        }
    }

    private static async Task WaitForOrderAsync(OrderTicket ticket, CancellationToken cancellationToken)
    {
        try
        {
            await ticket.ReadyForPickup.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Остановили симуляцию до выдачи заказа.
        }
    }

    private static List<ActorRuntime> CreateActors(ActorRole role, int count)
    {
        var actors = new List<ActorRuntime>(count);
        for (var index = 1; index <= count; index++)
        {
            actors.Add(new ActorRuntime(role, index));
        }

        return actors;
    }
}
