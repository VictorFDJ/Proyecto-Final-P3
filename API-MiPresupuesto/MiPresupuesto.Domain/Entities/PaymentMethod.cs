namespace MiPresupuesto.Domain.Entities;

public sealed class PaymentMethod : BaseEntity
{
    public required string Name { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Expense> Expenses { get; set; } = [];
}
