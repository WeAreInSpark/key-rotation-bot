using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Bogus;

using Kerbee.Models;

using Microsoft.AspNetCore.Mvc;

using OneOf;
using OneOf.Types;

namespace Kerbee.Graph.Fakes;

internal class FakeApplicationService : IApplicationService
{
    static readonly Faker<Application> s_faker = new Faker<Application>()
        .RuleFor(fake => fake.Id, fake => Guid.NewGuid())
        .RuleFor(fake => fake.AppId, fake => Guid.NewGuid().ToString())
        .RuleFor(fake => fake.DisplayName, fake => fake.Company.CompanyName())
        .RuleFor(fake => fake.CreatedOn, fake => fake.Date.Past())
        .RuleFor(fake => fake.ExpiresOn, fake => fake.Date.Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddMonths(5)));

    static readonly List<Application> s_applications = s_faker.Generate(5);
    static readonly List<Application> s_unmanagedApplications = s_faker.Generate(50);

    public Task AddApplicationAsync(Application application)
    {
        s_applications.Add(application);
        s_unmanagedApplications.Remove(s_unmanagedApplications.First(x=>x.Id == application.Id));
        return Task.CompletedTask;
    }

    public Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetApplicationsAsync()
    {
        var result = OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>.FromT0(s_applications);
        return Task.FromResult(result);
    }

    public Task<OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>> GetUnmanagedApplicationsAsync()
    {
        var result = OneOf<IEnumerable<Application>, UnauthorizedResult, Error<Exception>>.FromT0(s_unmanagedApplications);
        return Task.FromResult(result);
    }
}
