// Orchestrated saga: Place order -> Reserve stock -> Charge payment -> Create shipment.
// No distributed transaction: each step commits locally; a failure runs compensations in reverse.

Run("Scenario 1: happy path", stock: 5, balance: 1000, quantity: 3, unitPrice: 100);
Run("Scenario 2: payment fails", stock: 5, balance: 1000, quantity: 4, unitPrice: 300);
Run("Scenario 3: out of stock", stock: 2, balance: 1000, quantity: 3, unitPrice: 100);

static void Run(string title, int stock, decimal balance, int quantity, decimal unitPrice)
{
    Console.WriteLine($"{title}  (stock={stock}, balance={balance}, order={quantity} x {unitPrice} = {quantity * unitPrice})");
    var inventory = new InventoryService(stock);
    var payments = new PaymentService(balance);
    var shipping = new ShippingService();
    var orders = new OrderService();

    var saga = new PlaceOrderSaga(orders, inventory, payments, shipping);
    var state = saga.Execute(new OrderRequest("ORD-1", "KB", quantity, unitPrice));

    Console.WriteLine($"  => saga={state}, order={orders.Status}, stock left={inventory.Available}, balance={payments.Balance}, shipments={shipping.Count}");
    Console.WriteLine();
}

sealed record OrderRequest(string OrderId, string Sku, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

enum SagaState { Started, StockReserved, PaymentCharged, Completed, Compensated }

sealed class StepFailedException(string message) : Exception(message);

// ===== Each service = a separate bounded context with its own local transaction =====
sealed class OrderService
{
    public string Status { get; private set; } = "None";
    public void CreatePending(string id) { Status = "Pending"; Log($"Sales     : order {id} Pending"); }
    public void Confirm(string id) { Status = "Confirmed"; Log($"Sales     : order {id} Confirmed"); }
    public void Cancel(string id, string reason) { Status = "Cancelled"; Log($"Sales     : order {id} Cancelled ({reason})  [compensation]"); }
    private static void Log(string text) => Console.WriteLine($"  {text}");
}

sealed class InventoryService(int available)
{
    public int Available { get; private set; } = available;

    public void Reserve(string sku, int qty)
    {
        if (qty > Available)
            throw new StepFailedException($"only {Available} {sku} in stock");
        Available -= qty;
        Console.WriteLine($"  Inventory : reserved {qty} {sku}, left {Available}");
    }

    public void Release(string sku, int qty)
    {
        Available += qty;
        Console.WriteLine($"  Inventory : released {qty} {sku}, left {Available}  [compensation]");
    }
}

sealed class PaymentService(decimal balance)
{
    public decimal Balance { get; private set; } = balance;

    public void Charge(decimal amount)
    {
        if (amount > Balance)
            throw new StepFailedException($"insufficient funds: {amount} > {Balance}");
        Balance -= amount;
        Console.WriteLine($"  Billing   : charged {amount}, balance {Balance}");
    }
}

sealed class ShippingService
{
    public int Count { get; private set; }

    public void CreateShipment(string orderId)
    {
        Count++;
        Console.WriteLine($"  Shipping  : shipment SHP-{orderId} created");
    }
}

// ===== The orchestrator: knows the steps and their compensations =====
sealed class PlaceOrderSaga(OrderService orders, InventoryService inventory, PaymentService payments, ShippingService shipping)
{
    public SagaState State { get; private set; } = SagaState.Started;

    public SagaState Execute(OrderRequest r)
    {
        orders.CreatePending(r.OrderId);
        try
        {
            inventory.Reserve(r.Sku, r.Quantity);
            State = SagaState.StockReserved;

            payments.Charge(r.Total);
            State = SagaState.PaymentCharged;

            shipping.CreateShipment(r.OrderId);
            orders.Confirm(r.OrderId);
            State = SagaState.Completed;
        }
        catch (StepFailedException ex)
        {
            Console.WriteLine($"  !! step failed: {ex.Message}");
            Compensate(r, ex.Message);
        }
        return State;
    }

    // Undo completed steps in reverse order
    private void Compensate(OrderRequest r, string reason)
    {
        if (State >= SagaState.StockReserved)
            inventory.Release(r.Sku, r.Quantity);
        orders.Cancel(r.OrderId, reason);
        State = SagaState.Compensated;
    }
}
