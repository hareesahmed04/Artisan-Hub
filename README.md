# ArtisanHub – Local Artisan / Maker Marketplace

DBMS project · BS Computer Science · Iqra University
Group: **Harees Ahmed**, **Zeeshan Wazir**

ArtisanHub is a web marketplace where verified local artists sell handmade goods and take custom art orders. It is built with **ASP.NET Core MVC (C#)** and **Microsoft SQL Server**, on a 15-table database normalized to 3NF.

---

## 1. What you need

| Tool | Notes |
| --- | --- |
| **.NET 8 SDK** | Download the .NET 8 **SDK** (x64 installer for Windows) from https://dotnet.microsoft.com/download/dotnet/8.0. The file `global.json` makes the project build with the .NET 8 SDK even if newer SDKs are installed too (see "Your Windows doesn't fully support CET" below). Check with `dotnet --list-sdks`. |
| **SQL Server** | SQL Server Express, Developer edition or LocalDB. SSMS is optional (useful to look at the tables). |
| **VS Code** | With the **C# Dev Kit** extension (Microsoft). Visual Studio 2022/2026 also works: open `ArtisanHub.sln`. |

## 2. Run it (VS Code)

1. Open the **Artisan Hub** folder in VS Code (File > Open Folder).
2. Open a terminal (Terminal > New Terminal) and run:

   ```
   cd ArtisanHub
   dotnet run
   ```

   Optional: rename the folder `vscode-settings` to `.vscode`; after that you can just press **F5** to build and run.
3. Open **http://localhost:5080** in your browser.

On the **first run** the app:

- finds your SQL Server automatically (it tries `.\SQLEXPRESS`, then `localhost`, then `(localdb)\MSSQLLocalDB`),
- creates the database **ArtisanHubDB**,
- runs `Database/ArtisanHub_Database.sql` (15 tables, 5 views, 1 trigger and sample data).

The terminal prints which SQL Server it connected to. The first `dotnet run` also downloads the NuGet packages, so it needs internet once.

### If it says it cannot connect to SQL Server

1. Make sure the SQL Server service is running (Windows: press Win+R, type `services.msc`, start **SQL Server (SQLEXPRESS)**).
2. Open `ArtisanHub/appsettings.json` and put the **server name you use in SSMS** in `DefaultConnection`, for example:

   ```json
   "DefaultConnection": "Server=DESKTOP-OGQJ58B\\SQLEXPRESS;Database=ArtisanHubDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
   ```

   (In JSON a backslash is written twice: `\\`.)

### If the build says "Your Windows doesn't fully support CET"

The C# compiler in .NET 9 and newer SDKs turns on a Windows security feature (CET) that older, unpatched Windows builds can't handle, so the compiler crashes. Two ways to fix it:

1. **Use the .NET 8 SDK (quick).** Install the .NET 8 SDK from the link above and run `dotnet run` again. `global.json` already tells `dotnet` to use it.
2. **Update Windows (long-term).** Install all Windows updates (Settings > Windows Update). After that, newer SDKs work too, and you can delete `global.json` if you want to build with them.

### If it says "A compatible .NET SDK was not found"

`global.json` asks for the .NET 8 SDK and it isn't installed yet. Install it from https://dotnet.microsoft.com/download/dotnet/8.0 and run the command again.

## 3. Demo logins

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@artisanhub.pk` | `Admin@123` |
| Artist (verified) | `sana@artisanhub.pk` | `Pass@123` |
| Artist (waiting for verification) | `kamran@artisanhub.pk` | `Pass@123` |
| Customer | `ayesha@example.com` | `Pass@123` |

Other artists: `rashid`, `hina`, `imran`, `maryam`, `ali` @artisanhub.pk. Other customers: `bilal`, `fatima`, `usman` @example.com. Same password `Pass@123`.

## 4. Features (by user type)

**Customer**
- Browse and search products by category, artist's city, price range and "takes custom orders"; sort by newest, popular, rating or price.
- Product page with photo gallery, stock, artist info, star ratings and reviews.
- Cart grouped by artist (each artist ships their own parcel, Rs. 250 delivery per artist).
- Checkout with saved or new address and payment method (Cash on delivery, Card, JazzCash, EasyPaisa – online methods are demo payments, nothing is charged).
- My orders: order tracker, tracking number per artist parcel, cancel before shipping (stock is returned), review items after delivery.
- Custom requests: describe a piece, the artist sends a quote, accept or decline it.
- Profile: edit details, change password, manage delivery addresses.

**Artist (seller panel)**
- Dashboard: sales this month, all-time sales, parcels to ship, new requests, low-stock alerts, recent orders.
- Products: add, edit, hide/show, upload photos (or add by URL), choose the main photo.
- Orders & shipping: courier, tracking number and status (Preparing → Shipped → Delivered) for their part of each order.
- Custom requests: send or update a quote, decline, mark completed.
- Sales report: 6-month chart, best-selling products (uses `vw_SalesByArtist`).
- Store profile: name, city, bio.
- New artists can set up their store immediately; products appear in the shop after the admin verifies them.

**Admin**
- Dashboard with revenue, orders, artists, customers, artists waiting for verification and sales by category.
- Verify / unverify artists, add / edit / delete categories, activate / deactivate users, see all orders.
- Reports: sales by artist, category and city, straight from the SQL views.

## 5. Where the DBMS work is

| Proposal item | File |
| --- | --- |
| 15 tables, PK/FK/UNIQUE/CHECK/DEFAULT constraints | `Database/ArtisanHub_Database.sql` |
| Views: `vw_OrderTotals`, `vw_SalesByArtist`, `vw_SalesByCategory`, `vw_SalesByCity`, `vw_ProductRatings` | `Database/ArtisanHub_Database.sql` |
| Trigger `trg_OrderItems_ReduceStock` (reduces stock, blocks overselling) | `Database/ArtisanHub_Database.sql` |
| Sample data (8 cities, 6 categories, 12 users, 7 artists, 26 products, 12 orders, reviews, requests) | `Database/ArtisanHub_Database.sql` |
| 13 report queries for the lab (JOIN, GROUP BY, aggregates) | `Database/ArtisanHub_Reports.sql` |
| C# classes for the 15 tables | `ArtisanHub/Models/` |
| Table and view mapping (EF Core) | `ArtisanHub/Data/ArtisanHubContext.cs` |
| Order placement in one transaction (Orders → OrderItems → Payments → Shipments) | `ArtisanHub/Services/OrderService.cs` |

Order totals are **never stored**; they are calculated by `vw_OrderTotals`, which keeps the Orders table in 3NF.

To reset the database to the sample data, run `Database/ArtisanHub_Database.sql` in SSMS (it drops and recreates every table).

## 6. Suggested demo for the presentation

1. Log in as **ayesha@example.com** → search "vase" → open a product → add to cart → checkout with JazzCash → see the order tracker.
2. Log in as **sana@artisanhub.pk** → Orders & shipping → enter courier TCS and a tracking number → mark Shipped, then Delivered.
3. Back as Ayesha → the order shows Delivered → write a review.
4. As Ayesha → request a custom piece from Qalam Art House. As **maryam@artisanhub.pk** → send a quote. As Ayesha → accept it.
5. Log in as **admin@artisanhub.pk** → verify **Truck Art Tales** → its products appear in the shop → open Reports.
6. In SSMS, run `ArtisanHub_Reports.sql` to show the same numbers with SQL.

## 7. Publishing to MonsterASP.NET

1. In the MonsterASP.NET control panel, create a website and an **MSSQL database**, and copy its connection string.
2. Put that connection string in `ArtisanHub/appsettings.json` → `DefaultConnection`.
3. Publish: `dotnet publish ArtisanHub -c Release -o publish` and upload the `publish` folder (Web Deploy or FTP), or follow MonsterASP's guide: https://help.monsterasp.net/books/deploy/page/how-to-deploy-net-core-web-application-with-mssql-using-visual-studio
4. On first start the app builds the tables and sample data inside the hosted database automatically.

## 8. Project structure

```
Artisan Hub/
├── ArtisanHub.sln
├── global.json                   ← makes dotnet use the .NET 8 SDK
├── Database/
│   ├── ArtisanHub_Database.sql   ← schema, views, trigger, sample data
│   └── ArtisanHub_Reports.sql    ← report queries for SSMS
└── ArtisanHub/                   ← ASP.NET Core MVC project
    ├── Controllers/              ← Home, Products, Stores, Account, Cart, Checkout,
    │                               Orders, Reviews, Requests, Seller, Admin
    ├── Data/                     ← DbContext, SQL Server finder, database initializer
    ├── Helpers/                  ← formatting and user-claim helpers
    ├── Models/                   ← 15 table classes + view classes + status values
    ├── Services/                 ← order logic, image uploads, catalog queries
    ├── ViewModels/               ← form and page models
    ├── Views/                    ← Razor pages (shop, seller panel, admin panel)
    └── wwwroot/                  ← CSS theme, JavaScript, logo and product artwork
```

The ArtisanHub logo and all product illustrations are original artwork made for this project.
