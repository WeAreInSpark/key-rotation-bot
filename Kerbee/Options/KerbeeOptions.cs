using System.ComponentModel.DataAnnotations;

using Kerbee.Models;

namespace Kerbee.Options;

public class KerbeeOptions
{
    public const string Kerbee = "Kerbee";

    [Required]
    public string VaultBaseUrl { get; set; } = null!;

    [Url]
    public string? Webhook { get; set; }

    [Required]
    public string Environment { get; set; } = "AzureCloud";

    [Range(0, 365)]
    public int RenewBeforeExpiryInDays { get; set; } = 30;

    [Range(0, 12)]
    public int ValidityInMonths { get; set; } = 3;

    public KeyType DefaultKeyType { get; set; } = KeyType.Certificate;
}
