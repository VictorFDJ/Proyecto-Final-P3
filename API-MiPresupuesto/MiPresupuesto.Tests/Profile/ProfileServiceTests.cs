using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Profile;
using MiPresupuesto.Domain.Entities;
using MiPresupuesto.Infrastructure.Security;

namespace MiPresupuesto.Tests.Profile;

public sealed class ProfileServiceTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public async Task UpdateNameAsync_OnlyUpdatesRequestedUser()
    {
        var ana = CreateUser("Ana", "ana@example.com");
        var luis = CreateUser("Luis", "luis@example.com");
        var repository = new FakeUserRepository([ana, luis]);
        var service = new ProfileService(repository, new FakeUnitOfWork(), _hasher);

        var response = await service.UpdateNameAsync(ana.Id, new UpdateNameRequest("Ana María"));

        Assert.Equal("Ana María", response.Name);
        Assert.Equal("Ana María", ana.Name);
        Assert.Equal("Luis", luis.Name);
    }

    [Fact]
    public async Task ChangePasswordAsync_WithCorrectCurrentPassword_ChangesHash()
    {
        var user = CreateUser("Ana", "ana@example.com");
        var oldHash = user.PasswordHash;
        var service = new ProfileService(
            new FakeUserRepository([user]),
            new FakeUnitOfWork(),
            _hasher);

        await service.ChangePasswordAsync(
            user.Id,
            new ChangePasswordRequest("Clave123", "Nueva456"));

        Assert.NotEqual(oldHash, user.PasswordHash);
        Assert.True(_hasher.Verify("Nueva456", user.PasswordHash));
        Assert.False(_hasher.Verify("Clave123", user.PasswordHash));
    }

    [Fact]
    public async Task ChangePasswordAsync_WithWrongCurrentPassword_ThrowsValidation()
    {
        var user = CreateUser("Ana", "ana@example.com");
        var service = new ProfileService(
            new FakeUserRepository([user]),
            new FakeUnitOfWork(),
            _hasher);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.ChangePasswordAsync(
                user.Id,
                new ChangePasswordRequest("Incorrecta", "Nueva456")));

        Assert.Contains("currentPassword", exception.Errors!.Keys);
    }

    private User CreateUser(string name, string email) => new()
    {
        Name = name,
        Email = email,
        PasswordHash = _hasher.Hash("Clave123")
    };

    private sealed class FakeUserRepository(IEnumerable<User> users) : IUserRepository
    {
        private readonly List<User> _users = [.. users];

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.Any(x => x.Email == email));

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.SingleOrDefault(x => x.Email == email));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_users.SingleOrDefault(x => x.Id == id));

        public void Add(User user) => _users.Add(user);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }
}
