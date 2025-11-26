using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using CampusEats.Frontend.State;

namespace CampusEats.Frontend.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider, IDisposable
    {
        private readonly AuthState _authState;

        public CustomAuthStateProvider(AuthState authState)
        {
            _authState = authState;
            // Ne abonăm la evenimentul Changed din AuthState
            _authState.Changed += OnAuthStateChanged;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();

            if (_authState.IsAuthenticated)
            {
                var claims = new List<Claim>
                {
                    // Adăugăm ID-ul utilizatorului ca "sub" (Subject)
                    new Claim("sub", _authState.UserId), 
                    new Claim(ClaimTypes.NameIdentifier, _authState.UserId), // Pentru siguranță
                    new Claim(ClaimTypes.Name, _authState.Username)
                };

                foreach (var role in _authState.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                identity = new ClaimsIdentity(claims, "JwtAuth");
            }

            var user = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }

        private void OnAuthStateChanged()
        {
            // Notificăm Blazor că starea s-a schimbat (login/logout)
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public void Dispose()
        {
            _authState.Changed -= OnAuthStateChanged;
        }
    }
}