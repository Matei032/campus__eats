using System.Net.Http.Json;
using System.Text.Json;
using CampusEats.Frontend.Models.Auth;
using CampusEats.Frontend.Services;
using CampusEats.Frontend.State;
using Microsoft.AspNetCore.Components.Authorization;

namespace CampusEats.Frontend.Features.Auth
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private readonly LocalStorageService _localStorageService;
        private readonly AuthState _authState;

        // 1. REPARAT: Am readăugat evenimentul pe care îl caută AuthBadge și HeaderBar
        public event Action? AuthStateChanged;

        public AuthService(HttpClient httpClient, 
                           LocalStorageService localStorageService, 
                           AuthState authState)
        {
            _httpClient = httpClient;
            _localStorageService = localStorageService;
            _authState = authState;
        }

        public async Task InitializeAsync()
        {
            var token = await _localStorageService.GetItemAsync("authToken");

            if (!string.IsNullOrWhiteSpace(token))
            {
                // Acum preluăm și userId
                var (userId, email, roles) = ParseJwt(token);
                
                // Trimitem userId către AuthState (trebuie să modifici și AuthState.Set!)
                _authState.Set(token, userId, email, roles); 
            }
            else
            {
                _authState.Clear();
            }
            NotifyStateChanged();
        }

        // 2. REPARAT: Redenumit în LoginAsync și schimbat return-ul în (bool, List<string>)
        // pentru a fi compatibil cu Login.razor
        public async Task<(bool ok, List<string> errors)> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var authData = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

                    if (authData != null && !string.IsNullOrEmpty(authData.Token))
                    {
                        await HandleAuthSuccess(authData);
                        return (true, new List<string>());
                    }
                }

                // Extragem eroarea din backend dacă există
                var errorMsg = await response.Content.ReadAsStringAsync();
                return (false, new List<string> { "Autentificare eșuată.", errorMsg });
            }
            catch (Exception ex)
            {
                return (false, new List<string> { ex.Message });
            }
        }

        public async Task<(bool ok, List<string> errors)> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);

                if (response.IsSuccessStatusCode)
                {
                    // ÎNCERCĂM să citim token-ul pentru auto-login
                    try
                    {
                        var authData = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                        
                        // Dacă avem token, logăm utilizatorul automat
                        if (authData != null && !string.IsNullOrEmpty(authData.Token))
                        {
                            await HandleAuthSuccess(authData);
                        }
                    }
                    catch (JsonException)
                    {
                        // EROAREA TA ERA AICI!
                        // Dacă backend-ul a trimis text simplu (ex: "Success") în loc de JSON, apărea eroarea.
                        // Acum o prindem și o ignorăm (pentru că response.IsSuccessStatusCode e true, deci contul s-a creat).
                        Console.WriteLine("Register reușit, dar fără token automat.");
                    }

                    // Returnăm true pentru că userul a fost creat în baza de date
                    return (true, new List<string>());
                }
                
                // Gestionare erori (400 Bad Request etc.)
                var errorMsg = await response.Content.ReadAsStringAsync();
                return (false, new List<string> { "Înregistrare eșuată.", errorMsg });
            }
            catch (Exception ex)
            {
                return (false, new List<string> { ex.Message });
            }
        }

        // 3. REPARAT: Redenumit în LogoutAsync
        public async Task LogoutAsync()
        {
            await _localStorageService.RemoveItemAsync("authToken");
            _authState.Clear();
            NotifyStateChanged();
        }

        private async Task HandleAuthSuccess(AuthResponseDto authData)
        {
            await _localStorageService.SetItemAsync("authToken", authData.Token);
            
            // Backend-ul ne dă ID-ul direct în authData.User.Id, deci e sigur
            var userId = authData.User.Id.ToString();
            var roles = new List<string> { authData.User.Role };
            
            _authState.Set(authData.Token, userId, authData.User.Email, roles);
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => AuthStateChanged?.Invoke();

        // Helper actualizat pentru a extrage ID-ul utilizatorului
        private static (string userId, string email, List<string> roles) ParseJwt(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2) return ("", "", new List<string>());

                var payload = parts[1];
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }
                
                var jsonBytes = Convert.FromBase64String(payload);
                var jsonString = System.Text.Encoding.UTF8.GetString(jsonBytes);
                
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                // 1. Extragem User ID (cheia 'sub' sau 'nameid')
                string userId = "";
                if (root.TryGetProperty("sub", out var sub)) userId = sub.GetString() ?? "";
                else if (root.TryGetProperty("nameid", out var nid)) userId = nid.GetString() ?? "";
                
                // 2. Extragem Email
                string email = "";
                if (root.TryGetProperty("email", out var e)) email = e.GetString() ?? "";
                else if (root.TryGetProperty("unique_name", out var un)) email = un.GetString() ?? "";

                // 3. Extragem Roluri
                var roles = new List<string>();
                string[] roleKeys = { "role", "roles", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" };
                
                foreach (var key in roleKeys)
                {
                    if (root.TryGetProperty(key, out var r))
                    {
                        if (r.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var item in r.EnumerateArray()) roles.Add(item.GetString()!);
                        }
                        else if (r.ValueKind == JsonValueKind.String)
                        {
                            roles.Add(r.GetString()!);
                        }
                    }
                }

                return (userId, email, roles);
            }
            catch
            {
                return ("", "", new List<string>());
            }
        }
    }
}