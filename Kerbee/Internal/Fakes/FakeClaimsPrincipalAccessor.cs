using System.Security.Claims;

namespace Kerbee.Internal.Fakes;

public class FakeClaimsPrincipalAccessor : IClaimsPrincipalAccessor
{
    public string? AccessToken { get; set; }
    public ClaimsPrincipal? Principal { get; set; }
}
