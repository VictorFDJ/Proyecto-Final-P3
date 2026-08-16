using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Domain.Entities;
using MiPresupuesto.Infrastructure.Security;

namespace MiPresupuesto.Tests.Auth;

public sealed class AuthServiceTests
{
    private readonly FakeUserRepository _users = new();
    private readonly PasswordHasher _passwordHasher = new();

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserWithHashedPassword()
    {
        var service = CreateService();

        var response = await service.RegisterAsync(
            new RegisterRequest("  Ana Pérez  ", "ANA@EXAMPLE.COM", "Clave123"));

        Assert.NotNull(response);
        var savedUser = Assert.Single(_users.Items);
        Assert.Equal("Ana Pérez", savedUser.Name);
        Assert.Equal("ana@example.com", savedUser.Email);
        Assert.NotEqual("Clave123", savedUser.PasswordHash);
        Assert.True(_passwordHasher.Verify("Clave123", savedUser.PasswordHash));
        Assert.Equal(savedUser.Id, response.User.Id);
        Assert.Equal("test-token", response.Token);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ReturnsNull()
    {
        _users.Items.Add(CreateUser("ana@example.com", "Clave123"));
        var service = CreateService();

        var response = await service.RegisterAsync(
            new RegisterRequest("Otra Ana", "ANA@example.com", "Otra1234"));

        Assert.Null(response);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsToken()
    {
        _users.Items.Add(CreateUser("ana@example.com", "Clave123"));
        var service = CreateService();

        var response = await service.LoginAsync(new LoginRequest("ana@example.com", "Clave123"));

        Assert.NotNull(response);
        Assert.Equal("test-token", response.Token);
        Assert.Equal("ana@example.com", response.User.Email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        _users.Items.Add(CreateUser("ana@example.com", "Clave123"));
        var service = CreateService();

        var response = await service.LoginAsync(new LoginRequest("ana@example.com", "incorrecta"));

        Assert.Null(response);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithExistingAccount_CreatesTemporaryHashedToken()
    {
        var user = CreateUser("ana@example.com", "Clave123");
        _users.Items.Add(user);
        var service = CreateService();

        var token = await service.RequestPasswordResetAsync(
            new ForgotPasswordRequest("ANA@example.com"));

        Assert.NotNull(token);
        Assert.Equal(64, token.Length);
        Assert.NotEqual(token, user.PasswordResetTokenHash);
        Assert.True(user.PasswordResetTokenExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_WithUnknownAccount_ReturnsNull()
    {
        var service = CreateService();

        var token = await service.RequestPasswordResetAsync(
            new ForgotPasswordRequest("nadie@example.com"));

        Assert.Null(token);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ChangesPasswordAndConsumesToken()
    {
        var user = CreateUser("ana@example.com", "Clave123");
        _users.Items.Add(user);
        var service = CreateService();
        var token = await service.RequestPasswordResetAsync(
            new ForgotPasswordRequest("ana@example.com"));

        var changed = await service.ResetPasswordAsync(
            new ResetPasswordRequest("ana@example.com", token!, "Nueva1234"));
        var reused = await service.ResetPasswordAsync(
            new ResetPasswordRequest("ana@example.com", token!, "Otra1234"));

        Assert.True(changed);
        Assert.False(reused);
        Assert.True(_passwordHasher.Verify("Nueva1234", user.PasswordHash));
        Assert.Null(user.PasswordResetTokenHash);
        Assert.Null(user.PasswordResetTokenExpiresAtUtc);
    }

    [Fact]
    public async Task ResetPasswordAsync_WithExpiredToken_ReturnsFalse()
    {
        var user = CreateUser("ana@example.com", "Clave123");
        _users.Items.Add(user);
        var service = CreateService();
        var token = await service.RequestPasswordResetAsync(
            new ForgotPasswordRequest("ana@example.com"));
        user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);

        var changed = await service.ResetPasswordAsync(
            new ResetPasswordRequest("ana@example.com", token!, "Nueva1234"));

        Assert.False(changed);
        Assert.True(_passwordHasher.Verify("Clave123", user.PasswordHash));
    }

    [Theory]
    [InlineData("corta")]
    [InlineData("sololetras")]
    [InlineData("12345678")]
    public async Task RegisterAsync_WithWeakPassword_ThrowsValidation(string password)
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ValidationException>(() => service.RegisterAsync(
            new RegisterRequest("Ana", "ana@example.com", password)));
    }

    private AuthService CreateService() => new(
        _users,
        new FakeUnitOfWork(),
        _passwordHasher,
        new FakeTokenGenerator());

    private User CreateUser(string email, string password) => new()
    {
        Name = "Ana",
        Email = email,
        PasswordHash = _passwordHasher.Hash(password)
    };

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.Any(x => x.Email == email));

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(x => x.Email == email));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Items.SingleOrDefault(x => x.Id == id));

        public void Add(User user) => Items.Add(user);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);
    }

    private sealed class FakeTokenGenerator : IJwtTokenGenerator
    {
        public (string Token, DateTime ExpiresAtUtc) Generate(User user)
            => ("test-token", DateTime.UtcNow.AddHours(2));
    }
}
