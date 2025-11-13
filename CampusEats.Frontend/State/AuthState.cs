using System;
using System.Collections.Generic;

namespace CampusEats.Frontend.State;

public class AuthState
{
    public string? Token { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public List<string> Roles { get; private set; } = new();
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(Token);

    public event Action? Changed;

    public void Set(string? token, string username, IEnumerable<string> roles)
    {
        Token = token;
        Username = username ?? string.Empty;
        Roles = new List<string>(roles ?? Array.Empty<string>());
        Changed?.Invoke();
    }

    public void Clear()
    {
        Token = null;
        Username = string.Empty;
        Roles = new();
        Changed?.Invoke();
    }
}