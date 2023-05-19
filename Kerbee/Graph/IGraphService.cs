﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Graph.Models;

namespace Kerbee.Graph;

public interface IGraphService
{
    Task MakeManagedIdentityOwnerOfApplicationAsync(string applicationObjectId);
    Task<IEnumerable<Application>> GetApplicationsAsync();
    Task<IEnumerable<Application>> GetUnmanagedApplicationsAsync();
    Task<string> GeneratePasswordAsync(Application application);
    Task<Guid> AddCertificateAsync(string applicationObjectId, byte[] cer);
}
