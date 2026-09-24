# Spec: Pharmacy Management System (PharmacyMS)

## Objective

A desktop Pharmacy Management System built with **C# WinForms** for a single pharmacy store. It manages drug inventory with **batch/expiry tracking (FEFO)**, purchases from suppliers, and point-of-sale to customers, with user accounts and role-based access.

**Primary users:** Pharmacy cashiers (sales), pharmacists (inventory, purchases), admin (users, reports).

**Success looks like:**

- Cashier can complete a sale in under 30 seconds (search product → add to cart → checkout).
- Stock is deducted **per batch, first-expired-first-out (FEFO)** automatically.
- Expired/near-expiry batches are flagged on the dashboard (30/60/90-day warnings).
- Admin can see profit reports (sales revenue vs. purchase cost per product).

## Tech Stack

| Layer          | Technology                                                                                   |
| -------------- | -------------------------------------------------------------------------------------------- |
| UI             | C# WinForms, .NET 8, Visual Studio 2022 — **single project `pharmacy`** (assignment scope)   |
| Business Logic | Service classes inside the same project (OOP-first)                                          |
| Data Access    | EF Core 8 (Code First), SQL Server (LocalDB for dev)                                         |
| Testing        | xUnit + EF Core InMemory provider                                                            |
| Reporting      | ReportViewer / RDLC (or export to Excel via ClosedXML)                                       |

## Commands

```
Build:        dotnet build PharmacyMS.sln
Run:          dotnet run --project pharmacy
Test:         dotnet test tests/PharmacyMS.Tests
Add migration: dotnet ef migrations add <Name> -p pharmacy -s pharmacy
Update DB:    dotnet ef database update -p pharmacy -s pharmacy
```

## Project Structure

**Single-project, OOP-first layout** — one WinForms project (`pharmacy`), folders organized by OOP role. Chosen for assignment readability: a grader opens one project and immediately sees Models, Interfaces, Repositories, Services, Forms.

```
pharmacy-management-system/
├── PharmacyMS.sln
├── docs/                        ← this spec, plan, ERD
├── pharmacy/                    ← the single WinForms project
│   ├── pharmacy.csproj
│   ├── Program.cs               ← entry point
│   ├── Forms/                   ← every window (standard .NET naming)
│   │   ├── BaseForm.cs          ← INHERITANCE: shared styling/shortcuts for all forms
│   │   ├── FrmLogin.cs
│   │   ├── FrmMain.cs
│   │   ├── Categories/          (FrmCategoryList, FrmCategoryEdit)
│   │   ├── Products/            (FrmProductList, FrmProductEdit)
│   │   ├── Suppliers/           (FrmSupplierList, FrmSupplierEdit)
│   │   ├── Customers/           (FrmCustomerList, FrmCustomerEdit)
│   │   ├── Users/               (FrmUserList, FrmUserEdit)
│   │   ├── Purchases/           (FrmPurchaseList, FrmPurchaseNew, FrmPurchaseView)
│   │   ├── Sales/               (FrmPos, FrmSaleList, FrmSaleView)
│   │   ├── Batches/             (FrmBatchList, FrmExpiryAlerts)
│   │   └── Reports/             (FrmSalesReport, FrmStockReport, FrmExpiryReport)
│   ├── UserControls/            (UcProductSearch, UcCart, UcDashboardCard)
│   ├── Models/                  ← ENCAPSULATION: the 10 entity classes, one per table
│   │   (Category, Product, Supplier, Customer, User,
│   │    Purchase, PurchaseDetail, Sale, SaleDetail, Batch)
│   ├── Interfaces/              ← ABSTRACTION: IRepository<T>, ISaleService, ...
│   ├── Repositories/            ← INHERITANCE + POLYMORPHISM: Repository<T> base + per-entity repos
│   ├── Services/                ← business logic (SaleService FEFO, AuthService, ...)
│   ├── Data/                    ← AppDbContext, Migrations/, Seed/ (DbSeeder)
│   ├── DTOs/                    ← objects passed between Forms and Services (incl. Result<T>)
│   ├── Validators/              (ProductValidator, SaleValidator, ...)
│   ├── Enums/                   (UserRole, PaymentMethod, PurchaseStatus)
│   └── Helpers/                 (FormLoader, GridStyler, ToastHelper)
└── tests/
    └── PharmacyMS.Tests/        (xUnit)
        ├── Services/            (SaleServiceTests, BatchServiceTests, ...)
        └── Repositories/        (repository tests with InMemory provider)
```

### OOP Concepts Map (assignment checklist)

