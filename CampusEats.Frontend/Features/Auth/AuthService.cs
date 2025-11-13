using System.Net.Http.Json;
using System.Text.Json;
using CampusEats.Frontend.Services;
using CampusEats.Frontend.State;

namespace CampusEats.Frontend.Features.Auth;

public class AuthService
{
	public const string JwtKey = "campuseats.jwt";
	public const string EmailKey = "campuseats.email";

	private static readonly string[] FallbackJwtKeys = new[]
	{
		"jwt", "token", "access_token", "auth.token", "campuseats_token"
	};

	private readonly LocalStorageService _storage;
	private readonly AuthState _state;
	private readonly HttpClient _http;

	// Componentele (AuthBadge/Nav/HeaderBar) se abonează la acest eveniment
	public event Action? AuthStateChanged;

	public AuthService(LocalStorageService storage, AuthState state, HttpClient http)
	{
		_storage = storage;
		_state = state;
		_http = http;
	}

	// Se apelează în Program.cs înainte de RunAsync
	public async Task InitializeAsync()
	{
		var token = await TryReadTokenAsync();
		if (!string.IsNullOrWhiteSpace(token))
		{
			var (username, roles) = ParseJwt(token);
			_state.Set(token, username, roles);
			AuthStateChanged?.Invoke();
			Console.WriteLine($"[AUTH] Initialized. User={username}, Roles=[{string.Join(",", roles)}]");
		}
		else
		{
			_state.Clear();
			AuthStateChanged?.Invoke();
			Console.WriteLine("[AUTH] No token in local storage.");
		}
	}

	public async Task SaveTokenAsync(string token, string? usernameHint = null)
	{
		if (string.IsNullOrWhiteSpace(token))
		{
			await LogoutAsync();
			return;
		}

		await _storage.SetItemAsync(JwtKey, token);

		var (username, roles) = ParseJwt(token);
		if (!string.IsNullOrWhiteSpace(usernameHint) && string.IsNullOrWhiteSpace(username))
			username = usernameHint;

		if (!string.IsNullOrWhiteSpace(username))
			await _storage.SetItemAsync(EmailKey, username);

		_state.Set(token, username, roles);
		AuthStateChanged?.Invoke();
		Console.WriteLine($"[AUTH] Token saved. User={username}, Roles=[{string.Join(",", roles)}]");
	}

	public async Task LogoutAsync()
	{
		await _storage.RemoveItemAsync(JwtKey);
		await _storage.RemoveItemAsync(EmailKey);
		_state.Clear();
		AuthStateChanged?.Invoke();
		Console.WriteLine("[AUTH] Logged out.");
	}

	// Folosită de Login.razor: returnează (ok, errors)
	public async Task<(bool ok, List<string> errors)> LoginAsync(string email, string password)
	{
		try
		{
			var payload = new { email, password };
			using var resp = await _http.PostAsJsonAsync("/api/auth/login", payload);

			if (!resp.IsSuccessStatusCode)
			{
				var errs = await ExtractErrors(resp);
				return (false, errs);
			}

			var text = await resp.Content.ReadAsStringAsync();
			var (token, username, roles) = ParseLoginOrRegisterResponse(text);

			if (string.IsNullOrWhiteSpace(token))
				return (false, new List<string> { "Login response did not include a token." });

			await SaveTokenAsync(token, string.IsNullOrWhiteSpace(username) ? email : username);
			return (true, new());
		}
		catch (Exception ex)
		{
			return (false, new List<string> { ex.Message });
		}
	}

	// Folosită de Register.razor: returnează (ok, errors)
	public async Task<(bool ok, List<string> errors)> RegisterAsync(string email, string password, string fullName)
	{
		try
		{
			var payload = new { email, password, fullName };
			using var resp = await _http.PostAsJsonAsync("/api/auth/register", payload);

			if (!resp.IsSuccessStatusCode)
			{
				var errs = await ExtractErrors(resp);
				return (false, errs);
			}

			var text = await resp.Content.ReadAsStringAsync();
			var (token, username, roles) = ParseLoginOrRegisterResponse(text);

			// Unele API-uri nu întorc token la register -> caz în care doar anunțăm succes fără token
			if (!string.IsNullOrWhiteSpace(token))
				await SaveTokenAsync(token, string.IsNullOrWhiteSpace(username) ? email : username);

			return (true, new());
		}
		catch (Exception ex)
		{
			return (false, new List<string> { ex.Message });
		}
	}

