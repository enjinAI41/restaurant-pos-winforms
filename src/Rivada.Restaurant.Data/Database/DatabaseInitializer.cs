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

INSERT INTO Categories (Name, IsActive, CreatedAt, UpdatedAt)
VALUES
('MEZELER', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('BAŞLANGIÇLAR', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('SALATALAR', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ANA YEMEKLER', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('TAVUK YEMEKLERİ', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('MEYVE', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ÇEREZLER', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('SOĞUK & SICAK İÇECEKLER', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('BİRALAR (50cl)', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ŞARAPLAR 37.5cl', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('ŞARAPLAR 75cl', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('VOTKA', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('VİSKİ', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
('RAKILAR', 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
ON CONFLICT(Name) DO UPDATE SET
    IsActive = excluded.IsActive,
    UpdatedAt = CURRENT_TIMESTAMP;

UPDATE Categories
SET IsActive = 0,
    UpdatedAt = CURRENT_TIMESTAMP
WHERE Name IN ('Icecek', 'Yemek', 'Tatli', 'Drink', 'Food', 'Dessert');

INSERT INTO Products (CategoryId, Name, Price, IsActive, CreatedAt, UpdatedAt)
SELECT
    categories.Id,
    data.column2,
    data.column3,
    1,
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
FROM (
    VALUES
    ('MEZELER', 'Beyaz Peynir', 150),
    ('MEZELER', 'Haydari', 200),
    ('MEZELER', 'Acılı Meze', 200),
    ('MEZELER', 'Köpoğlu', 200),
    ('MEZELER', 'Atom', 200),
    ('MEZELER', 'Rus Salatası', 200),
    ('MEZELER', 'Pastırmalı Humus', 220),
    ('MEZELER', 'Şakşuka', 200),
    ('MEZELER', 'Patlıcan Ezme', 200),
    ('MEZELER', 'Girit Ezme', 200),
    ('MEZELER', 'Yaprak Sarma', 200),
    ('MEZELER', 'Çiğ Köfte', 200),
    ('MEZELER', 'Havuç Tarator', 200),
    ('MEZELER', 'Semizotu', 200),
    ('MEZELER', 'Kuru Cacık', 180),
    ('BAŞLANGIÇLAR', 'Kavun', 150),
    ('BAŞLANGIÇLAR', 'Kaşarlı Mantar', 300),
    ('BAŞLANGIÇLAR', 'Güveçte Sucuk', 300),
    ('BAŞLANGIÇLAR', 'Yaprak Ciğer', 450),
    ('BAŞLANGIÇLAR', 'Paçanga Böreği', 280),
    ('BAŞLANGIÇLAR', 'Sigara Böreği', 200),
    ('BAŞLANGIÇLAR', 'Söğüş', 100),
    ('BAŞLANGIÇLAR', 'Patates Tava', 200),
    ('SALATALAR', 'Mevsim Salata', 220),
    ('SALATALAR', 'Çoban Salata', 220),
    ('SALATALAR', 'Kaşık Salata', 220),
    ('ANA YEMEKLER', 'Rivada Güveç', 800),
    ('ANA YEMEKLER', 'Kuzu Pirzola', 850),
    ('ANA YEMEKLER', 'Antrikot', 850),
    ('ANA YEMEKLER', 'Saç Kavurma', 800),
    ('ANA YEMEKLER', 'Köfte Izgara', 550),
    ('ANA YEMEKLER', 'Bonfile', 1000),
    ('ANA YEMEKLER', 'Et Çökertme', 700),
    ('TAVUK YEMEKLERİ', 'Tavuk Saç Kavurma', 450),
    ('TAVUK YEMEKLERİ', 'Köri Soslu Tavuk', 450),
    ('TAVUK YEMEKLERİ', 'Kremalı Tavuk', 450),
    ('TAVUK YEMEKLERİ', 'Kremalı Makarna', 450),
    ('TAVUK YEMEKLERİ', 'Köri Soslu Makarna', 450),
    ('TAVUK YEMEKLERİ', 'Tavuk Izgara', 450),
    ('TAVUK YEMEKLERİ', 'Tavuk Schnitzel', 450),
    ('TAVUK YEMEKLERİ', 'Tavuk Çökertme', 450),
    ('MEYVE', 'Meyve Tabağı (Küçük)', 180),
    ('MEYVE', 'Meyve Tabağı (Büyük)', 350),
    ('MEYVE', 'Serpme Meyve', 400),
    ('MEYVE', 'Ballı Muz', 250),
    ('ÇEREZLER', 'Tuzlu Fıstık', 150),
    ('ÇEREZLER', 'Badem', 250),
    ('ÇEREZLER', 'Antep Fıstığı', 350),
    ('ÇEREZLER', 'Lüks Kokteyl', 280),
    ('ÇEREZLER', 'Çikolata', 150),
    ('SOĞUK & SICAK İÇECEKLER', 'Cola', 100),
    ('SOĞUK & SICAK İÇECEKLER', 'Ice Tea', 100),
    ('SOĞUK & SICAK İÇECEKLER', 'Fanta', 100),
    ('SOĞUK & SICAK İÇECEKLER', 'Soda', 65),
    ('SOĞUK & SICAK İÇECEKLER', 'Şalgam (30cl)', 80),
    ('SOĞUK & SICAK İÇECEKLER', 'Su (0.5 lt)', 20),
    ('SOĞUK & SICAK İÇECEKLER', 'Su (1.5 lt)', 50),
    ('SOĞUK & SICAK İÇECEKLER', 'Redbull', 160),
    ('SOĞUK & SICAK İÇECEKLER', 'Demleme Çay', 25),
    ('SOĞUK & SICAK İÇECEKLER', 'Türk Kahvesi', 75),
    ('SOĞUK & SICAK İÇECEKLER', 'Nescafe', 90),
    ('BİRALAR (50cl)', 'Efes Pilsen', 220),
    ('BİRALAR (50cl)', 'Tuborg Gold', 220),
    ('BİRALAR (50cl)', 'Carlsberg', 220),
    ('BİRALAR (50cl)', 'Bomonti', 230),
    ('BİRALAR (50cl)', 'Miller', 250),
    ('BİRALAR (50cl)', 'Tuborg Extra', 240),
    ('ŞARAPLAR 37.5cl', 'Buzbağ (Öküzgözü-Boğazkere)', 800),
    ('ŞARAPLAR 75cl', 'Buzbağ', 1300),
    ('ŞARAPLAR 75cl', 'Terra Roze Kalecik', 1350),
    ('VOTKA', 'İstanblue 20cl', 750),
    ('VOTKA', 'İstanblue 35cl', 1200),
    ('VOTKA', 'İstanblue 50cl', 1600),
    ('VOTKA', 'İstanblue 70cl', 2000),
    ('VİSKİ', 'Chivas Regal 12 Years Old 35cl', 2200),
    ('VİSKİ', 'Chivas Regal 12 Years Old 50cl', 3000),
    ('VİSKİ', 'Chivas Regal 12 Years Old 70cl', 3900),
    ('RAKILAR', 'Yeni Rakı 20cl', 850),
    ('RAKILAR', 'Yeni Rakı 35cl', 1250),
    ('RAKILAR', 'Yeni Rakı 50cl', 1750),
    ('RAKILAR', 'Yeni Rakı 70cl', 2250),
    ('RAKILAR', 'Yeni Rakı 100cl', 2900),
    ('RAKILAR', 'Efe Gold 20cl', 1050),
    ('RAKILAR', 'Efe Gold 35cl', 1600),
    ('RAKILAR', 'Efe Gold 50cl', 2250),
    ('RAKILAR', 'Efe Gold 70cl', 2800),
    ('RAKILAR', 'Efe Gold 100cl', 3600),
    ('RAKILAR', 'Tekirdağ Altın 20cl', 1050),
    ('RAKILAR', 'Tekirdağ Altın 35cl', 1600),
    ('RAKILAR', 'Tekirdağ Altın 50cl', 2250),
    ('RAKILAR', 'Tekirdağ Altın 70cl', 2800),
    ('RAKILAR', 'Tekirdağ Altın 100cl', 3600),
    ('RAKILAR', 'Tekirdağ Yaş Üzüm 35cl', 1400),
    ('RAKILAR', 'Tekirdağ Yaş Üzüm 50cl', 2000),
    ('RAKILAR', 'Tekirdağ Yaş Üzüm 70cl', 2500),
    ('RAKILAR', 'Beylerbeyi Göbek 20cl', 1200),
    ('RAKILAR', 'Beylerbeyi Göbek 35cl', 2000),
    ('RAKILAR', 'Beylerbeyi Göbek 50cl', 2750),
    ('RAKILAR', 'Beylerbeyi Göbek 70cl', 3600)
) AS data
INNER JOIN Categories categories
    ON categories.Name = data.column1
ON CONFLICT(CategoryId, Name) DO UPDATE SET
    Price = excluded.Price,
    IsActive = 1,
    UpdatedAt = CURRENT_TIMESTAMP;

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
