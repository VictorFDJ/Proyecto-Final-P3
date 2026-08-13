namespace MiPresupuesto.Domain.Entities;

public sealed class User : BaseEntity
{
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }

    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<PaymentMethod> PaymentMethods { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
}
