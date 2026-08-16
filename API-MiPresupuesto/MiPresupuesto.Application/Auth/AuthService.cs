using System.Security.Cryptography;
using System.Text;
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

    public async Task<string?> RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = InputValidator.Email(request.Email);
        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetTokenHash = HashToken(token);
        user.PasswordResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(15);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<bool> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = InputValidator.Email(request.Email);
        var newPassword = InputValidator.Password(request.NewPassword, "newPassword");
        var user = await users.GetByEmailAsync(email, cancellationToken);

        if (user?.PasswordResetTokenHash is null ||
            user.PasswordResetTokenExpiresAtUtc is null ||
            user.PasswordResetTokenExpiresAtUtc <= DateTime.UtcNow ||
            !TokenMatches(user.PasswordResetTokenHash, request.Token))
        {
            return false;
        }

        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private AuthResponse CreateResponse(User user)
    {
        var token = tokenGenerator.Generate(user);
        return new AuthResponse(
            token.Token,
            token.ExpiresAtUtc,
            new UserResponse(user.Id, user.Name, user.Email));
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool TokenMatches(string storedHash, string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(storedHash),
                Convert.FromHexString(HashToken(token.Trim())));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
