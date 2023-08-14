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
    public Guid AppId { get; set; }
    public string? KeyVaultKeyId { get; set; }
    public Guid? KeyId { get; set; }
    public KeyType KeyType { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
}
