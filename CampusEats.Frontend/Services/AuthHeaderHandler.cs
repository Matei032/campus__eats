using System.Net.Http.Headers;
using CampusEats.Frontend.State;

namespace CampusEats.Frontend.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly AuthState _state;
    public AuthHeaderHandler(AuthState state) => _state = state;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_state.Token) && request.Headers.Authorization is null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _state.Token);
        }
        return base.SendAsync(request, cancellationToken);
    }
}