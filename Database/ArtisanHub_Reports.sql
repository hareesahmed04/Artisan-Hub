/* =====================================================================
   ArtisanHub - Sample report queries (run after ArtisanHub_Database.sql)
   These show the aggregation, JOIN and GROUP BY work the schema supports.
   ===================================================================== */
USE ArtisanHubDB;
GO

-- 1. Order totals (computed by the view, nothing stored)
SELECT * FROM vw_OrderTotals ORDER BY OrderDate DESC;

-- 2. Total sales per artist
SELECT StoreName, CityName, OrdersCount, ItemsSold, TotalSales
FROM vw_SalesByArtist
ORDER BY TotalSales DESC;

-- 3. Total sales per category
SELECT CategoryName, ProductCount, ItemsSold, TotalSales
FROM vw_SalesByCategory
ORDER BY TotalSales DESC;

-- 4. Total sales per city (city of the artist)
SELECT CityName, Province, ArtistCount, ItemsSold, TotalSales
FROM vw_SalesByCity
ORDER BY TotalSales DESC;

-- 5. Top 5 best-selling products
SELECT TOP 5 p.Title, a.StoreName, SUM(oi.Quantity) AS UnitsSold,
       SUM(oi.Quantity * oi.UnitPrice) AS Revenue
FROM OrderItems oi
JOIN Orders o   ON o.OrderID   = oi.OrderID
JOIN Products p ON p.ProductID = oi.ProductID
JOIN Artists a  ON a.ArtistID  = p.ArtistID
WHERE o.Status <> 'Cancelled'
GROUP BY p.Title, a.StoreName
ORDER BY UnitsSold DESC, Revenue DESC;

-- 6. Monthly sales for the whole marketplace
SELECT FORMAT(o.OrderDate, 'yyyy-MM') AS [Month],
       COUNT(DISTINCT o.OrderID)       AS Orders,
       SUM(oi.Quantity * oi.UnitPrice) AS Sales
FROM Orders o
JOIN OrderItems oi ON oi.OrderID = o.OrderID
WHERE o.Status <> 'Cancelled'
GROUP BY FORMAT(o.OrderDate, 'yyyy-MM')
ORDER BY [Month];

-- 7. Customers ranked by total spending
SELECT u.FullName, u.Email, COUNT(t.OrderID) AS Orders, SUM(t.TotalAmount) AS TotalSpent
FROM vw_OrderTotals t
JOIN Users u ON u.UserID = t.UserID
WHERE t.Status <> 'Cancelled'
GROUP BY u.FullName, u.Email
ORDER BY TotalSpent DESC;

-- 8. Products with an average rating of 4 or more
SELECT p.Title, r.AvgRating, r.ReviewCount
FROM vw_ProductRatings r
JOIN Products p ON p.ProductID = r.ProductID
WHERE r.AvgRating >= 4
ORDER BY r.AvgRating DESC, r.ReviewCount DESC;

-- 9. Low-stock products (3 or fewer left)
SELECT a.StoreName, p.Title, p.StockQty
FROM Products p
JOIN Artists a ON a.ArtistID = p.ArtistID
WHERE p.StockQty <= 3 AND p.IsActive = 1
ORDER BY p.StockQty;

-- 10. Shipments still waiting to be sent, per artist
SELECT a.StoreName, COUNT(*) AS WaitingShipments
FROM Shipments s
JOIN Artists a ON a.ArtistID = s.ArtistID
WHERE s.Status = 'Preparing'
GROUP BY a.StoreName
ORDER BY WaitingShipments DESC;

-- 11. Custom requests by status
SELECT Status, COUNT(*) AS Requests, AVG(QuotedPrice) AS AvgQuote
FROM CustomRequests
GROUP BY Status;

-- 12. Artists waiting for admin verification
SELECT a.StoreName, u.FullName, u.Email, c.CityName, a.JoinDate
FROM Artists a
JOIN Users u  ON u.UserID = a.UserID
JOIN Cities c ON c.CityID = a.CityID
WHERE a.IsVerified = 0;

-- 13. Trigger test: this insert fails because only a few units are in stock
-- BEGIN TRAN;
--   INSERT INTO OrderItems (OrderID, ProductID, Quantity, UnitPrice) VALUES (8, 17, 999, 8500);
-- ROLLBACK;
