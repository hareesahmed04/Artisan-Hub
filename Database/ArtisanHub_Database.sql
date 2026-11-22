/* =====================================================================
   ArtisanHub - Local Artisan / Maker Marketplace
   DBMS Project | Iqra University | BS Computer Science
   Group: Harees Ahmed, Zeeshan Wazir

   Full database script for Microsoft SQL Server:
     1. Creates the ArtisanHubDB database (if missing)
     2. Creates 15 tables with PK, FK, UNIQUE, CHECK and DEFAULT constraints
     3. Creates 5 views and 1 trigger
     4. Inserts sample data (demo logins are listed at the bottom)

   Running this script again RESETS all data in ArtisanHubDB.
   The web app runs the part after the BEGIN_SCHEMA marker automatically
   the first time it starts, so running it by hand is optional.
   ===================================================================== */

IF DB_ID(N'ArtisanHubDB') IS NULL
    CREATE DATABASE ArtisanHubDB;
GO

USE ArtisanHubDB;
GO

-- @@BEGIN_SCHEMA@@

/* ---------- Clean start (drop in reverse dependency order) ---------- */
DROP VIEW IF EXISTS vw_ProductRatings;
DROP VIEW IF EXISTS vw_SalesByCity;
DROP VIEW IF EXISTS vw_SalesByCategory;
DROP VIEW IF EXISTS vw_SalesByArtist;
DROP VIEW IF EXISTS vw_OrderTotals;
DROP TABLE IF EXISTS Reviews, Shipments, Payments, OrderItems, Orders, CartItems,
                     CustomRequests, ProductImages, Products, Categories,
                     Artists, Addresses, Cities, Users, Roles;
GO

/* =========================== ACCOUNTS MODULE =========================== */

CREATE TABLE Roles (
    RoleID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Roles PRIMARY KEY,
    RoleName    NVARCHAR(20)      NOT NULL CONSTRAINT UQ_Roles_RoleName UNIQUE,
    CONSTRAINT CK_Roles_RoleName CHECK (RoleName IN (N'Admin', N'Artist', N'Customer'))
);

CREATE TABLE Users (
    UserID       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    RoleID       INT               NOT NULL CONSTRAINT FK_Users_Roles REFERENCES Roles(RoleID),
    FullName     NVARCHAR(100)     NOT NULL,
    Email        NVARCHAR(255)     NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    PasswordHash NVARCHAR(255)     NOT NULL,
    Phone        NVARCHAR(20)      NULL,
    CreatedAt    DATETIME2         NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME(),
    IsActive     BIT               NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1
);

CREATE TABLE Cities (
    CityID    INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cities PRIMARY KEY,
    CityName  NVARCHAR(60)      NOT NULL,
    Province  NVARCHAR(60)      NOT NULL,
    CONSTRAINT UQ_Cities_Name_Province UNIQUE (CityName, Province)
);

CREATE TABLE Addresses (
    AddressID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Addresses PRIMARY KEY,
    UserID      INT               NOT NULL CONSTRAINT FK_Addresses_Users REFERENCES Users(UserID),
    CityID      INT               NOT NULL CONSTRAINT FK_Addresses_Cities REFERENCES Cities(CityID),
    AddressLine NVARCHAR(200)     NOT NULL,
    PostalCode  NVARCHAR(10)      NULL,
    IsDefault   BIT               NOT NULL CONSTRAINT DF_Addresses_IsDefault DEFAULT 0
);

CREATE TABLE Artists (
    ArtistID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Artists PRIMARY KEY,
    UserID     INT               NOT NULL CONSTRAINT FK_Artists_Users REFERENCES Users(UserID)
                                          CONSTRAINT UQ_Artists_UserID UNIQUE,
    CityID     INT               NOT NULL CONSTRAINT FK_Artists_Cities REFERENCES Cities(CityID),
    StoreName  NVARCHAR(100)     NOT NULL CONSTRAINT UQ_Artists_StoreName UNIQUE,
    Bio        NVARCHAR(1000)    NULL,
    JoinDate   DATE              NOT NULL CONSTRAINT DF_Artists_JoinDate DEFAULT CAST(GETDATE() AS DATE),
    IsVerified BIT               NOT NULL CONSTRAINT DF_Artists_IsVerified DEFAULT 0
);

/* =========================== CATALOG MODULE =========================== */

CREATE TABLE Categories (
    CategoryID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    CategoryName NVARCHAR(60)      NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
    Description  NVARCHAR(300)     NULL
);

CREATE TABLE Products (
    ProductID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    ArtistID       INT               NOT NULL CONSTRAINT FK_Products_Artists REFERENCES Artists(ArtistID),
    CategoryID     INT               NOT NULL CONSTRAINT FK_Products_Categories REFERENCES Categories(CategoryID),
    Title          NVARCHAR(150)     NOT NULL,
    Description    NVARCHAR(2000)    NULL,
    Price          DECIMAL(10,2)     NOT NULL CONSTRAINT CK_Products_Price CHECK (Price > 0),
    StockQty       INT               NOT NULL CONSTRAINT CK_Products_StockQty CHECK (StockQty >= 0),
    IsCustomizable BIT               NOT NULL CONSTRAINT DF_Products_IsCustomizable DEFAULT 0,
    IsActive       BIT               NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT 1,
    CreatedAt      DATETIME2         NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT SYSDATETIME()
);

CREATE TABLE ProductImages (
    ImageID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductImages PRIMARY KEY,
    ProductID INT               NOT NULL CONSTRAINT FK_ProductImages_Products
                                         REFERENCES Products(ProductID) ON DELETE CASCADE,
    ImageUrl  NVARCHAR(500)     NOT NULL,
    IsPrimary BIT               NOT NULL CONSTRAINT DF_ProductImages_IsPrimary DEFAULT 0
);

