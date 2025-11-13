namespace CampusEats.Frontend.Services;

public class DebugAuthHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var hasAuth = request.Headers.Authorization != null;
        var prefix = hasAuth ? request.Headers.Authorization!.Parameter?.Substring(0, Math.Min(12, request.Headers.Authorization!.Parameter!.Length)) : "(none)";
        Console.WriteLine($"[HTTP DEBUG] {request.Method} {request.RequestUri} | Auth? {hasAuth} | Token(0..12)={prefix}");
        return base.SendAsync(request, cancellationToken);
    }
}