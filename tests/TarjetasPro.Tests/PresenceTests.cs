using TarjetasPro.Api.Presence;

namespace TarjetasPro.Tests;

/// <summary>«En línea ahora»: cuenta personas, no pestañas, y no incluye al admin que consulta.</summary>
public class PresenceTests
{
    private const string Admin = "admin", Hermano = "hermano";

    private static (int Total, int SignedIn, int Guests) Count(PresenceTracker t, string? except = Admin)
    {
        var s = t.Snapshot(except);
        return (s.Total, s.SignedIn, s.Guests);
    }

    [Fact]
    public void Varias_pestanas_sin_sesion_en_un_navegador_son_un_visitante()
    {
        var t = new PresenceTracker();
        t.Ping("a", "/", null, "navegador-1", false);
        t.Ping("b", "/menu", null, "navegador-1", false);
        t.Ping("c", "/", null, "navegador-2", false);
        Assert.Equal((2, 0, 2), Count(t));
    }

    [Fact]
    public void El_admin_no_cuenta_aunque_una_pestana_suya_tenga_el_acceso_vencido()
    {
        var t = new PresenceTracker();
        t.Ping("a", "/admin/visitas", Admin, "mi-compu", true);
        t.Ping("b", "/dashboard", null, "mi-compu", true);
        Assert.Equal((0, 0, 0), Count(t));
    }

    [Fact]
    public void Se_recuerda_de_quien_es_el_navegador_aunque_ya_no_haya_pestanas_con_acceso()
    {
        var t = new PresenceTracker();
        t.Ping("a", "/dashboard", Hermano, "su-compu", true);
        t.Leave("a");
        t.Ping("b", "/dashboard", null, "su-compu", true);
        t.Ping("c", "/perfil", Hermano, "su-celular", true);
        // Computadora (acceso vencido) y celular: la misma persona, con sesión.
        Assert.Equal((1, 1, 0), Count(t));
    }

    [Fact]
    public void Con_sesion_pero_sin_saber_de_quien_cuenta_como_una_persona_con_sesion()
    {
        var t = new PresenceTracker();
        t.Ping("a", "/dashboard", null, "reciente", true);
        t.Ping("b", "/perfil", null, "reciente", true);
        Assert.Equal((1, 1, 0), Count(t));
    }

    [Fact]
    public void Al_cerrar_sesion_el_navegador_vuelve_a_ser_visitante()
    {
        var t = new PresenceTracker();
        t.Ping("a", "/dashboard", Hermano, "compartida", true);
        t.Ping("a", "/", null, "compartida", false);
        t.Leave("a");
        t.Ping("b", "/", null, "compartida", false);
        Assert.Equal((1, 0, 1), Count(t));
    }

    [Fact]
    public void La_web_anterior_sin_identificador_sigue_contando()
    {
        var t = new PresenceTracker();
        t.Ping("a", "/dashboard", Hermano);
        t.Ping("a", "/dashboard", null);
        t.Ping("b", "/", null);
        Assert.Equal((2, 1, 1), Count(t));
    }

    [Fact]
    public void Las_paginas_cuentan_personas_y_quien_sale_desaparece()
    {
        var t = new PresenceTracker();
        t.Ping("a", "/", null, "n1", false);
        t.Ping("b", "/", null, "n1", false);
        t.Ping("c", "/", null, "n2", false);
        Assert.Equal(2, Assert.Single(t.Snapshot(Admin).Pages).Count);
        foreach (var tab in new[] { "a", "b", "c" }) t.Leave(tab);
        Assert.Equal((0, 0, 0), Count(t));
    }
}