CREATE TABLE CustomRequests (
    RequestID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CustomRequests PRIMARY KEY,
    UserID      INT               NOT NULL CONSTRAINT FK_CustomRequests_Users REFERENCES Users(UserID),
    ArtistID    INT               NOT NULL CONSTRAINT FK_CustomRequests_Artists REFERENCES Artists(ArtistID),
    CategoryID  INT               NULL     CONSTRAINT FK_CustomRequests_Categories REFERENCES Categories(CategoryID),
    Description NVARCHAR(2000)    NOT NULL,
    Budget      DECIMAL(10,2)     NULL     CONSTRAINT CK_CustomRequests_Budget CHECK (Budget > 0),
    RequiredBy  DATE              NULL,
    QuotedPrice DECIMAL(10,2)     NULL     CONSTRAINT CK_CustomRequests_QuotedPrice CHECK (QuotedPrice > 0),
    Status      NVARCHAR(20)      NOT NULL CONSTRAINT DF_CustomRequests_Status DEFAULT N'Pending',
    CreatedAt   DATETIME2         NOT NULL CONSTRAINT DF_CustomRequests_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT CK_CustomRequests_Status
        CHECK (Status IN (N'Pending', N'Quoted', N'Accepted', N'Rejected', N'Completed'))
);

/* ============================ ORDERS MODULE ============================ */

CREATE TABLE CartItems (
    UserID    INT       NOT NULL CONSTRAINT FK_CartItems_Users REFERENCES Users(UserID),
    ProductID INT       NOT NULL CONSTRAINT FK_CartItems_Products REFERENCES Products(ProductID),
    Quantity  INT       NOT NULL CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0),
    AddedAt   DATETIME2 NOT NULL CONSTRAINT DF_CartItems_AddedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_CartItems PRIMARY KEY (UserID, ProductID)
);

CREATE TABLE Orders (
    OrderID     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    UserID      INT               NOT NULL CONSTRAINT FK_Orders_Users REFERENCES Users(UserID),
    AddressID   INT               NOT NULL CONSTRAINT FK_Orders_Addresses REFERENCES Addresses(AddressID),
    OrderDate   DATETIME2         NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSDATETIME(),
    Status      NVARCHAR(20)      NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Pending',
    ShippingFee DECIMAL(10,2)     NOT NULL CONSTRAINT DF_Orders_ShippingFee DEFAULT 0,
    CONSTRAINT CK_Orders_Status
        CHECK (Status IN (N'Pending', N'Paid', N'Shipped', N'Delivered', N'Cancelled')),
    CONSTRAINT CK_Orders_ShippingFee CHECK (ShippingFee >= 0)
);

CREATE TABLE OrderItems (
    OrderID   INT           NOT NULL CONSTRAINT FK_OrderItems_Orders REFERENCES Orders(OrderID),
    ProductID INT           NOT NULL CONSTRAINT FK_OrderItems_Products REFERENCES Products(ProductID),
    Quantity  INT           NOT NULL CONSTRAINT CK_OrderItems_Quantity CHECK (Quantity > 0),
    UnitPrice DECIMAL(10,2) NOT NULL CONSTRAINT CK_OrderItems_UnitPrice CHECK (UnitPrice > 0),
    CONSTRAINT PK_OrderItems PRIMARY KEY (OrderID, ProductID)
);

CREATE TABLE Payments (
    PaymentID      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    OrderID        INT               NOT NULL CONSTRAINT FK_Payments_Orders REFERENCES Orders(OrderID)
                                              CONSTRAINT UQ_Payments_OrderID UNIQUE,
    Method         NVARCHAR(20)      NOT NULL,
    Amount         DECIMAL(10,2)     NOT NULL CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
    Status         NVARCHAR(20)      NOT NULL CONSTRAINT DF_Payments_Status DEFAULT N'Pending',
    PaidAt         DATETIME2         NULL,
    TransactionRef NVARCHAR(50)      NULL,
    CONSTRAINT CK_Payments_Method CHECK (Method IN (N'COD', N'Card', N'JazzCash', N'EasyPaisa')),
    CONSTRAINT CK_Payments_Status CHECK (Status IN (N'Pending', N'Completed', N'Failed', N'Refunded'))
);

CREATE TABLE Shipments (
    ShipmentID  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Shipments PRIMARY KEY,
    OrderID     INT               NOT NULL CONSTRAINT FK_Shipments_Orders REFERENCES Orders(OrderID),
    ArtistID    INT               NOT NULL CONSTRAINT FK_Shipments_Artists REFERENCES Artists(ArtistID),
    CourierName NVARCHAR(60)      NULL,
    TrackingNo  NVARCHAR(50)      NULL,
    Status      NVARCHAR(20)      NOT NULL CONSTRAINT DF_Shipments_Status DEFAULT N'Preparing',
    ShippedAt   DATETIME2         NULL,
    DeliveredAt DATETIME2         NULL,
    CONSTRAINT UQ_Shipments_Order_Artist UNIQUE (OrderID, ArtistID),
    CONSTRAINT CK_Shipments_Status CHECK (Status IN (N'Preparing', N'Shipped', N'Delivered')),
    CONSTRAINT CK_Shipments_Dates CHECK (DeliveredAt >= ShippedAt)
);

CREATE TABLE Reviews (
    ReviewID   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY,
    ProductID  INT               NOT NULL CONSTRAINT FK_Reviews_Products REFERENCES Products(ProductID),
    UserID     INT               NOT NULL CONSTRAINT FK_Reviews_Users REFERENCES Users(UserID),
    Rating     TINYINT           NOT NULL CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5),
    Comment    NVARCHAR(1000)    NULL,
    ReviewDate DATETIME2         NOT NULL CONSTRAINT DF_Reviews_ReviewDate DEFAULT SYSDATETIME(),
    CONSTRAINT UQ_Reviews_Product_User UNIQUE (ProductID, UserID)
);

