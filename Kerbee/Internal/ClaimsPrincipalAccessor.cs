using System.Security.Claims;
using System.Threading;

namespace Kerbee.Internal;

public interface IClaimsPrincipalAccessor
{
    string? AccessToken { get; set; }
    ClaimsPrincipal? Principal { get; set; }
}

public class ClaimsPrincipalAccessor : IClaimsPrincipalAccessor
{
    private readonly AsyncLocal<ContextHolder> _context = new();

    public string? AccessToken
    {
        get => _context.Value?.AccessToken;
        set
        {
            _context.Value ??= new ContextHolder();
            _context.Value.AccessToken = value;
        }
    }

    public ClaimsPrincipal? Principal
    {
        get => _context.Value?.Principal;
        set
        {
            _context.Value ??= new ContextHolder();
            _context.Value.Principal = value;
        }
    }

    private class ContextHolder
    {
        public string? AccessToken;
        public ClaimsPrincipal? Principal;
    }
}
