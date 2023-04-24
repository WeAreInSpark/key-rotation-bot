using System.ComponentModel.DataAnnotations;

namespace Kerbee.Options;

public class KerbeeOptions
{
    [Required]
    public string VaultBaseUrl { get; set; }

    [Url]
    public string Webhook { get; set; }

    [Required]
    public string Environment { get; set; } = "AzureCloud";

    [Range(0, 365)]
    public int RenewBeforeExpiry { get; set; } = 30;
}
