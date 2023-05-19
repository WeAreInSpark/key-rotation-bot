using System;

namespace Kerbee.Models;
public class SecretItem
{
    public int Id { get; set; }
    public string Description { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
}
