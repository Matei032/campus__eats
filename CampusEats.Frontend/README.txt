CampusEats Frontend (Blazor WebAssembly) – Quick setup

1) Copy the folders (Pages, Shared, Services, wwwroot) into your Blazor WASM project.
   - If files exist, replace them (make a backup if needed).

2) Program.cs – register services and point HttpClient to your BACKEND URL:
   Replace the default HttpClient registration with:
       builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(CampusEats.Frontend.Services.ApiClient.BackendBaseUrl) });
   Add:
       builder.Services.AddScoped<CampusEats.Frontend.Services.ApiClient>();
       builder.Services.AddScoped<CampusEats.Frontend.Services.CartService>();

3) Set the backend URL in Services/ApiClient.cs:
       public static string BackendBaseUrl = "https://localhost:5001/";
   Change it to your backend address/port.

4) Ensure wwwroot/index.html contains Bootstrap links (already included in this package).

5) Routes available:
   /          Home
   /menu      Menu listing grouped by category (add to cart)
   /cart      Cart + Place order
   /orders    Orders for a user (enter UserId GUID)
   /order/{id} Order details + cancel
   /kitchen   Pending orders + set status (Preparing/Ready/Completed)
   /inventory Daily inventory report (with date filter)
   /products-admin  Admin list
   /edit-product    Create
   /edit-product/{id} Edit

6) Build & run the WASM app. Make sure backend is running and CORS allows the WASM origin.