using System.ComponentModel.DataAnnotations;

namespace Kerbee.Options;

public class KerbeeOptions
{
    [Required]
    [Url]
    public string Endpoint { get; set; } = "https://acme-v02.api.letsencrypt.org/";

    [Required]
    public string Contacts { get; set; }

    [Required]
    public string VaultBaseUrl { get; set; }

    [Url]
    public string Webhook { get; set; }

    [Required]
    public string Environment { get; set; } = "AzureCloud";

    public string PreferredChain { get; set; }

    public bool MitigateChainOrder { get; set; } = false;

    [Range(0, 365)]
    public int RenewBeforeExpiry { get; set; } = 30;
}
