# Plan: PharmacyMS — Implementation Order

Build order is dependency-driven: models first, then repositories, then services, then forms. Single-project OOP layout in `pharmacy/` (see `docs/SPEC.md`).

## Phase 0 — Solution Skeleton

- [ ] Task: Create solution with `pharmacy` (WinForms, .NET 8) + `tests/PharmacyMS.Tests` (xUnit)
  - Acceptance: `dotnet build` succeeds; folders exist as per spec (Forms, Models, Interfaces, Repositories, Services, Data, DTOs, Validators, Enums, Helpers, UserControls)
  - Verify: `dotnet build PharmacyMS.sln`
  - Files: `PharmacyMS.sln`, `pharmacy/pharmacy.csproj`, `tests/PharmacyMS.Tests/PharmacyMS.Tests.csproj`

## Phase 1 — Models & Enums (Encapsulation)

- [ ] Task: Create all 10 model classes in `Models/` (one file per table) + enums in `Enums/`
  - Acceptance: Properties are encapsulated; navigation properties set; `Result<T>` in `DTOs/`
  - Verify: `dotnet build`
  - Files: `Models/*.cs` (Category, Product, Supplier, Customer, User, Purchase, PurchaseDetail, Sale, SaleDetail, Batch), `Enums/*.cs`, `DTOs/Result.cs`

## Phase 2 — Data Layer

- [ ] Task: `AppDbContext` in `Data/` with all FKs, relationships, and cascade rules
  - Acceptance: Model builds; details cascade with parent, batches restricted
  - Verify: `dotnet ef migrations add InitialCreate -p pharmacy -s pharmacy`
  - Files: `Data/AppDbContext.cs`
- [ ] Task: Initial migration + `DbSeeder` in `Data/Seed/` (default admin, sample category/products)
  - Acceptance: `dotnet ef database update` creates schema; seeder runs on first launch
  - Verify: Inspect DB tables; login with seeded admin
  - Files: `Data/Migrations/*`, `Data/Seed/DbSeeder.cs`

## Phase 3 — Repositories (Inheritance + Polymorphism)

- [ ] Task: `IRepository<T>` in `Interfaces/`, `Repository<T>` base class in `Repositories/`, per-entity repositories
  - Acceptance: Generic CRUD works; per-entity repos add specialized queries (e.g., `ProductRepository.GetByBarcode`)
  - Verify: `dotnet test` (repository tests with InMemory provider)
  - Files: `Interfaces/IRepository.cs`, `Repositories/Repository.cs`, `Repositories/ProductRepository.cs`, etc.

## Phase 4 — Core Services (most tested code)

- [ ] Task: `AuthService` (login, BCrypt hash, role check)
- [ ] Task: CRUD services for Categories, Suppliers, Customers, Products, Users (list/search/add/edit/deactivate)
- [ ] Task: `BatchService` (expiry status calculation, disposal, low-stock check)
- [ ] Task: `PurchaseService` (create purchase → create batches → stock in)
- [ ] Task: `SaleService` (cart checkout → FEFO allocation → stock out → totals/discount)
  - Acceptance (all): Unit tests green; FEFO picks earliest expiry; insufficient stock rejected
  - Verify: `dotnet test`
  - Files: `Services/*.cs`, `Interfaces/ISaleService.cs` etc., `DTOs/*`, `Validators/*`

## Phase 5 — UI Shell

- [ ] Task: `BaseForm` in `Forms/` (shared styling + keyboard shortcuts — inheritance demo)
- [ ] Task: `FrmLogin` + `FrmMain` (menu strip, role-based visibility, child-form hosting)
  - Acceptance: Admin sees all menus; Cashier sees only POS/Sales
  - Verify: Manual run + login with each seeded role
  - Files: `Forms/BaseForm.cs`, `Forms/FrmLogin.cs`, `Forms/FrmMain.cs`, `Helpers/FormLoader.cs`

## Phase 6 — CRUD Screens (parallelizable)

- [ ] Task: Categories, Suppliers, Customers screens (list + edit dialogs)
- [ ] Task: Products screen (with category combo, barcode field, reorder level)
- [ ] Task: Users screen (admin only, password hashing via AuthService)
  - Acceptance: Full add/edit/search flows work; validation errors shown in UI
  - Verify: Manual walkthrough per screen
  - Files: `Forms/<Entity>/*`

## Phase 7 — Transaction Screens

- [ ] Task: Purchase entry form (multi-line grid → save → batches created)
- [ ] Task: POS form (product search by name/barcode, cart grid, payment, receipt)
  - Acceptance: End-to-end purchase→stock→sale→stock-out works
  - Verify: Manual scenario + service tests still green
  - Files: `Forms/Purchases/*`, `Forms/Sales/*`, `UserControls/UcCart.cs`

## Phase 8 — Batches & Dashboard

- [ ] Task: Batch list + expiry alerts screen (30/60/90/expired color coding)
- [ ] Task: Dashboard cards (today's sales, low stock count, expiring soon count)
  - Files: `Forms/Batches/*`, `UserControls/UcDashboardCard.cs`

## Phase 9 — Reports

- [ ] Task: Sales report (date range, by user), Stock report, Profit report, Expiry report
  - Acceptance: Numbers match manual calculations from test data
  - Files: `Forms/Reports/*`

## Phase 10 — Polish & Ship

- [ ] Task: Grid styling, keyboard shortcuts (F2=new sale, F4=search), error handling wrapper
- [ ] Task: Final regression: build, full test run, backup script for DB

## Risks & Mitigations

| Risk                               | Mitigation                                           |
| ---------------------------------- | ---------------------------------------------------- |
| FEFO allocation bugs corrupt stock | Heavy unit tests on SaleService before any UI        |
| WinForms UI logic creep            | Code-behind only wires events; all logic in Services |
| Migrations drift                   | Never edit applied migrations; always add new ones   |
| Password security                  | BCrypt from day one, no plaintext anywhere           |

