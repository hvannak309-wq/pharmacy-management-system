-- PharmacyMS schema (MS SQL Server)

CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL
);

CREATE TABLE Suppliers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(300) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Customers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(300) NULL
);

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Role INT NOT NULL, -- UserRole enum: 0=Admin, 1=Pharmacist, 2=Cashier
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Barcode NVARCHAR(100) NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CostPrice DECIMAL(18,2) NOT NULL,
    ReorderLevel INT NOT NULL DEFAULT 0,
    IsPrescriptionRequired BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

CREATE TABLE Batches (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProductId INT NOT NULL,
    BatchNumber NVARCHAR(50) NOT NULL,
    ExpiryDate DATE NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    PurchasePrice DECIMAL(18,2) NOT NULL,
    IsDisposed BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Batches_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE Purchases (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SupplierId INT NOT NULL,
    UserId INT NOT NULL,
    PurchaseDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status INT NOT NULL, -- PurchaseStatus enum
    CONSTRAINT FK_Purchases_Suppliers FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id),
    CONSTRAINT FK_Purchases_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE Purchase_Details (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseId INT NOT NULL,
    ProductId INT NOT NULL,
    BatchId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_PurchaseDetails_Purchases FOREIGN KEY (PurchaseId) REFERENCES Purchases(Id) ON DELETE CASCADE,
    CONSTRAINT FK_PurchaseDetails_Products FOREIGN KEY (ProductId) REFERENCES Products(Id),
    CONSTRAINT FK_PurchaseDetails_Batches FOREIGN KEY (BatchId) REFERENCES Batches(Id)
);

CREATE TABLE Sales (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NULL,
    UserId INT NOT NULL,
    SaleDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod INT NOT NULL, -- PaymentMethod enum
    CONSTRAINT FK_Sales_Customers FOREIGN KEY (CustomerId) REFERENCES Customers(Id),
    CONSTRAINT FK_Sales_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE Sale_Details (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SaleId INT NOT NULL,
    ProductId INT NOT NULL,
    BatchId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_SaleDetails_Sales FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SaleDetails_Products FOREIGN KEY (ProductId) REFERENCES Products(Id),
    CONSTRAINT FK_SaleDetails_Batches FOREIGN KEY (BatchId) REFERENCES Batches(Id)
);

CREATE INDEX IX_Products_Barcode ON Products(Barcode);
CREATE INDEX IX_Batches_Product_Expiry ON Batches(ProductId, ExpiryDate);