	private async Task<string?> TryReadTokenAsync()
	{
		var t = await _storage.GetItemAsync(JwtKey);
		if (!string.IsNullOrWhiteSpace(t)) return t;

		foreach (var k in FallbackJwtKeys)
		{
			t = await _storage.GetItemAsync(k);
			if (!string.IsNullOrWhiteSpace(t))
			{
				await _storage.SetItemAsync(JwtKey, t);
				return t;
			}
		}
		return null;
	}

	// În multe backend-uri tokenul vine sub diverse chei: token, accessToken, jwt, data.token etc.
	private static (string token, string username, List<string> roles) ParseLoginOrRegisterResponse(string json)
	{
		try
		{
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			string? token = TryFindString(root, ["token", "accessToken", "jwt", "bearerToken"]);
			if (string.IsNullOrWhiteSpace(token) && root.TryGetProperty("data", out var data))
			{
				token = TryFindString(data, ["token", "accessToken", "jwt", "bearerToken"]);
			}

			string username = TryFindString(root, ["email", "username", "name", "preferred_username", "unique_name"]) ?? "";
			if (string.IsNullOrWhiteSpace(username) && root.TryGetProperty("data", out var data2))
			{
				username = TryFindString(data2, ["email", "username", "name", "preferred_username", "unique_name"]) ?? "";
			}

			// Unele răspunsuri pot include roluri; dacă nu, le vom extrage din JWT
			var roles = new List<string>();
			if (root.TryGetProperty("roles", out var rv))
			{
				if (rv.ValueKind == JsonValueKind.Array)
				{
					foreach (var x in rv.EnumerateArray())
						if (x.ValueKind == JsonValueKind.String) roles.Add(x.GetString()!);
				}
				else if (rv.ValueKind == JsonValueKind.String)
				{
					var s = rv.GetString();
					if (!string.IsNullOrWhiteSpace(s))
						roles.AddRange(s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
				}
			}

			return (token ?? "", username, roles);
		}
		catch
		{
			return ("", "", new());
		}
	}

	private static string? TryFindString(JsonElement root, IEnumerable<string> keys)
	{
		foreach (var k in keys)
		{
			if (root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
				return v.GetString();
		}
		return null;
	}

	private static async Task<List<string>> ExtractErrors(HttpResponseMessage resp)
	{
		try
		{
			var text = await resp.Content.ReadAsStringAsync();
			using var doc = JsonDocument.Parse(text);
			var root = doc.RootElement;

			var list = new List<string>();

			// Common formats: { errors: ["a","b"] } sau { message: "..." } sau { error: "..." }
			if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array)
			{
				foreach (var e in errs.EnumerateArray())
					if (e.ValueKind == JsonValueKind.String)
						list.Add(e.GetString()!);
			}
			else if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
			{
				list.Add(m.GetString()!);
			}
			else if (root.TryGetProperty("error", out var e2) && e2.ValueKind == JsonValueKind.String)
			{
				list.Add(e2.GetString()!);
			}

			if (list.Count == 0)
				list.Add($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");

			return list;
		}
		catch
		{
			return new List<string> { $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}" };
		}
	}

	// Decode JWT fără validare, doar pentru a extrage email/roles
	private static (string username, List<string> roles) ParseJwt(string token)
	{
		try
		{
			var parts = token.Split('.');
			if (parts.Length < 2) return ("", new());

			static string FixBase64(string s)
			{
				s = s.Replace('-', '+').Replace('_', '/');
				return s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
			}

			var payloadJson = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(FixBase64(parts[1])));
			using var doc = JsonDocument.Parse(payloadJson);
			var root = doc.RootElement;

			// username/email
			var username = "";
			string[] emailKeys = { "email", "unique_name", "name", "preferred_username", "sub" };
			foreach (var k in emailKeys)
			{
				if (root.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
				{
					username = v.GetString() ?? "";
					if (!string.IsNullOrWhiteSpace(username)) break;
				}
			}

			// roles – suport array sau string și diverse chei
			var roles = new List<string>();
			string[] roleKeys = {
				"role", "roles",
				"http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
			};
			foreach (var rk in roleKeys)
			{
				if (!root.TryGetProperty(rk, out var rv)) continue;

				if (rv.ValueKind == JsonValueKind.Array)
				{
					foreach (var item in rv.EnumerateArray())
						if (item.ValueKind == JsonValueKind.String)
							roles.Add(item.GetString()!);
				}
				else if (rv.ValueKind == JsonValueKind.String)
				{
					var s = rv.GetString();
					if (!string.IsNullOrWhiteSpace(s))
					{
						foreach (var p in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
							roles.Add(p);
					}
				}
			}

			roles = roles.Select(r => r.Trim()).Where(r => r.Length > 0)
						 .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			return (username, roles);
		}
		catch
		{
			return ("", new());
		}
	}
}