/* ---------- Indexes on foreign keys used by search and reports ---------- */
CREATE INDEX IX_Products_ArtistID   ON Products(ArtistID);
CREATE INDEX IX_Products_CategoryID ON Products(CategoryID);
CREATE INDEX IX_OrderItems_ProductID ON OrderItems(ProductID);
CREATE INDEX IX_Orders_UserID       ON Orders(UserID);
CREATE INDEX IX_Shipments_ArtistID  ON Shipments(ArtistID);
GO

/* ================================ VIEWS ================================ */

-- Order total is computed, never stored (keeps Orders in 3NF)
CREATE VIEW vw_OrderTotals AS
SELECT o.OrderID,
       o.UserID,
       o.OrderDate,
       o.Status,
       SUM(oi.Quantity)                                            AS ItemCount,
       CAST(SUM(oi.Quantity * oi.UnitPrice) AS DECIMAL(12,2))      AS ItemsTotal,
       o.ShippingFee,
       CAST(SUM(oi.Quantity * oi.UnitPrice) + o.ShippingFee AS DECIMAL(12,2)) AS TotalAmount
FROM Orders o
JOIN OrderItems oi ON oi.OrderID = o.OrderID
GROUP BY o.OrderID, o.UserID, o.OrderDate, o.Status, o.ShippingFee;
GO

-- Sales per artist (cancelled orders excluded)
CREATE VIEW vw_SalesByArtist AS
SELECT a.ArtistID,
       a.StoreName,
       c.CityName,
       a.IsVerified,
       COUNT(DISTINCT s.OrderID)                                   AS OrdersCount,
       ISNULL(SUM(s.Quantity), 0)                                  AS ItemsSold,
       CAST(ISNULL(SUM(s.Quantity * s.UnitPrice), 0) AS DECIMAL(12,2)) AS TotalSales
FROM Artists a
JOIN Cities c ON c.CityID = a.CityID
LEFT JOIN Products p ON p.ArtistID = a.ArtistID
LEFT JOIN (SELECT oi.OrderID, oi.ProductID, oi.Quantity, oi.UnitPrice
           FROM OrderItems oi
           JOIN Orders o ON o.OrderID = oi.OrderID
           WHERE o.Status <> N'Cancelled') s ON s.ProductID = p.ProductID
GROUP BY a.ArtistID, a.StoreName, c.CityName, a.IsVerified;
GO

-- Sales per category
CREATE VIEW vw_SalesByCategory AS
SELECT cat.CategoryID,
       cat.CategoryName,
       COUNT(DISTINCT p.ProductID)                                 AS ProductCount,
       ISNULL(SUM(s.Quantity), 0)                                  AS ItemsSold,
       CAST(ISNULL(SUM(s.Quantity * s.UnitPrice), 0) AS DECIMAL(12,2)) AS TotalSales
FROM Categories cat
LEFT JOIN Products p ON p.CategoryID = cat.CategoryID
LEFT JOIN (SELECT oi.ProductID, oi.Quantity, oi.UnitPrice
           FROM OrderItems oi
           JOIN Orders o ON o.OrderID = oi.OrderID
           WHERE o.Status <> N'Cancelled') s ON s.ProductID = p.ProductID
GROUP BY cat.CategoryID, cat.CategoryName;
GO

-- Sales per city (city of the artist who made the item)
CREATE VIEW vw_SalesByCity AS
SELECT ci.CityID,
       ci.CityName,
       ci.Province,
       COUNT(DISTINCT a.ArtistID)                                  AS ArtistCount,
       ISNULL(SUM(s.Quantity), 0)                                  AS ItemsSold,
       CAST(ISNULL(SUM(s.Quantity * s.UnitPrice), 0) AS DECIMAL(12,2)) AS TotalSales
FROM Cities ci
LEFT JOIN Artists a ON a.CityID = ci.CityID
LEFT JOIN Products p ON p.ArtistID = a.ArtistID
LEFT JOIN (SELECT oi.ProductID, oi.Quantity, oi.UnitPrice
           FROM OrderItems oi
           JOIN Orders o ON o.OrderID = oi.OrderID
           WHERE o.Status <> N'Cancelled') s ON s.ProductID = p.ProductID
GROUP BY ci.CityID, ci.CityName, ci.Province;
GO

-- Average rating per product
CREATE VIEW vw_ProductRatings AS
SELECT r.ProductID,
       CAST(AVG(CAST(r.Rating AS DECIMAL(4,2))) AS DECIMAL(4,2)) AS AvgRating,
       COUNT(*)                                                    AS ReviewCount
FROM Reviews r
GROUP BY r.ProductID;
GO

/* =============================== TRIGGER =============================== */

-- Reduces stock when items are ordered; refuses to oversell.
CREATE TRIGGER trg_OrderItems_ReduceStock
ON OrderItems
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1
               FROM Products p
               JOIN (SELECT ProductID, SUM(Quantity) AS Qty
                     FROM inserted GROUP BY ProductID) i ON i.ProductID = p.ProductID
               WHERE p.StockQty < i.Qty)
    BEGIN
        THROW 50001, N'Not enough stock for one or more products in this order.', 1;
    END;

    UPDATE p
    SET    p.StockQty = p.StockQty - i.Qty
    FROM   Products p
    JOIN  (SELECT ProductID, SUM(Quantity) AS Qty
           FROM inserted GROUP BY ProductID) i ON i.ProductID = p.ProductID;
