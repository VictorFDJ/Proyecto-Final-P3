using System.ComponentModel.DataAnnotations;

namespace MiPresupuesto.Application.Auth;

public sealed record RegisterRequest(
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    string Name,
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    [MaxLength(256, ErrorMessage = "El correo no puede superar los 256 caracteres.")]
    string Email,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$",
        ErrorMessage = "La contraseña debe incluir letras y números.")]
    string Password);
public sealed record LoginRequest(
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
    string Email,
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    string Password);
public sealed record AuthResponse(string Token, DateTime ExpiresAtUtc, UserResponse User);
public sealed record UserResponse(Guid Id, string Name, string Email);
