using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace CampusEats.Frontend.Services;

public class LocalStorageService
{
    private readonly IJSRuntime _js;
    public LocalStorageService(IJSRuntime js) => _js = js;

    public ValueTask SetItemAsync(string key, string value)
        => _js.InvokeVoidAsync("localStorage.setItem", key, value);

    public async ValueTask<string?> GetItemAsync(string key)
        => await _js.InvokeAsync<string?>("localStorage.getItem", key);

    public ValueTask RemoveItemAsync(string key)
        => _js.InvokeVoidAsync("localStorage.removeItem", key);
}