END;
GO

/* ============================= SAMPLE DATA ============================= */

SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (RoleID, RoleName) VALUES (1, N'Admin'), (2, N'Artist'), (3, N'Customer');
SET IDENTITY_INSERT Roles OFF;

SET IDENTITY_INSERT Cities ON;
INSERT INTO Cities (CityID, CityName, Province) VALUES
    (1, N'Karachi', N'Sindh'),
    (2, N'Lahore', N'Punjab'),
    (3, N'Islamabad', N'Islamabad Capital Territory'),
    (4, N'Hyderabad', N'Sindh'),
    (5, N'Multan', N'Punjab'),
    (6, N'Peshawar', N'Khyber Pakhtunkhwa'),
    (7, N'Quetta', N'Balochistan'),
    (8, N'Faisalabad', N'Punjab');
SET IDENTITY_INSERT Cities OFF;

SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (CategoryID, CategoryName, Description) VALUES
    (1, N'Pottery & Ceramics', N'Hand-thrown pots, glazed tableware and painted tiles fired in local kilns.'),
    (2, N'Textiles & Embroidery', N'Hand-embroidered shawls, block prints and woven fabrics.'),
    (3, N'Jewelry', N'Handmade silver, brass, kundan and beaded jewelry.'),
    (4, N'Woodwork', N'Carved and hand-turned wooden decor, boxes and kitchenware.'),
    (5, N'Paintings & Calligraphy', N'Original paintings, calligraphy and miniature art.'),
    (6, N'Home Decor', N'Lamps, lanterns, rugs and wall hangings for the home.');
SET IDENTITY_INSERT Categories OFF;

-- Passwords: admin = Admin@123, every other demo account = Pass@123 (stored as PBKDF2 hashes)
DECLARE @AdminHash NVARCHAR(255) = N'AQAAAAIAAYagAAAAEBagtbrkOxF/QAfpzcZvfN96ndiXN+qRnlgfjBia6I6qP89f22m2zCUpeo/8RNllJA==';
DECLARE @UserHash  NVARCHAR(255) = N'AQAAAAIAAYagAAAAEO+94cJO8VTZd07UgW+tNI6Prta3FBCwCPXUA9riaLOIsTirbl45OdEpMK/CHagdVg==';

SET IDENTITY_INSERT Users ON;
INSERT INTO Users (UserID, RoleID, FullName, Email, PasswordHash, Phone, CreatedAt, IsActive) VALUES
    (1, 1, N'Site Administrator', N'admin@artisanhub.pk', @AdminHash, N'0300-0000000', DATEADD(DAY, -200, SYSDATETIME()), 1),
    (2, 2, N'Sana Malik', N'sana@artisanhub.pk', @UserHash, N'0301-2345678', DATEADD(DAY, -180, SYSDATETIME()), 1),
    (3, 2, N'Rashid Memon', N'rashid@artisanhub.pk', @UserHash, N'0302-3456789', DATEADD(DAY, -175, SYSDATETIME()), 1),
    (4, 2, N'Hina Qureshi', N'hina@artisanhub.pk', @UserHash, N'0303-4567890', DATEADD(DAY, -170, SYSDATETIME()), 1),
    (5, 2, N'Imran Afridi', N'imran@artisanhub.pk', @UserHash, N'0304-5678901', DATEADD(DAY, -165, SYSDATETIME()), 1),
    (6, 2, N'Maryam Javed', N'maryam@artisanhub.pk', @UserHash, N'0305-6789012', DATEADD(DAY, -160, SYSDATETIME()), 1),
    (7, 2, N'Ali Raza', N'ali@artisanhub.pk', @UserHash, N'0306-7890123', DATEADD(DAY, -150, SYSDATETIME()), 1),
    (8, 2, N'Kamran Baloch', N'kamran@artisanhub.pk', @UserHash, N'0307-8901234', DATEADD(DAY, -4, SYSDATETIME()), 1),
    (9, 3, N'Ayesha Khan', N'ayesha@example.com', @UserHash, N'0321-1112233', DATEADD(DAY, -140, SYSDATETIME()), 1),
    (10, 3, N'Bilal Siddiqui', N'bilal@example.com', @UserHash, N'0322-2223344', DATEADD(DAY, -130, SYSDATETIME()), 1),
    (11, 3, N'Fatima Noor', N'fatima@example.com', @UserHash, N'0323-3334455', DATEADD(DAY, -120, SYSDATETIME()), 1),
    (12, 3, N'Usman Tariq', N'usman@example.com', @UserHash, N'0324-4445566', DATEADD(DAY, -110, SYSDATETIME()), 1);
SET IDENTITY_INSERT Users OFF;

SET IDENTITY_INSERT Addresses ON;
INSERT INTO Addresses (AddressID, UserID, CityID, AddressLine, PostalCode, IsDefault) VALUES
    (1, 9, 1, N'House 14, Block 5, Gulshan-e-Iqbal', N'75300', 1),
    (2, 10, 2, N'Flat 3B, Gulberg III', N'54660', 1),
    (3, 11, 3, N'House 22, Street 9, F-7/2', N'44000', 1),
    (4, 12, 5, N'Plot 7, Shah Rukn-e-Alam Colony', N'60000', 1),
    (5, 9, 1, N'Office 402, Shahrah-e-Faisal', N'75350', 0);
SET IDENTITY_INSERT Addresses OFF;

