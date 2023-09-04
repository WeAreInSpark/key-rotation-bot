using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Configuration;

namespace Kerbee.Options;

public class WebsiteOptions
{
    [Required]
    [ConfigurationKeyName("WEBSITE_SITE_NAME")]
    public string SiteName { get; set; } = null!;
}
