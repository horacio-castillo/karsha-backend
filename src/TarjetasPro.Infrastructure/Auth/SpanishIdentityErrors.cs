using Microsoft.AspNetCore.Identity;

namespace TarjetasPro.Infrastructure.Auth;

/// <summary>Los errores de cuentas que ve la persona, en español. Usuario y correo repetidos dicen lo mismo para no mostrarse dos veces.</summary>
public class SpanishIdentityErrors : IdentityErrorDescriber
{
    private const string Taken = "Ese correo ya tiene una cuenta. Inicia sesión o recupera tu contraseña.";
    private const string BadEmail = "Revisa tu correo: parece que le falta algo (ej. tu@correo.com).";

    private static IdentityError E(string code, string text) => new() { Code = code, Description = text };

    public override IdentityError DefaultError() => E(nameof(DefaultError), "No pudimos completar la operación. Intenta de nuevo.");
    public override IdentityError ConcurrencyFailure() => E(nameof(ConcurrencyFailure), "La cuenta cambió mientras la guardábamos. Intenta de nuevo.");
    public override IdentityError DuplicateUserName(string userName) => E(nameof(DuplicateUserName), Taken);
    public override IdentityError DuplicateEmail(string email) => E(nameof(DuplicateEmail), Taken);
    public override IdentityError InvalidUserName(string? userName) => E(nameof(InvalidUserName), BadEmail);
    public override IdentityError InvalidEmail(string? email) => E(nameof(InvalidEmail), BadEmail);
    public override IdentityError InvalidToken() => E(nameof(InvalidToken), "El enlace no es válido o ya venció.");
    public override IdentityError PasswordMismatch() => E(nameof(PasswordMismatch), "La contraseña no es correcta.");
    public override IdentityError PasswordTooShort(int length) => E(nameof(PasswordTooShort), $"La contraseña necesita {length} caracteres o más.");
    public override IdentityError PasswordRequiresDigit() => E(nameof(PasswordRequiresDigit), "La contraseña necesita un número.");
    public override IdentityError PasswordRequiresUpper() => E(nameof(PasswordRequiresUpper), "La contraseña necesita una mayúscula.");
    public override IdentityError PasswordRequiresLower() => E(nameof(PasswordRequiresLower), "La contraseña necesita una minúscula.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => E(nameof(PasswordRequiresNonAlphanumeric), "La contraseña necesita un símbolo.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => E(nameof(PasswordRequiresUniqueChars), $"La contraseña necesita al menos {uniqueChars} caracteres distintos.");
    public override IdentityError LoginAlreadyAssociated() => E(nameof(LoginAlreadyAssociated), "Ese acceso ya está ligado a otra cuenta.");
}