SET IDENTITY_INSERT Artists ON;
INSERT INTO Artists (ArtistID, UserID, CityID, StoreName, Bio, JoinDate, IsVerified) VALUES
    (1, 2, 1, N'Clay & Kiln Studio', N'Sana throws every pot by hand in her Karachi studio and fires it in a wood kiln. Earthy glazes, honest shapes and pieces made to be used every day.', CAST(DATEADD(DAY, -178, SYSDATETIME()) AS DATE), 1),
    (2, 3, 4, N'Sindhi Threads', N'A family workshop in Hyderabad keeping Sindhi embroidery, mirror work and ajrak block printing alive for three generations.', CAST(DATEADD(DAY, -173, SYSDATETIME()) AS DATE), 1),
    (3, 4, 2, N'Noor Jewels', N'Hina designs silver and kundan pieces in Lahore, mixing Mughal motifs with clean modern lines.', CAST(DATEADD(DAY, -168, SYSDATETIME()) AS DATE), 1),
    (4, 5, 6, N'Walnut & Grain', N'Imran carves walnut and sheesham wood in Peshawar using hand tools passed down from his grandfather.', CAST(DATEADD(DAY, -163, SYSDATETIME()) AS DATE), 1),
    (5, 6, 2, N'Qalam Art House', N'Original calligraphy, miniature paintings and watercolours by Maryam, painted in her Lahore studio.', CAST(DATEADD(DAY, -158, SYSDATETIME()) AS DATE), 1),
    (6, 7, 3, N'Rang Home Decor', N'Lamps, lanterns and woven rugs from Islamabad that bring warm colour and handmade texture into the home.', CAST(DATEADD(DAY, -148, SYSDATETIME()) AS DATE), 1),
    (7, 8, 1, N'Truck Art Tales', N'Kamran paints the bright patterns of Pakistani truck art onto kettles, trunks and name plates.', CAST(DATEADD(DAY, -3, SYSDATETIME()) AS DATE), 0);
SET IDENTITY_INSERT Artists OFF;

SET IDENTITY_INSERT Products ON;
INSERT INTO Products (ProductID, ArtistID, CategoryID, Title, Description, Price, StockQty, IsCustomizable, IsActive, CreatedAt) VALUES
    (1, 1, 1, N'Hand-Thrown Terracotta Vase', N'A tall terracotta vase thrown on the wheel and finished with a soft matte glaze. Each piece has small variations that make it one of a kind. Height about 30 cm.', 3200.00, 12, 1, 1, DATEADD(DAY, -149, SYSDATETIME())),
    (2, 1, 1, N'Glazed Ceramic Serving Bowl', N'A wide, food-safe serving bowl with a glossy glaze that pools beautifully at the rim. Perfect for salads and rice dishes. Diameter about 28 cm.', 2450.00, 15, 0, 1, DATEADD(DAY, -148, SYSDATETIME())),
    (3, 1, 1, N'Set of 4 Clay Cups', N'Four hand-shaped clay cups for chai or water, glazed inside and left natural outside for a cool grip.', 1800.00, 20, 0, 1, DATEADD(DAY, -147, SYSDATETIME())),
    (4, 1, 1, N'Hand-Painted Tile Coasters (Set of 6)', N'Six ceramic tile coasters hand-painted with floral motifs, with cork backing to protect your table.', 1350.00, 25, 1, 1, DATEADD(DAY, -146, SYSDATETIME())),
    (5, 2, 2, N'Sindhi Embroidered Shawl', N'A warm wool-blend shawl with dense Sindhi embroidery and tiny mirrors along the border. Takes about three weeks to stitch.', 6500.00, 8, 1, 1, DATEADD(DAY, -145, SYSDATETIME())),
    (6, 2, 2, N'Ajrak Block-Print Dupatta', N'Traditional ajrak dupatta hand block-printed with natural dyes in deep crimson and indigo-black tones.', 2900.00, 14, 0, 1, DATEADD(DAY, -144, SYSDATETIME())),
    (7, 2, 2, N'Mirror-Work Cushion Cover', N'A 16 x 16 inch cotton cushion cover with hand-stitched mirror work and a hidden zip.', 1600.00, 18, 1, 1, DATEADD(DAY, -143, SYSDATETIME())),
    (8, 2, 2, N'Hand-Woven Cotton Table Runner', N'A hand-loomed cotton table runner with geometric khes patterns, 180 cm long.', 2200.00, 10, 0, 1, DATEADD(DAY, -142, SYSDATETIME())),
    (9, 3, 3, N'Silver Jhumka Earrings', N'Classic bell-shaped jhumkas in oxidised sterling silver with tiny hanging pearls.', 4800.00, 10, 1, 1, DATEADD(DAY, -141, SYSDATETIME())),
    (10, 3, 3, N'Kundan Choker Necklace', N'A bridal-style kundan choker set with matching earrings, hand-set stones on a gold-plated base.', 9500.00, 5, 1, 1, DATEADD(DAY, -140, SYSDATETIME())),
    (11, 3, 3, N'Beaded Brass Bracelet', N'A slim brass bracelet strung with hand-rolled glass beads. Adjustable fit.', 1450.00, 22, 0, 1, DATEADD(DAY, -139, SYSDATETIME())),
    (12, 3, 3, N'Turquoise Stone Ring', N'A sterling silver ring with a natural turquoise stone and hand-engraved band. Sizes can be adjusted on request.', 3600.00, 12, 1, 1, DATEADD(DAY, -138, SYSDATETIME())),
    (13, 4, 4, N'Carved Walnut Jewelry Box', N'A walnut wood box with hand-carved Swati motifs on the lid and a velvet-lined tray inside.', 5400.00, 7, 1, 1, DATEADD(DAY, -137, SYSDATETIME())),
    (14, 4, 4, N'Hand-Turned Wooden Salad Bowl', N'A sheesham wood bowl turned on a lathe and finished with food-safe oil. Diameter about 25 cm.', 3100.00, 9, 0, 1, DATEADD(DAY, -136, SYSDATETIME())),
    (15, 4, 4, N'Sheesham Wood Chopping Board', N'A thick end-grain chopping board. Can be engraved with a name or short message.', 2300.00, 16, 1, 1, DATEADD(DAY, -135, SYSDATETIME())),
    (16, 4, 4, N'Wooden Spice Box (9 Compartments)', N'A traditional masala dabba in walnut wood with nine compartments and a sliding lid.', 3800.00, 6, 0, 1, DATEADD(DAY, -134, SYSDATETIME())),
    (17, 5, 5, N'Arabic Calligraphy Canvas', N'Original calligraphy in gold and black acrylic on a 24 x 36 inch stretched canvas, ready to hang.', 8500.00, 4, 1, 1, DATEADD(DAY, -133, SYSDATETIME())),
    (18, 5, 5, N'Mughal Miniature Painting', N'A hand-painted miniature in the Mughal style using natural pigments on wasli paper, framed.', 12000.00, 3, 1, 1, DATEADD(DAY, -132, SYSDATETIME())),
    (19, 5, 5, N'Old Lahore Street in Watercolour', N'An original watercolour of a busy street in the walled city of Lahore, 12 x 16 inches, mounted.', 6000.00, 5, 1, 1, DATEADD(DAY, -131, SYSDATETIME())),
    (20, 5, 5, N'Calligraphy Bookmark Set', N'Five hand-lettered bookmarks on textured card, each with a short line of poetry.', 900.00, 30, 0, 1, DATEADD(DAY, -130, SYSDATETIME())),
    (21, 6, 6, N'Camel-Skin Lamp (Multani Style)', N'A traditional camel-skin lamp hand-painted with naqashi patterns that glows a warm amber when lit.', 7200.00, 6, 0, 1, DATEADD(DAY, -129, SYSDATETIME())),
    (22, 6, 6, N'Brass Lantern Tealight Holder', N'A pierced brass lantern that throws star-shaped patterns across the room when you light a tealight inside.', 2600.00, 13, 0, 1, DATEADD(DAY, -128, SYSDATETIME())),
    (23, 6, 6, N'Handwoven Jute Rug (3 x 5 ft)', N'A thick, hand-braided jute rug in natural and rust tones. Custom sizes available.', 5900.00, 5, 1, 1, DATEADD(DAY, -127, SYSDATETIME())),
    (24, 6, 6, N'Embroidered Wall Hanging', N'A colourful wall hanging with phulkari-style embroidery on a cotton base, with a wooden hanging rod.', 3400.00, 8, 1, 1, DATEADD(DAY, -126, SYSDATETIME())),
    (25, 7, 6, N'Truck-Art Painted Kettle', N'A steel kettle hand-painted with bright truck-art flowers and peacocks. Decorative use.', 2800.00, 10, 1, 1, DATEADD(DAY, -2, SYSDATETIME())),
    (26, 7, 5, N'Truck-Art Name Plate', N'A hand-painted metal name plate in truck-art style with your family name.', 3500.00, 10, 1, 1, DATEADD(DAY, -2, SYSDATETIME()));
