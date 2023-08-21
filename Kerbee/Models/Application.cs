using System;

namespace Kerbee.Models;

public class Application
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public Guid AppId { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
    public string? KeyId { get; internal set; }
    public KeyType KeyType { get; set; }
    public string? KeyName { get; set; }
}
