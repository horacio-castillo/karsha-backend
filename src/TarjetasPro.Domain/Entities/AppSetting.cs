namespace TarjetasPro.Domain.Entities;

/// <summary>Ajuste global de la plataforma (clave → valor), editable por el Admin.</summary>
public class AppSetting
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public static class AppSettingKeys
{
    /// <summary>"1" = tras iniciar sesión se muestra «¿Qué quieres hacer?»; "0" = va directo al panel.</summary>
    public const string HomePicker = "home.picker";
}
