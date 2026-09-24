-- PharmacyMS schema (MS SQL Server)
-- Table/column names: tbl* prefix; PK/FK {Entity}ID; Customer uses CusID/CusName.

CREATE TABLE tblCategory (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL
);

CREATE TABLE tblSupplier (
    SupplierID INT IDENTITY(1,1) PRIMARY KEY,
    SupplierName NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(300) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE tblCustomer (
    CusID INT IDENTITY(1,1) PRIMARY KEY,
    CusName NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(100) NULL,
    Address NVARCHAR(300) NULL
);

CREATE TABLE tblUser (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(200) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Role INT NOT NULL, -- UserRole enum: 0=Admin, 1=Pharmacist, 2=Cashier
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE tblProduct (
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID INT NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Barcode NVARCHAR(100) NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CostPrice DECIMAL(18,2) NOT NULL,
    ReorderLevel INT NOT NULL DEFAULT 0,
    IsPrescriptionRequired BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_tblProduct_tblCategory FOREIGN KEY (CategoryID) REFERENCES tblCategory(CategoryID)
);

CREATE TABLE tblBatch (
    BatchID INT IDENTITY(1,1) PRIMARY KEY,
    ProductID INT NOT NULL,
    BatchNumber NVARCHAR(50) NOT NULL,
    ExpiryDate DATE NOT NULL,
    Quantity INT NOT NULL DEFAULT 0,
    PurchasePrice DECIMAL(18,2) NOT NULL,
    IsDisposed BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_tblBatch_tblProduct FOREIGN KEY (ProductID) REFERENCES tblProduct(ProductID)
);

CREATE TABLE tblPurchase (
    PurchaseID INT IDENTITY(1,1) PRIMARY KEY,
    SupplierID INT NOT NULL,
    UserID INT NOT NULL,
    PurchaseDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Status INT NOT NULL, -- PurchaseStatus enum
    CONSTRAINT FK_tblPurchase_tblSupplier FOREIGN KEY (SupplierID) REFERENCES tblSupplier(SupplierID),
    CONSTRAINT FK_tblPurchase_tblUser FOREIGN KEY (UserID) REFERENCES tblUser(UserID)
);

CREATE TABLE tblPurchaseDetail (
    PurchaseDetailID INT IDENTITY(1,1) PRIMARY KEY,
    PurchaseID INT NOT NULL,
    ProductID INT NOT NULL,
    BatchID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_tblPurchaseDetail_tblPurchase FOREIGN KEY (PurchaseID) REFERENCES tblPurchase(PurchaseID) ON DELETE CASCADE,
    CONSTRAINT FK_tblPurchaseDetail_tblProduct FOREIGN KEY (ProductID) REFERENCES tblProduct(ProductID),
    CONSTRAINT FK_tblPurchaseDetail_tblBatch FOREIGN KEY (BatchID) REFERENCES tblBatch(BatchID)
);

CREATE TABLE tblSale (
    SaleID INT IDENTITY(1,1) PRIMARY KEY,
    CusID INT NULL,
    UserID INT NOT NULL,
    SaleDate DATETIME NOT NULL DEFAULT GETDATE(),
    TotalAmount DECIMAL(18,2) NOT NULL,
    Discount DECIMAL(18,2) NOT NULL DEFAULT 0,
    PaidAmount DECIMAL(18,2) NOT NULL,
    PaymentMethod INT NOT NULL, -- PaymentMethod enum
    CONSTRAINT FK_tblSale_tblCustomer FOREIGN KEY (CusID) REFERENCES tblCustomer(CusID),
    CONSTRAINT FK_tblSale_tblUser FOREIGN KEY (UserID) REFERENCES tblUser(UserID)
);

CREATE TABLE tblSaleDetail (
    SaleDetailID INT IDENTITY(1,1) PRIMARY KEY,
    SaleID INT NOT NULL,
    ProductID INT NOT NULL,
    BatchID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Subtotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_tblSaleDetail_tblSale FOREIGN KEY (SaleID) REFERENCES tblSale(SaleID) ON DELETE CASCADE,
    CONSTRAINT FK_tblSaleDetail_tblProduct FOREIGN KEY (ProductID) REFERENCES tblProduct(ProductID),
    CONSTRAINT FK_tblSaleDetail_tblBatch FOREIGN KEY (BatchID) REFERENCES tblBatch(BatchID)
);

CREATE INDEX IX_tblProduct_Barcode ON tblProduct(Barcode);
CREATE INDEX IX_tblBatch_Product_Expiry ON tblBatch(ProductID, ExpiryDate);
