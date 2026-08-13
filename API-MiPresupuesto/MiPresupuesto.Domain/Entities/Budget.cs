namespace MiPresupuesto.Domain.Entities;

public sealed class Budget : BaseEntity
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
