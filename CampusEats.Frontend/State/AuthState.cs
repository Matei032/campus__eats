using System;
using System.Collections.Generic;

namespace CampusEats.Frontend.State
{
    public class AuthState
    {
        public string? Token { get; private set; }
        public string UserId { get; private set; } = string.Empty; // <--- NOU
        public string Username { get; private set; } = string.Empty; // Email
        public List<string> Roles { get; private set; } = new();
        public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

        public event Action? Changed;

        // Metoda Set actualizată
        public void Set(string? token, string userId, string username, IEnumerable<string> roles)
        {
            Token = token;
            UserId = userId ?? string.Empty;
            Username = username ?? string.Empty;
            Roles = new List<string>(roles ?? Array.Empty<string>());
            Changed?.Invoke();
        }

        public void Clear()
        {
            Token = null;
            UserId = string.Empty;
            Username = string.Empty;
            Roles = new();
            Changed?.Invoke();
        }
    }
}