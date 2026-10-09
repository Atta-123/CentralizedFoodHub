# FoodHub — SQL Server + ASP.NET Core Web API + Angular

## 1. Extract

Extract the complete ZIP into a new local folder, for example `C:\Projects\FoodHub`. Open the inner folder containing `FoodHub.sln`.

## 2. Requirements

- .NET **10 SDK**, compatible Node.js (20.19+, 22.12+ or 24.x).
- Installed and running **SQL Server** instance. SSMS alone is a client, not the database engine.
- Run `01-check-setup.cmd` to check SDK and Node.

.NET 10 target ko VS2022 mein manually 8/9 mein change na karein; CLI scripts use karein ya compatible newer Visual Studio.

## 3. SQL Server connection

Open `src/FoodHub.Api/appsettings.json`.

Default uses SQL Server Express with Windows Authentication:

```json
"ConnectionStrings": {
  "SqlServer": "Server=.\\SQLEXPRESS;Database=FoodHubSqlServer;Trusted_Connection=True;TrustServerCertificate=True"
}
```

SSMS mein jis **Server name** se connect hotay hain, wahi yahan use karein:

| SSMS instance          | JSON Server value                 |
| ---------------------- | --------------------------------- |
| Default local instance | `Server=.;`                       |
| SQL Express            | `Server=.\\SQLEXPRESS;`           |
| LocalDB installed      | `Server=(localdb)\\MSSQLLocalDB;` |
| Named instance         | `Server=YOUR-PC\\YOUR-INSTANCE;`  |

JSON mein backslash double hota hai. Default new database name `FoodHubSqlServer` rakhein. Existing SQLite file automatically SQL Server mein import nahi hoti. Purani SQL Server database created with EnsureCreated ho to new database name use karein; existing tables par initial migration blindly apply na karein.

SQL Authentication ho to password source code mein commit na karein. Command Prompt mein environment variable use kar sakte hain:

```bat
set "ConnectionStrings__SqlServer=Server=localhost;Database=FoodHubSqlServer;User Id=foodhub_user;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=True"
```

`TrustServerCertificate=True` local development ke liye hai. Production mein trusted certificate aur proper permissions configure karein. Login ko local development database create/migrate karne ki permission chahiye.

## 4. Backend start

Double-click `02-start-backend.cmd`. Ye restore/build karke Development mein migrations apply aur demo data seed karti hai.

Manual Command Prompt commands:

```bat
cd /d C:\Projects\FoodHub

dotnet restore src\FoodHub.Api\FoodHub.Api.csproj

dotnet build src\FoodHub.Api\FoodHub.Api.csproj --no-restore

set ASPNETCORE_ENVIRONMENT=Development

dotnet run --no-build --no-launch-profile --project src\FoodHub.Api\FoodHub.Api.csproj --urls http://localhost:5180
```

Check: http://localhost:5180/health then http://localhost:5180/swagger.

In SSMS refresh Databases → FoodHubSqlServer → Tables. Tables: Users, Restaurants, MenuItems, Orders, OrderItems, OrderHistories, \_\_EFMigrationsHistory.

## 5. Angular

Run `03-start-frontend.cmd` in another window. Or:

```bat
cd /d C:\Projects\FoodHub\frontend
npm ci
npm start
```

Open http://localhost:4300. Both windows open rakhein.

## 6. Login + order flow

All demo passwords: `Demo@12345`.

| Role         | Email                  |
| ------------ | ---------------------- |
| Customer     | customer@foodhub.local |
| Admin        | admin@foodhub.local    |
| ABC owner    | abc@foodhub.local      |
| Pizza owner  | pizza@foodhub.local    |
| Khyber owner | khyber@foodhub.local   |

Customer → manual latitude `34.015`, longitude `71.525`, radius `5` → Find nearby restaurants → ABC menu → add two Classic Beef Burgers → address/phone → Place order.

ABC owner → Incoming orders → Accepted → Preparing → Ready For Pickup.

Admin → All orders → Confirm delivered → Overview. Rs.1000 order at20% commission: platform Rs.200, restaurant Rs.800. These are stored accounting amounts, not bank payouts.

Admin → Restaurants & menu → Add restaurant with owner credentials, coordinates, radius and commission. Owner can manage its own menu only.

## 7. EF Core migrations

InitialCreate migration is **already included**. Do not generate InitialCreate again.

Tool restore and apply explicitly (optional; Development startup already applies):

```bat
cd /d C:\Projects\FoodHub
dotnet tool restore
dotnet ef database update --project src\FoodHub.Infrastructure --startup-project src\FoodHub.Api
```

Design-time factory reads `ConnectionStrings__SqlServer`, otherwise uses the packaged SQL Express default. If you changed appsettings to another instance, set the same environment variable before running EF commands.

After changing a model:

```bat
dotnet ef migrations add YourChangeName --project src\FoodHub.Infrastructure --startup-project src\FoodHub.Api
dotnet ef database update --project src\FoodHub.Infrastructure --startup-project src\FoodHub.Api
```

Production: apply reviewed migration scripts before starting API; no automatic production schema change or demo seeding.

## 8. Common errors

| Error                                        | Action                                                                                                      |
| -------------------------------------------- | ----------------------------------------------------------------------------------------------------------- |
| Server not found / error26 / network-related | Match SSMS instance name; start SQL Server service; check instance TCP/port if remote.                      |
| Login failed                                 | Match Windows/SQL authentication; check login permissions.                                                  |
| CREATE DATABASE permission denied            | Create a new empty database using an authorized account, then grant the application local migration rights. |
| Certificate chain not trusted                | Local test connection uses TrustServerCertificate=True; production needs trusted certificate.               |
| Object already exists                        | Use a new database name; old EnsureCreated schema is not migration-tracked. Do not delete old data.         |
| MSB1003                                      | Open folder with FoodHub.sln; use explicit --project path.                                                  |
| NETSDK1045                                   | Install .NET10 SDK, not just Runtime.                                                                       |
| dotnet-ef not found                          | Run dotnet tool restore from root.                                                                          |
| Angular cannot connect                       | Backend health must work first, port5180 and frontend4300.                                                  |

`04-diagnostics.cmd` generates setup-diagnostics.txt. Exact backend terminal error share karein if issue persists.

## Models and architecture

`src/FoodHub.Domain`: separate User, Restaurant, MenuItem, Order, OrderItem, OrderHistory, OrderStatus, Roles, OrderRules, Geo files.

`src/FoodHub.Application`: request/response DTOs and interfaces.

`src/FoodHub.Infrastructure`: FoodDbContext, explicit relationships/constraints, migrations, services, SQL Server nearby query.

`src/FoodHub.Api/Controllers`: attribute-routed MVC API controllers. **Angular frontend uses JSON APIs; Razor MVC views are not included.** TS/HTML/SCSS are separate.

Money is stored in integer minor units (`PriceCents` = paisa): Rs500 =50000. API/UI uses decimal rupees. Commission uses decimal(5,2), snapshots preserve historical values; Guid Version handles optimistic concurrency.
