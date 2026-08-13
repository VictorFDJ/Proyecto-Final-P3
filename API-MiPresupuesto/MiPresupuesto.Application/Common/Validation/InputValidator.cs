using System.Diagnostics.CodeAnalysis;
using System.Net.Mail;
using MiPresupuesto.Application.Common.Exceptions;

namespace MiPresupuesto.Application.Common.Validation;

public static class InputValidator
{
    public static string Required(string? value, string field, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            Throw(field, "Este campo es obligatorio.");
        }

        if (normalized.Length > maxLength)
        {
            Throw(field, $"No puede superar los {maxLength} caracteres.");
        }

        return normalized;
    }

    public static string Email(string? value)
    {
        var email = Required(value, "email", 256).ToLowerInvariant();
        try
        {
            var parsed = new MailAddress(email);
            if (!string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                Throw("email", "El correo electrónico no es válido.");
            }
        }
        catch (FormatException)
        {
            Throw("email", "El correo electrónico no es válido.");
        }

        return email;
    }

    public static string Password(string? value, string field = "password")
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8)
        {
            Throw(field, "La contraseña debe tener al menos 8 caracteres.");
        }

        if (!value.Any(char.IsLetter) || !value.Any(char.IsDigit))
        {
            Throw(field, "La contraseña debe incluir letras y números.");
        }

        return value;
    }

    [DoesNotReturn]
    private static void Throw(string field, string message) =>
        throw new ValidationException("Revisa los datos enviados.",
            new Dictionary<string, string[]> { [field] = [message] });
}
