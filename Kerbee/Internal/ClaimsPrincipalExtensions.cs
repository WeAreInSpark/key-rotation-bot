using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Azure.Functions.Worker.Http;

namespace Kerbee.Internal;

public static class ClaimsPrincipalExtensions
{
    private class ClientPrincipalClaim
    {
        [JsonPropertyName("typ")]
        public string Type { get; set; }
        [JsonPropertyName("val")]
        public string Value { get; set; }
    }

    private class ClientPrincipal
    {
        [JsonPropertyName("auth_typ")]
        public string IdentityProvider { get; set; }
        [JsonPropertyName("name_typ")]
        public string NameClaimType { get; set; }
        [JsonPropertyName("role_typ")]
        public string RoleClaimType { get; set; }
        [JsonPropertyName("claims")]
        public IEnumerable<ClientPrincipalClaim> Claims { get; set; }
    }

    /// <summary>
    /// Code below originally from Microsoft Docs - https://docs.microsoft.com/en-gb/azure/static-web-apps/user-information?tabs=csharp#api-functions
    /// </summary>
    /// <param name="req">The HttpRequestData header.</param>
    /// <returns>Parsed ClaimsPrincipal from 'x-ms-client-principal' header.</returns>
    public static ClaimsPrincipal ParsePrincipal(this HttpRequestData req)
    {
        var principal = new ClientPrincipal();

        if (req.Headers.TryGetValues("x-ms-client-principal", out var header))
        {
            var data = header.First();
            var decoded = Convert.FromBase64String(data);
            var json = Encoding.UTF8.GetString(decoded);
            principal = JsonSerializer.Deserialize<ClientPrincipal>(json)!;
        }

        var identity = new ClaimsIdentity(principal.IdentityProvider);
        identity.AddClaims(principal.Claims.Select(c => new Claim(c.Type, c.Value)));

        return new ClaimsPrincipal(identity);
    }
}
