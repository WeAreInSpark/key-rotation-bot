using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Kerbee.Models;

using Microsoft.AspNetCore.Mvc;

using OneOf;
using OneOf.Types;

namespace Kerbee.Graph;

public interface IApplicationService
{
    Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetUnmanagedApplicationsAsync();

    Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetApplicationsAsync();

    Task AddApplicationAsync(Application application);
}