SET IDENTITY_INSERT Products OFF;

INSERT INTO ProductImages (ProductID, ImageUrl, IsPrimary) VALUES
    (1, N'/images/products/p1.svg', 1),
    (2, N'/images/products/p2.svg', 1),
    (3, N'/images/products/p3.svg', 1),
    (4, N'/images/products/p4.svg', 1),
    (5, N'/images/products/p5.svg', 1),
    (6, N'/images/products/p6.svg', 1),
    (7, N'/images/products/p7.svg', 1),
    (8, N'/images/products/p8.svg', 1),
    (9, N'/images/products/p9.svg', 1),
    (10, N'/images/products/p10.svg', 1),
    (11, N'/images/products/p11.svg', 1),
    (12, N'/images/products/p12.svg', 1),
    (13, N'/images/products/p13.svg', 1),
    (14, N'/images/products/p14.svg', 1),
    (15, N'/images/products/p15.svg', 1),
    (16, N'/images/products/p16.svg', 1),
    (17, N'/images/products/p17.svg', 1),
    (18, N'/images/products/p18.svg', 1),
    (19, N'/images/products/p19.svg', 1),
    (20, N'/images/products/p20.svg', 1),
    (21, N'/images/products/p21.svg', 1),
    (22, N'/images/products/p22.svg', 1),
    (23, N'/images/products/p23.svg', 1),
    (24, N'/images/products/p24.svg', 1),
    (25, N'/images/products/p25.svg', 1),
    (26, N'/images/products/p26.svg', 1),
    (1, N'/images/products/p1-b.svg', 0),
    (5, N'/images/products/p5-b.svg', 0),
    (10, N'/images/products/p10-b.svg', 0),
    (17, N'/images/products/p17-b.svg', 0);

