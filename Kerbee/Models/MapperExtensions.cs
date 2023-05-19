using System.Collections.Generic;

using Kerbee.Entities;

using Riok.Mapperly.Abstractions;

namespace Kerbee.Models;

[Mapper]
public static partial class MapperExtensions
{
    [MapProperty(nameof(Application.Id), nameof(ApplicationEntity.RowKey))]
    public static partial ApplicationEntity ToEntity(this Application application);

    [MapProperty(nameof(ApplicationEntity.RowKey), nameof(Application.Id))]
    public static partial Application ToModel(this ApplicationEntity applicationEntity);
    public static partial IEnumerable<Application> ToModel(this IEnumerable<ApplicationEntity> applicationEntities);

    public static partial Application ToModel(this Microsoft.Graph.Models.Application applicationEntity);
    public static partial IEnumerable<Application> ToModel(this IEnumerable<Microsoft.Graph.Models.Application> applicationEntities);
}
