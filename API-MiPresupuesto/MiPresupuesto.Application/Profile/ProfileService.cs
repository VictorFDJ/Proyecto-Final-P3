using MiPresupuesto.Application.Auth;
using MiPresupuesto.Application.Common.Exceptions;
using MiPresupuesto.Application.Common.Validation;

namespace MiPresupuesto.Application.Profile;

public interface IProfileService
{
    Task<UserResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateNameAsync(Guid userId, UpdateNameRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public sealed class ProfileService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher) : IProfileService
{
    public async Task<UserResponse> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        return new UserResponse(user.Id, user.Name, user.Email);
    }

    public async Task<UserResponse> UpdateNameAsync(
        Guid userId,
        UpdateNameRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        user.Name = InputValidator.Required(request.Name, "name", 100);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UserResponse(user.Id, user.Name, user.Email);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserAsync(userId, cancellationToken);
        if (!passwordHasher.Verify(request.CurrentPassword ?? string.Empty, user.PasswordHash))
        {
            throw new ValidationException("No se pudo cambiar la contraseña.",
                new Dictionary<string, string[]> { ["currentPassword"] = ["La contraseña actual es incorrecta."] });
        }

        var newPassword = InputValidator.Password(request.NewPassword, "newPassword");
        user.PasswordHash = passwordHasher.Hash(newPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Domain.Entities.User> GetUserAsync(Guid userId, CancellationToken cancellationToken)
        => await users.GetByIdAsync(userId, cancellationToken)
           ?? throw new NotFoundException("El usuario no existe.");
}
