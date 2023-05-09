using System.Collections.Generic;

using Riok.Mapperly.Abstractions;

namespace Kerbee.Models;

[Mapper]
public static partial class MapperExtensions
{
    public static partial Application ToModel(this Microsoft.Graph.Models.Application application);
    public static partial IEnumerable<Application> ToModel(this IEnumerable<Microsoft.Graph.Models.Application> application);
}
