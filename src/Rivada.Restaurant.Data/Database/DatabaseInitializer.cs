using System;
using Microsoft.Data.Sqlite;

namespace Rivada.Restaurant.Data.Database
{
    public class DatabaseInitializer
    {
        private readonly SqliteDatabaseHelper _databaseHelper;

        public DatabaseInitializer(SqliteDatabaseHelper databaseHelper)
        {
            _databaseHelper = databaseHelper ?? throw new ArgumentNullException(nameof(databaseHelper));
        }

        public void Initialize()
        {
            using var connection = _databaseHelper.CreateConnection();
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Roles (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    RoleId INTEGER NOT NULL,
    Username TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    FullName TEXT,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS RestaurantTables (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Area TEXT,
    Status INTEGER NOT NULL DEFAULT 0 CHECK (Status IN (0,1)),
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Categories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Products (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CategoryId INTEGER NOT NULL,
    Name TEXT NOT NULL,
    Price REAL NOT NULL CHECK (Price >= 0),
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0,1)),
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON UPDATE CASCADE ON DELETE RESTRICT,
    UNIQUE (CategoryId, Name)
);

CREATE TABLE IF NOT EXISTS Bills (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    TableId INTEGER NOT NULL,
    OpenedByUserId INTEGER NOT NULL,
    ClosedByUserId INTEGER,
    Status INTEGER NOT NULL DEFAULT 0 CHECK (Status IN (0,1)),
    TotalAmount REAL NOT NULL DEFAULT 0 CHECK (TotalAmount >= 0),
    OpenedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ClosedAt TEXT,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (TableId) REFERENCES RestaurantTables(Id) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (OpenedByUserId) REFERENCES Users(Id) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (ClosedByUserId) REFERENCES Users(Id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS BillItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BillId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    ProductNameSnapshot TEXT NOT NULL,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    UnitPrice REAL NOT NULL CHECK (UnitPrice >= 0),
    LineTotal REAL NOT NULL CHECK (LineTotal >= 0),
    Note TEXT,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BillId) REFERENCES Bills(Id) ON UPDATE CASCADE ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS Payments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    BillId INTEGER NOT NULL UNIQUE,
    PaymentType INTEGER NOT NULL CHECK (PaymentType IN (0,1)),
    Amount REAL NOT NULL CHECK (Amount >= 0),
    PaidByUserId INTEGER NOT NULL,
    PaidAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (BillId) REFERENCES Bills(Id) ON UPDATE CASCADE ON DELETE RESTRICT,
    FOREIGN KEY (PaidByUserId) REFERENCES Users(Id) ON UPDATE CASCADE ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_Bills_OneOpenBillPerTable
ON Bills(TableId)
WHERE Status = 0;

CREATE INDEX IF NOT EXISTS IX_Users_RoleId ON Users(RoleId);
CREATE INDEX IF NOT EXISTS IX_Products_CategoryId_IsActive ON Products(CategoryId, IsActive);
CREATE INDEX IF NOT EXISTS IX_Bills_Status_TableId ON Bills(Status, TableId);
CREATE INDEX IF NOT EXISTS IX_BillItems_BillId ON BillItems(BillId);
CREATE INDEX IF NOT EXISTS IX_BillItems_ProductId ON BillItems(ProductId);
CREATE INDEX IF NOT EXISTS IX_Payments_PaidAt ON Payments(PaidAt);

INSERT OR IGNORE INTO Roles (Name, IsActive, CreatedAt, UpdatedAt)
VALUES
('Cashier', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Waiter', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Admin', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

INSERT OR IGNORE INTO Users (RoleId, Username, PasswordHash, FullName, IsActive, CreatedAt, UpdatedAt)
VALUES
((SELECT Id FROM Roles WHERE Name = 'Cashier' LIMIT 1), 'cashier1', '123456', 'Ana Kasa', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
((SELECT Id FROM Roles WHERE Name = 'Waiter' LIMIT 1), 'waiter1', '123456', 'Salon Garsonu', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
((SELECT Id FROM Roles WHERE Name = 'Admin' LIMIT 1), 'admin', 'admin123', 'Sistem Yoneticisi', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

UPDATE RestaurantTables
SET Name = 'T1', Area = 'DIS', IsActive = 1, UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Terrace 1'
  AND NOT EXISTS (SELECT 1 FROM RestaurantTables WHERE Name = 'T1');

UPDATE RestaurantTables
SET Name = 'T2', Area = 'DIS', IsActive = 1, UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Terrace 2'
  AND NOT EXISTS (SELECT 1 FROM RestaurantTables WHERE Name = 'T2');

UPDATE RestaurantTables
SET Name = 'T3', Area = 'DIS', IsActive = 1, UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Terrace 3'
  AND NOT EXISTS (SELECT 1 FROM RestaurantTables WHERE Name = 'T3');

UPDATE RestaurantTables
SET Name = 'T4', Area = 'DIS', IsActive = 1, UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Hall 1'
  AND NOT EXISTS (SELECT 1 FROM RestaurantTables WHERE Name = 'T4');

UPDATE RestaurantTables
SET Name = 'T5', Area = 'DIS', IsActive = 1, UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Hall 2'
  AND NOT EXISTS (SELECT 1 FROM RestaurantTables WHERE Name = 'T5');

UPDATE RestaurantTables
SET Name = 'İÇ 1', Area = 'IC', IsActive = 1, UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Hall 3'
  AND NOT EXISTS (SELECT 1 FROM RestaurantTables WHERE Name = 'İÇ 1');

INSERT OR IGNORE INTO RestaurantTables (Name, Area, Status, IsActive, CreatedAt, UpdatedAt)
VALUES
('T1', 'DIS', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('T2', 'DIS', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('T3', 'DIS', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('T4', 'DIS', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('T5', 'DIS', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('İÇ 1', 'IC', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('İÇ 2', 'IC', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('İÇ 3', 'IC', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('İÇ 4', 'IC', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('İÇ 5', 'IC', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('İÇ 6', 'IC', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('İÇ 7', 'IC', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('İÇ 8', 'IC', 0, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

UPDATE RestaurantTables
SET IsActive = CASE
    WHEN Name IN ('T1', 'T2', 'T3', 'T4', 'T5', 'İÇ 1', 'İÇ 2', 'İÇ 3', 'İÇ 4', 'İÇ 5', 'İÇ 6', 'İÇ 7', 'İÇ 8') THEN 1
    ELSE 0
END,
UpdatedAt = CURRENT_TIMESTAMP;

INSERT OR IGNORE INTO Categories (Name, IsActive, CreatedAt, UpdatedAt)
VALUES
('Icecek', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Yemek', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('Tatli', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

UPDATE Categories
SET Name = 'Icecek',
    IsActive = 1,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Drink'
  AND NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'Icecek');

UPDATE Categories
SET Name = 'Yemek',
    IsActive = 1,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Food'
  AND NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'Yemek');

UPDATE Categories
SET Name = 'Tatli',
    IsActive = 1,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Dessert'
  AND NOT EXISTS (SELECT 1 FROM Categories WHERE Name = 'Tatli');

UPDATE Categories
SET IsActive = CASE
    WHEN Name IN ('Icecek', 'Yemek', 'Tatli') THEN 1
    ELSE 0
END,
UpdatedAt = CURRENT_TIMESTAMP;

INSERT OR IGNORE INTO Products (CategoryId, Name, Price, IsActive, CreatedAt, UpdatedAt)
VALUES
((SELECT Id FROM Categories WHERE Name = 'Icecek' LIMIT 1), 'Cay', 25.00, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
((SELECT Id FROM Categories WHERE Name = 'Icecek' LIMIT 1), 'Kahve', 45.00, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
((SELECT Id FROM Categories WHERE Name = 'Yemek' LIMIT 1), 'Cizburger', 220.00, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
((SELECT Id FROM Categories WHERE Name = 'Tatli' LIMIT 1), 'Baklava', 130.00, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

UPDATE Products
SET Name = 'Cay',
    IsActive = 1,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Tea'
  AND NOT EXISTS (
      SELECT 1 FROM Products p2
      WHERE p2.CategoryId = Products.CategoryId
        AND p2.Name = 'Cay');

UPDATE Products
SET Name = 'Kahve',
    IsActive = 1,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Coffee'
  AND NOT EXISTS (
      SELECT 1 FROM Products p2
      WHERE p2.CategoryId = Products.CategoryId
        AND p2.Name = 'Kahve');

UPDATE Products
SET Name = 'Cizburger',
    IsActive = 1,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Name = 'Cheeseburger'
  AND NOT EXISTS (
      SELECT 1 FROM Products p2
      WHERE p2.CategoryId = Products.CategoryId
        AND p2.Name = 'Cizburger');

UPDATE Users
SET FullName = CASE Username
    WHEN 'cashier1' THEN 'Ana Kasa'
    WHEN 'waiter1' THEN 'Salon Garsonu'
    WHEN 'admin' THEN 'Sistem Yoneticisi'
    ELSE FullName
END,
UpdatedAt = CURRENT_TIMESTAMP
WHERE Username IN ('cashier1', 'waiter1', 'admin');
";

            command.ExecuteNonQuery();
        }
    }
}
