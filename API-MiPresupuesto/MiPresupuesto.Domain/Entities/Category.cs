namespace MiPresupuesto.Domain.Entities;

public sealed class Category : BaseEntity
{
    public required string Name { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
}
