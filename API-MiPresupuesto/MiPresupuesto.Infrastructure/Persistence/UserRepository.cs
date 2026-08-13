using Microsoft.EntityFrameworkCore;
using MiPresupuesto.Application.Auth;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => dbContext.Users.SingleOrDefaultAsync(x => x.Email == email, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => dbContext.Users.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
