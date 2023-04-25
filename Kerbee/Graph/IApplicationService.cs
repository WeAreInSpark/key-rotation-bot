using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Models;

using OneOf;
using OneOf.Types;

namespace Kerbee.Graph;

public interface IApplicationService
{
    Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetApplicationsAsync();

    Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetManagedApplicationsAsync();
}
