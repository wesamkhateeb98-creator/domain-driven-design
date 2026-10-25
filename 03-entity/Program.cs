Console.WriteLine("✅ Entity: equal by identity, attributes may change");
var ali1 = Customer.Create(new CustomerId(1), "Ali", "ali@shop.com");
var ali2 = Customer.Create(new CustomerId(2), "Ali", "ali@shop.com");
Show("ali1", ali1);
Show("ali2", ali2);
Show("ali1 == ali2 (same data)", ali1 == ali2);

ali1.Rename("Ali Hassan");
ali1.ChangeEmail("ali.hassan@shop.com");
Show("ali1 after rename + email", ali1);

var loadedAgain = Customer.Create(new CustomerId(1), "Ali Hassan", "ali.hassan@shop.com");
Show("ali1 == loadedAgain (Id 1)", ali1 == loadedAgain);

var customers = new HashSet<Customer> { ali1, loadedAgain, ali2 };
Show("HashSet {ali1, loadedAgain, ali2}", $"Count = {customers.Count}");

Try("Rename(\"\")", () => ali1.Rename(""));

Console.WriteLine();
Console.WriteLine("✅ Typed ids stop mixing identities");
var orderId = new OrderId(1);
Show("CustomerId(1) == OrderId(1)?", "does not compile: different types");
Show("orderId", orderId);

static void Show(string label, object? value) => Console.WriteLine($"  {label,-34}: {value}");

static void Try(string label, Action action)
{
    try { action(); Show(label, "accepted"); }
    catch (ArgumentException ex) { Show(label, $"rejected -> {ex.Message}"); }
}

readonly record struct CustomerId(int Value);
readonly record struct OrderId(int Value);

abstract class EntityBase<TId> where TId : notnull
{
    protected EntityBase(TId id) => Id = id;

    public TId Id { get; }

    public override bool Equals(object? obj) =>
        obj is EntityBase<TId> other && other.GetType() == GetType() && other.Id.Equals(Id);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(EntityBase<TId>? left, EntityBase<TId>? right) => Equals(left, right);
    public static bool operator !=(EntityBase<TId>? left, EntityBase<TId>? right) => !Equals(left, right);
}

sealed class Customer : EntityBase<CustomerId>
{
    private Customer(CustomerId id, string name, string email) : base(id)
    {
        Name = name;
        Email = email;
    }

    public string Name { get; private set; }
    public string Email { get; private set; }

    public static Customer Create(CustomerId id, string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name is required");
        return new Customer(id, name, email);
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("name is required");
        Name = newName;
    }

    public void ChangeEmail(string newEmail) => Email = newEmail;

    public override string ToString() => $"Id={Id.Value}, {Name}, {Email}";
}
