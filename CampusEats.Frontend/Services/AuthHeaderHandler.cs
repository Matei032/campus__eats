using System.Net.Http.Headers;
using CampusEats.Frontend.Services;

namespace CampusEats.Frontend.Services
{
    public class AuthHeaderHandler : DelegatingHandler
    {
        private readonly LocalStorageService _localStorageService;

        public AuthHeaderHandler(LocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 1. Citește token-ul din stocarea locală
            // Nota: Cheia "authToken" trebuie să fie aceeași cu cea folosită în AuthService la Login
            var token = await _localStorageService.GetItemAsync("authToken");

            // 2. Dacă avem un token, îl atașăm la header-ul Authorization
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // 3. Trimite cererea mai departe către backend
            return await base.SendAsync(request, cancellationToken);
        }
    }
}