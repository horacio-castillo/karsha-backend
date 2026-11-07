using Microsoft.AspNetCore.Authorization;
using TarjetasPro.Domain.Security;

namespace TarjetasPro.Api.Security;

public static class PermissionPolicies
{
    public static AuthorizationOptions AddPermissionPolicies(this AuthorizationOptions options)
    {
        foreach (var permission in Permissions.All)
            options.AddPolicy(permission, p => p.RequireClaim(Permissions.ClaimType, permission));
        return options;
    }
}
