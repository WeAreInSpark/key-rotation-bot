using System;
using System.Linq;
using System.Security.Claims;

namespace Kerbee.Internal;

internal static class AppRoleExtensions
{
    private const string IssueCertificateAppRole = "Kerbee.IssueCertificate";
    private const string RevokeCertificateAppRole = "Kerbee.RevokeCertificate";

    private static bool IsAppRoleRequired => bool.TryParse(Environment.GetEnvironmentVariable("Kerbee:AppRoleRequired"), out var result) && result;

    private static bool IsInAppRole(this ClaimsPrincipal claimsPrincipal, string role)
    {
        var roles = claimsPrincipal.Claims.Where(x => x.Type == "roles").Select(x => x.Value);

        return roles.Contains(role);
    }

    public static bool HasIssueCertificateRole(this ClaimsPrincipal claimsPrincipal) => !IsAppRoleRequired || claimsPrincipal.IsInAppRole(IssueCertificateAppRole);

    public static bool HasRevokeCertificateRole(this ClaimsPrincipal claimsPrincipal) => !IsAppRoleRequired || claimsPrincipal.IsInAppRole(RevokeCertificateAppRole);
}
