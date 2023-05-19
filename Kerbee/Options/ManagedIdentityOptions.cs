﻿using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Configuration;

namespace Kerbee.Options;

public class ManagedIdentityOptions
{
    [Required]
    [ConfigurationKeyName("AZURE_CLIENT_ID")]
    public string ClientId { get; set; } = null!;
}
