using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Common.Validation;
using MiPresupuesto.Domain.Entities;

namespace MiPresupuesto.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator) : IAuthService
{
    public async Task<AuthResponse?> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = InputValidator.Required(request.Name, "name", 100);
        var email = InputValidator.Email(request.Email);
        var password = InputValidator.Password(request.Password);

        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = passwordHasher.Hash(password)
        };

        users.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateResponse(user);
    }

    public async Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = InputValidator.Email(request.Email);
        if (string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var token = tokenGenerator.Generate(user);
        return new AuthResponse(
            token.Token,
            token.ExpiresAtUtc,
            new UserResponse(user.Id, user.Name, user.Email));
    }
}