| OOP Pillar        | Where it is demonstrated                                                                       |
| ----------------- | ---------------------------------------------------------------------------------------------- |
| **Encapsulation** | `Models/` — private fields + public properties; `Services/` hide all business rules from forms |
| **Inheritance**   | `BaseForm` → every form; `Repository<T>` → `ProductRepository`, `SaleRepository`, ...          |
| **Polymorphism**  | Forms/Services call repository and service methods through base types and interfaces           |
| **Abstraction**   | `Interfaces/` — forms depend on `ISaleService`, never on EF Core directly                      |

### Folder dependency rule (one-directional, keeps it easy to read)

```
Forms → Services → Repositories → Models / Data
Forms never call Repositories or EF Core directly.
```

## Database Design (10 tables)

| Table            | Key Columns                                                                               | Relationships                                 |
| ---------------- | ----------------------------------------------------------------------------------------- | --------------------------------------------- |
| Categories       | Id, Name, Description                                                                     | 1→N Products                                  |
| Products         | Id, CategoryId, Name, Barcode, UnitPrice, CostPrice, ReorderLevel, IsPrescriptionRequired | N→1 Category; 1→N Batches                     |
| Suppliers        | Id, Name, Phone, Email, Address, IsActive                                                 | 1→N Purchases                                 |
| Customers        | Id, Name, Phone, Email, Address                                                           | 1→N Sales                                     |
| Users            | Id, Username, PasswordHash, FullName, Role (enum), IsActive                               | 1→N Sales, 1→N Purchases                      |
| Purchases        | Id, SupplierId, UserId, PurchaseDate, TotalAmount, Status                                 | 1→N Purchase_Details                          |
| Purchase_Details | Id, PurchaseId, ProductId, BatchId, Quantity, UnitCost, Subtotal                          | N→1 Purchase, Product, Batch                  |
| Sales            | Id, CustomerId, UserId, SaleDate, TotalAmount, Discount, PaidAmount, PaymentMethod        | 1→N Sale_Details                              |
| Sale_Details     | Id, SaleId, ProductId, BatchId, Quantity, UnitPrice, Subtotal                             | N→1 Sale, Product, Batch                      |
| Batches          | Id, ProductId, BatchNumber, ExpiryDate, Quantity, PurchasePrice, IsDisposed               | N→1 Product; referenced by both detail tables |

**Core business rule (FEFO):** When a sale line is added, the SaleService allocates quantity across the product's batches ordered by `ExpiryDate ASC`, skipping expired and disposed batches. If total batch quantity is insufficient, the sale line is rejected.

**Derived stock:** `Product stock = SUM(Batches.Quantity)` — never store a redundant stock column.

## Code Style

```csharp
// PascalCase public members, camelCase locals, _camelCase private fields
// One entity per file, async EF calls all the way down
public class SaleService : ISaleService
{
    private readonly IUnitOfWork _unitOfWork;

    public SaleService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<Result<int>> CheckoutAsync(CheckoutDto dto)
    {
        // validate → allocate batches FEFO → save sale + details → deduct stock
    }
}
```

- Forms named `Frm`+Entity+`List|Edit|View`; controls `txt`, `lbl`, `dgv`, `cmb`, `btn` prefixes.
- Every form inherits `BaseForm` (shared styling + keyboard shortcuts) — the inheritance demo for the assignment.
- Services return `Result<T>` instead of throwing for expected failures.

## Testing Strategy

- **Unit tests (xUnit):** Business services with InMemory EF — FEFO allocation, stock deduction, insufficient-stock rejection, totals/discount math, expiry flagging.
- **Repository tests:** CRUD + queries against InMemory provider.
- **Manual checks:** Form navigation, barcode scanning input, report rendering.
- Coverage focus: SaleService and BatchService get the most tests (money + stock logic).

## Boundaries

- **Always:** Run `dotnet build` + `dotnet test` before committing; keep the dependency rule intact; validate inputs in DTOs/validators.
- **Ask first:** Schema changes to any of the 10 tables, adding NuGet packages, changing .NET version.
- **Never:** Store plaintext passwords (use BCrypt/PBKDF2), commit connection strings with real credentials, put business logic in Form code-behind.

## Success Criteria

1. All 10 tables exist with correct FKs via EF Core migrations.
2. Login works; role-based menu visibility (Admin/Pharmacist/Cashier).
3. POS screen: search by name/barcode → cart → FEFO stock deduction → receipt.
4. Purchase entry creates batches and increases stock.
5. Expiry dashboard shows 30/60/90-day and expired batch alerts.
6. Reports: sales by date range, stock levels, profit (sell − cost).
7. `dotnet build` and `dotnet test` pass with zero warnings-as-errors violations.

## Open Questions

1. Barcode scanner hardware — assume keyboard-wedge emulation (no SDK needed)?
2. Receipt printing — plain text to thermal printer via `PrintDocument`, or RDLC?
3. Drug-level extras needed later (dosage form, strength, prescription image)? — deferred to v2.
