using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.Extensions.Configuration;

namespace Kerbee.Options;

public class AzureAdOptions
{
    [Required]
    [ConfigurationKeyName("WEBSITE_AUTH_OPENID_ISSUER")]
    public Uri Issuer { get; set; }

    public string TenantId
    {
        get
        {
            if (Issuer is null)
            {
                return null;
            }

            return Issuer.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
    }

    [Required]
    [ConfigurationKeyName("WEBSITE_AUTH_CLIENT_ID")]
    public string ClientId { get; set; }

    [Required]
    [ConfigurationKeyName("MICROSOFT_PROVIDER_AUTHENTICATION_SECRET")]
    public string ClientSecret { get; set; }
}
