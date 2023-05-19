using System;

using Azure;
using Azure.Data.Tables;

using Kerbee.Models;

namespace Kerbee.Entities;

public class ApplicationEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "kerbee";
    public string RowKey { get; set; } = null!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    public string DisplayName { get; set; } = null!;
    public object AppId { get; internal set; } = null!;
    public string? KeyVaultKeyId { get; internal set; }
    public Guid? KeyId { get; internal set; }
    public KeyType KeyType { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
}