-- Orders: the stock trigger reduces Products.StockQty as each order's items are inserted
SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (OrderID, UserID, AddressID, OrderDate, Status, ShippingFee) VALUES
    (1, 9, 1, DATEADD(HOUR, -3, DATEADD(DAY, -60, SYSDATETIME())), N'Delivered', 500.00),
    (2, 10, 2, DATEADD(HOUR, -3, DATEADD(DAY, -45, SYSDATETIME())), N'Delivered', 250.00),
    (3, 11, 3, DATEADD(HOUR, -3, DATEADD(DAY, -30, SYSDATETIME())), N'Delivered', 500.00),
    (4, 12, 4, DATEADD(HOUR, -3, DATEADD(DAY, -20, SYSDATETIME())), N'Shipped', 250.00),
    (5, 9, 1, DATEADD(HOUR, -3, DATEADD(DAY, -12, SYSDATETIME())), N'Shipped', 500.00),
    (6, 10, 2, DATEADD(HOUR, -3, DATEADD(DAY, -6, SYSDATETIME())), N'Paid', 500.00),
    (7, 11, 3, DATEADD(HOUR, -3, DATEADD(DAY, -3, SYSDATETIME())), N'Pending', 500.00),
    (8, 12, 4, DATEADD(HOUR, -3, DATEADD(DAY, -1, SYSDATETIME())), N'Pending', 250.00),
    (9, 9, 1, DATEADD(HOUR, -3, DATEADD(DAY, -40, SYSDATETIME())), N'Cancelled', 250.00),
    (10, 12, 4, DATEADD(HOUR, -3, DATEADD(DAY, -75, SYSDATETIME())), N'Delivered', 500.00),
    (11, 11, 3, DATEADD(HOUR, -3, DATEADD(DAY, -90, SYSDATETIME())), N'Delivered', 500.00),
    (12, 10, 2, DATEADD(HOUR, -3, DATEADD(DAY, -100, SYSDATETIME())), N'Delivered', 500.00);
SET IDENTITY_INSERT Orders OFF;

INSERT INTO OrderItems (OrderID, ProductID, Quantity, UnitPrice) VALUES
    (1, 1, 1, 3200.00),
    (1, 6, 1, 2900.00),
    (2, 9, 1, 4800.00),
    (2, 11, 2, 1450.00),
    (3, 13, 1, 5400.00),
    (3, 22, 2, 2600.00),
    (4, 17, 1, 8500.00),
    (4, 20, 2, 900.00),
    (5, 2, 2, 2450.00),
    (5, 10, 1, 9500.00),
    (6, 5, 1, 6500.00),
    (6, 14, 1, 3100.00),
    (7, 21, 1, 7200.00),
    (7, 3, 1, 1800.00),
    (8, 18, 1, 12000.00),
    (9, 7, 2, 1600.00),
    (10, 23, 1, 5900.00),
    (10, 15, 1, 2300.00),
    (11, 19, 1, 6000.00),
    (11, 4, 2, 1350.00),
    (12, 12, 1, 3600.00),
    (12, 8, 1, 2200.00);

-- Order 9 was cancelled, so its stock goes back on the shelf
UPDATE Products SET StockQty = StockQty + 2 WHERE ProductID = 7;

INSERT INTO Payments (OrderID, Method, Amount, Status, PaidAt, TransactionRef) VALUES
    (1, N'JazzCash', 6600.00, N'Completed', DATEADD(HOUR, -3, DATEADD(DAY, -60, SYSDATETIME())), N'AH-JA-107919'),
    (2, N'COD', 7950.00, N'Completed', DATEADD(DAY, -43, SYSDATETIME()), NULL),
    (3, N'Card', 11100.00, N'Completed', DATEADD(HOUR, -3, DATEADD(DAY, -30, SYSDATETIME())), N'AH-CA-123757'),
    (4, N'EasyPaisa', 10550.00, N'Completed', DATEADD(HOUR, -3, DATEADD(DAY, -20, SYSDATETIME())), N'AH-EA-131676'),
    (5, N'Card', 14900.00, N'Completed', DATEADD(HOUR, -3, DATEADD(DAY, -12, SYSDATETIME())), N'AH-CA-139595'),
    (6, N'JazzCash', 10100.00, N'Completed', DATEADD(HOUR, -3, DATEADD(DAY, -6, SYSDATETIME())), N'AH-JA-147514'),
    (7, N'COD', 9500.00, N'Pending', NULL, NULL),
    (8, N'COD', 12250.00, N'Pending', NULL, NULL),
    (9, N'Card', 3450.00, N'Refunded', DATEADD(HOUR, -3, DATEADD(DAY, -40, SYSDATETIME())), N'AH-CA-171271'),
    (10, N'COD', 8700.00, N'Completed', DATEADD(DAY, -73, SYSDATETIME()), NULL),
    (11, N'Card', 9200.00, N'Completed', DATEADD(HOUR, -3, DATEADD(DAY, -90, SYSDATETIME())), N'AH-CA-187109'),
    (12, N'JazzCash', 6300.00, N'Completed', DATEADD(HOUR, -3, DATEADD(DAY, -100, SYSDATETIME())), N'AH-JA-195028');

