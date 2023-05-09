using System;

namespace Kerbee.Models;

public class Application
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; }
    public object AppId { get; internal set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
}
