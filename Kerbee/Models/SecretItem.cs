using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kerbee.Models;
public class SecretItem
{
    public int Id { get; set; }
    public string Description { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public DateTimeOffset ExpiresOn { get; set; }
}