INSERT INTO Shipments (OrderID, ArtistID, CourierName, TrackingNo, Status, ShippedAt, DeliveredAt) VALUES
    (1, 1, N'TCS', N'TCS-77120345', N'Delivered', DATEADD(DAY, -59, SYSDATETIME()), DATEADD(DAY, -57, SYSDATETIME())),
    (1, 2, N'Leopards Courier', N'LP-55091827', N'Delivered', DATEADD(DAY, -59, SYSDATETIME()), DATEADD(DAY, -57, SYSDATETIME())),
    (2, 3, N'M&P', N'MP-30981245', N'Delivered', DATEADD(DAY, -44, SYSDATETIME()), DATEADD(DAY, -42, SYSDATETIME())),
    (3, 4, N'TCS', N'TCS-77155610', N'Delivered', DATEADD(DAY, -29, SYSDATETIME()), DATEADD(DAY, -27, SYSDATETIME())),
    (3, 6, N'PostEx', N'PX-11290034', N'Delivered', DATEADD(DAY, -29, SYSDATETIME()), DATEADD(DAY, -27, SYSDATETIME())),
    (4, 5, N'TCS', N'TCS-77201992', N'Shipped', DATEADD(DAY, -19, SYSDATETIME()), NULL),
    (5, 1, N'Leopards Courier', N'LP-55140032', N'Shipped', DATEADD(DAY, -11, SYSDATETIME()), NULL),
    (5, 3, NULL, NULL, N'Preparing', NULL, NULL),
    (6, 2, NULL, NULL, N'Preparing', NULL, NULL),
    (6, 4, NULL, NULL, N'Preparing', NULL, NULL),
    (7, 6, NULL, NULL, N'Preparing', NULL, NULL),
    (7, 1, NULL, NULL, N'Preparing', NULL, NULL),
    (8, 5, NULL, NULL, N'Preparing', NULL, NULL),
    (10, 6, N'Trax', N'TRX-40021877', N'Delivered', DATEADD(DAY, -74, SYSDATETIME()), DATEADD(DAY, -72, SYSDATETIME())),
    (10, 4, N'M&P', N'MP-30900122', N'Delivered', DATEADD(DAY, -74, SYSDATETIME()), DATEADD(DAY, -72, SYSDATETIME())),
    (11, 5, N'TCS', N'TCS-76980011', N'Delivered', DATEADD(DAY, -89, SYSDATETIME()), DATEADD(DAY, -87, SYSDATETIME())),
    (11, 1, N'TCS', N'TCS-76980012', N'Delivered', DATEADD(DAY, -89, SYSDATETIME()), DATEADD(DAY, -87, SYSDATETIME())),
    (12, 3, N'Leopards Courier', N'LP-54800310', N'Delivered', DATEADD(DAY, -99, SYSDATETIME()), DATEADD(DAY, -97, SYSDATETIME())),
    (12, 2, N'PostEx', N'PX-11002345', N'Delivered', DATEADD(DAY, -99, SYSDATETIME()), DATEADD(DAY, -97, SYSDATETIME()));

INSERT INTO Reviews (ProductID, UserID, Rating, Comment, ReviewDate) VALUES
    (1, 9, 5, N'Even more beautiful in person. The glaze has lovely depth and it arrived very well packed.', DATEADD(DAY, -55, SYSDATETIME())),
    (6, 9, 4, N'Rich colours and soft fabric. Slightly smaller than I expected but still lovely.', DATEADD(DAY, -54, SYSDATETIME())),
    (9, 10, 5, N'Gorgeous jhumkas, light enough to wear all evening. Got so many compliments!', DATEADD(DAY, -40, SYSDATETIME())),
    (13, 11, 5, N'The carving is so detailed. My mother loved it as a gift.', DATEADD(DAY, -25, SYSDATETIME())),
    (22, 11, 4, N'Makes beautiful patterns on the wall at night. Brass is good quality.', DATEADD(DAY, -25, SYSDATETIME())),
    (23, 12, 4, N'Sturdy and well made. Colour is a little lighter than the photo.', DATEADD(DAY, -70, SYSDATETIME())),
    (19, 11, 5, N'Maryam captured the feel of the old city perfectly. It is the centre of our living room now.', DATEADD(DAY, -85, SYSDATETIME())),
    (4, 11, 5, N'Bright and cheerful coasters, cork backing is a nice touch.', DATEADD(DAY, -85, SYSDATETIME())),
    (12, 10, 4, N'Nice stone and the artist adjusted the size for me without any extra charge.', DATEADD(DAY, -95, SYSDATETIME()));

INSERT INTO CustomRequests (UserID, ArtistID, CategoryID, Description, Budget, RequiredBy, QuotedPrice, Status, CreatedAt) VALUES
    (9, 1, 1, N'A set of 6 dinner plates in an earthy glaze with our family initial "K" painted at the rim.', 9000.00, CAST(DATEADD(DAY, 30, SYSDATETIME()) AS DATE), NULL, N'Pending', DATEADD(DAY, -2, SYSDATETIME())),
    (10, 5, 5, N'Surah Al-Fatiha calligraphy in gold leaf on a dark green background, 24 x 36 inches, for our new home.', 15000.00, CAST(DATEADD(DAY, 45, SYSDATETIME()) AS DATE), 18000.00, N'Quoted', DATEADD(DAY, -8, SYSDATETIME())),
    (11, 3, 3, N'Four matching pairs of small silver jhumkas for bridesmaids, simple design without stones.', 16000.00, CAST(DATEADD(DAY, 20, SYSDATETIME()) AS DATE), 17500.00, N'Accepted', DATEADD(DAY, -15, SYSDATETIME())),
    (12, 4, 4, N'An engraved walnut name board for my clinic: "Dr. Usman Tariq - Family Physician", about 24 inches wide.', 6000.00, NULL, 6500.00, N'Completed', DATEADD(DAY, -50, SYSDATETIME())),
    (9, 2, 2, N'A custom ajrak shawl in maroon and mustard tones instead of the usual colours.', 5000.00, CAST(DATEADD(DAY, 25, SYSDATETIME()) AS DATE), NULL, N'Rejected', DATEADD(DAY, -20, SYSDATETIME()));

INSERT INTO CartItems (UserID, ProductID, Quantity, AddedAt) VALUES
    (9, 20, 2, DATEADD(DAY, -1, SYSDATETIME())),
    (9, 11, 1, DATEADD(DAY, -1, SYSDATETIME())),
    (11, 9, 1, DATEADD(DAY, -2, SYSDATETIME()));
GO

/* =====================================================================
   DEMO LOGINS
     Admin    : admin@artisanhub.pk   / Admin@123
     Artist   : sana@artisanhub.pk    / Pass@123   (Clay & Kiln Studio)
     Artist   : kamran@artisanhub.pk  / Pass@123   (waiting for admin verification)
     Customer : ayesha@example.com    / Pass@123
   (all artists: sana, rashid, hina, imran, maryam, ali, kamran @artisanhub.pk;
    all customers: ayesha, bilal, fatima, usman @example.com)
   ===================================================================== */
