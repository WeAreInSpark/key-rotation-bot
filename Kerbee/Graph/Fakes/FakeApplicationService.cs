using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Bogus;

using Kerbee.Models;

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
        s_unmanagedApplications.Remove(s_unmanagedApplications.First(x => x.Id == application.Id));
        return Task.CompletedTask;
    }

    public Task DeleteApplicationAsync(Application application)
    {
        s_applications.Remove(s_applications.First(x => x.Id == application.Id));
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Application>> GetApplicationsAsync()
    {
        return Task.FromResult((IEnumerable<Application>)s_applications.ToArray());
    }

    public Task<IEnumerable<Application>> GetUnmanagedApplicationsAsync()
    {
        return Task.FromResult((IEnumerable<Application>)s_unmanagedApplications.ToArray());
    }
}
