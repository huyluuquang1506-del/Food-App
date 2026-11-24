/* =====================================================================
   FOODAPP - UNG DUNG DAT DO AN THONG MINH
   Script tao CSDL (SQL Server / LocalDB) - chay MOT LAN tu dau den cuoi.
   Khop voi project ASP.NET MVC "Web-Food-App" (EF6 Code First, chuoi ket noi FoodAppContext).

   LUU Y: script se XOA CSDL FoodApp neu da ton tai roi tao lai tu dau.
   Ket noi: (localdb)\MSSQLLocalDB  (hoac SQL Server khac -> sua Web.config)
   ===================================================================== */
USE master;
GO
IF DB_ID(N'FoodApp') IS NOT NULL
BEGIN
    ALTER DATABASE FoodApp SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE FoodApp;
END
GO
CREATE DATABASE FoodApp COLLATE Vietnamese_CI_AS;
GO
USE FoodApp;
GO

/* =====================================================================
   1. TAO BANG + RANG BUOC
   ===================================================================== */

-- 1.1 Users: nguoi dung (khach hang / quan tri)
CREATE TABLE Users (
    UserId       int IDENTITY(1,1) NOT NULL,
    Username     varchar(50)   NOT NULL,
    PasswordHash varchar(64)   NOT NULL,              -- SHA-256 dang hex IN HOA, khong luu mat khau tho
    FullName     nvarchar(100) NOT NULL,
    Email        varchar(100)  NOT NULL,
    Phone        varchar(15)   NULL,
    Address      nvarchar(255) NULL,
    Role         varchar(20)   NOT NULL CONSTRAINT DF_Users_Role DEFAULT 'Customer',
    CreatedAt    datetime2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSDATETIME(),
    IsActive     bit           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer','Admin'))
);
GO

-- 1.2 Categories: nhom mon an
CREATE TABLE Categories (
    CategoryId   int IDENTITY(1,1) NOT NULL,
    CategoryName nvarchar(100) NOT NULL,
    Description  nvarchar(255) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (CategoryId),
    CONSTRAINT UQ_Categories_Name UNIQUE (CategoryName)
);
GO

-- 1.3 Restaurants: quan an
CREATE TABLE Restaurants (
    RestaurantId   int IDENTITY(1,1) NOT NULL,
    RestaurantName nvarchar(150) NOT NULL,
    Address        nvarchar(255) NOT NULL,
    Phone          varchar(15)   NULL,
    OpenTime       time          NOT NULL CONSTRAINT DF_Restaurants_Open DEFAULT '07:00',
    CloseTime      time          NOT NULL CONSTRAINT DF_Restaurants_Close DEFAULT '22:00',
    IsActive       bit           NOT NULL CONSTRAINT DF_Restaurants_IsActive DEFAULT 1,
    CONSTRAINT PK_Restaurants PRIMARY KEY (RestaurantId)
);
GO

-- 1.4 Foods: mon an
CREATE TABLE Foods (
    FoodId       int IDENTITY(1,1) NOT NULL,
    FoodName     nvarchar(150)  NOT NULL,
    CategoryId   int            NOT NULL,
    RestaurantId int            NOT NULL,
    Price        decimal(18,2)  NOT NULL,
    Stock        int            NOT NULL CONSTRAINT DF_Foods_Stock DEFAULT 0,
    Description  nvarchar(max)  NULL,
    ImageUrl     nvarchar(255)  NULL,
    IsActive     bit            NOT NULL CONSTRAINT DF_Foods_IsActive DEFAULT 1,
    CONSTRAINT PK_Foods PRIMARY KEY (FoodId),
    CONSTRAINT FK_Foods_Categories FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),                         -- NO ACTION: chan xoa nhom dang co mon
    CONSTRAINT FK_Foods_Restaurants FOREIGN KEY (RestaurantId)
        REFERENCES Restaurants(RestaurantId),
    CONSTRAINT CK_Foods_Price CHECK (Price >= 0),
    CONSTRAINT CK_Foods_Stock CHECK (Stock >= 0)
);
GO

-- 1.5 Vouchers: ma giam gia
CREATE TABLE Vouchers (
    VoucherId       int IDENTITY(1,1) NOT NULL,
    Code            varchar(30)   NOT NULL,
    DiscountPercent int           NOT NULL,
    MaxDiscount     decimal(18,2) NOT NULL,
    MinOrderAmount  decimal(18,2) NOT NULL CONSTRAINT DF_Vouchers_Min DEFAULT 0,
    StartDate       datetime2     NOT NULL,
    EndDate         datetime2     NOT NULL,
    Quantity        int           NOT NULL CONSTRAINT DF_Vouchers_Qty DEFAULT 0,
    IsActive        bit           NOT NULL CONSTRAINT DF_Vouchers_IsActive DEFAULT 1,
    CONSTRAINT PK_Vouchers PRIMARY KEY (VoucherId),
    CONSTRAINT UQ_Vouchers_Code UNIQUE (Code),
    CONSTRAINT CK_Vouchers_Percent CHECK (DiscountPercent BETWEEN 1 AND 100),
    CONSTRAINT CK_Vouchers_Max CHECK (MaxDiscount >= 0),
    CONSTRAINT CK_Vouchers_Qty CHECK (Quantity >= 0),
    CONSTRAINT CK_Vouchers_Date CHECK (EndDate > StartDate)
);
GO

-- 1.6 Orders: don hang
CREATE TABLE Orders (
    OrderId         int IDENTITY(1,1) NOT NULL,
    UserId          int           NOT NULL,
    VoucherId       int           NULL,
    OrderDate       datetime2     NOT NULL CONSTRAINT DF_Orders_OrderDate DEFAULT SYSDATETIME(),
    Status          nvarchar(20)  NOT NULL CONSTRAINT DF_Orders_Status DEFAULT N'Pending',
    ShippingAddress nvarchar(255) NOT NULL,
    PaymentMethod   nvarchar(20)  NOT NULL CONSTRAINT DF_Orders_Payment DEFAULT N'COD',
    TotalAmount     decimal(18,2) NOT NULL CONSTRAINT DF_Orders_Total DEFAULT 0,      -- tien hang (trigger tu cap nhat)
    ShippingFee     decimal(18,2) NOT NULL CONSTRAINT DF_Orders_Ship DEFAULT 15000,   -- khop CartHelper.DefaultShippingFee trong code
    DiscountAmount  decimal(18,2) NOT NULL CONSTRAINT DF_Orders_Discount DEFAULT 0,
    FinalAmount     AS (TotalAmount + ShippingFee - DiscountAmount) PERSISTED,        -- tien phai tra
    CONSTRAINT PK_Orders PRIMARY KEY (OrderId),
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_Orders_Vouchers FOREIGN KEY (VoucherId) REFERENCES Vouchers(VoucherId),
    CONSTRAINT CK_Orders_Status CHECK (Status IN (N'Pending',N'Confirmed',N'Delivering',N'Completed',N'Cancelled')),
    CONSTRAINT CK_Orders_Payment CHECK (PaymentMethod IN (N'COD',N'Momo',N'Banking')),
    CONSTRAINT CK_Orders_Amounts CHECK (TotalAmount >= 0 AND ShippingFee >= 0 AND DiscountAmount >= 0)
);
GO

-- 1.7 OrderDetails: bang trung gian Orders - Foods (quan he n-n)
CREATE TABLE OrderDetails (
    OrderDetailId int IDENTITY(1,1) NOT NULL,
    OrderId       int           NOT NULL,
    FoodId        int           NOT NULL,
    Quantity      int           NOT NULL,
    UnitPrice     decimal(18,2) NOT NULL,                  -- gia tai thoi diem dat
    Note          nvarchar(255) NULL,
    CONSTRAINT PK_OrderDetails PRIMARY KEY (OrderDetailId),
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId) ON DELETE CASCADE,       -- xoa don thi xoa chi tiet
    CONSTRAINT FK_OrderDetails_Foods FOREIGN KEY (FoodId)
        REFERENCES Foods(FoodId),                           -- chan xoa mon da co trong don
    CONSTRAINT UQ_OrderDetails_Order_Food UNIQUE (OrderId, FoodId),
    CONSTRAINT CK_OrderDetails_Qty CHECK (Quantity > 0),
    CONSTRAINT CK_OrderDetails_Price CHECK (UnitPrice >= 0)
);
GO

-- 1.8 Reviews: danh gia mon an
CREATE TABLE Reviews (
    ReviewId  int IDENTITY(1,1) NOT NULL,
    FoodId    int           NOT NULL,
    UserId    int           NOT NULL,
    Rating    int           NOT NULL,
    Comment   nvarchar(500) NULL,
    CreatedAt datetime2     NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Reviews PRIMARY KEY (ReviewId),
    CONSTRAINT FK_Reviews_Foods FOREIGN KEY (FoodId) REFERENCES Foods(FoodId) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5),
    CONSTRAINT UQ_Reviews_User_Food UNIQUE (UserId, FoodId)  -- moi nguoi danh gia moi mon 1 lan
);
GO

-- 1.9 Favorites: mon yeu thich (quan he n-n Users - Foods, khoa chinh kep)
CREATE TABLE Favorites (
    UserId  int       NOT NULL,
    FoodId  int       NOT NULL,
    AddedAt datetime2 NOT NULL CONSTRAINT DF_Favorites_AddedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_Favorites PRIMARY KEY (UserId, FoodId),
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    CONSTRAINT FK_Favorites_Foods FOREIGN KEY (FoodId) REFERENCES Foods(FoodId) ON DELETE CASCADE
);
GO

-- 1.10 PriceHistory: nhat ky doi gia (do trigger ghi)
CREATE TABLE PriceHistory (
    HistoryId int IDENTITY(1,1) NOT NULL,
    FoodId    int           NOT NULL,
    OldPrice  decimal(18,2) NOT NULL,
    NewPrice  decimal(18,2) NOT NULL,
    ChangedAt datetime2     NOT NULL CONSTRAINT DF_PriceHistory_ChangedAt DEFAULT SYSDATETIME(),
    CONSTRAINT PK_PriceHistory PRIMARY KEY (HistoryId),
    CONSTRAINT FK_PriceHistory_Foods FOREIGN KEY (FoodId) REFERENCES Foods(FoodId) ON DELETE CASCADE
);
GO

/* =====================================================================
   2. INDEX
   ===================================================================== */
CREATE INDEX IX_Foods_FoodName    ON Foods(FoodName);
CREATE INDEX IX_Foods_CategoryId  ON Foods(CategoryId);
CREATE INDEX IX_Foods_Restaurant  ON Foods(RestaurantId);
CREATE INDEX IX_Orders_UserId     ON Orders(UserId, OrderDate DESC);
CREATE INDEX IX_OrderDetails_Food ON OrderDetails(FoodId);
CREATE INDEX IX_Reviews_FoodId    ON Reviews(FoodId);
GO

/* =====================================================================
   3. TRIGGER
   ===================================================================== */

-- 3.1 Tru ton kho khi them chi tiet don (vuot ton -> CK_Foods_Stock bao loi 547, la hanh vi mong muon)
--     Luu y: viec HUY DON (cong lai kho + tra luot voucher) do code C# (OrderService.Cancel) xu ly.
CREATE TRIGGER trg_OrderDetails_UpdateStock
ON OrderDetails
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE f
    SET f.Stock = f.Stock - s.Qty
    FROM Foods f
    JOIN (SELECT FoodId, SUM(Quantity) AS Qty FROM inserted GROUP BY FoodId) s
         ON f.FoodId = s.FoodId;
END;
GO

-- 3.2 Tu cap nhat Orders.TotalAmount khi them/sua/xoa chi tiet
CREATE TRIGGER trg_OrderDetails_UpdateTotal
ON OrderDetails
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE o
    SET o.TotalAmount = ISNULL((SELECT SUM(d.Quantity * d.UnitPrice)
                                FROM OrderDetails d
                                WHERE d.OrderId = o.OrderId), 0)
    FROM Orders o
    WHERE o.OrderId IN (SELECT OrderId FROM inserted
                        UNION
                        SELECT OrderId FROM deleted);
END;
GO

-- 3.3 Ghi lich su khi doi gia mon (trang Quan tri > Tong quan hien 10 lan doi gia gan nhat)
CREATE TRIGGER trg_Foods_PriceHistory
ON Foods
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF UPDATE(Price)
        INSERT INTO PriceHistory (FoodId, OldPrice, NewPrice)
        SELECT i.FoodId, d.Price, i.Price
        FROM inserted i
        JOIN deleted d ON i.FoodId = d.FoodId
        WHERE i.Price <> d.Price;
END;
GO

/* =====================================================================
   4. VIEW  (code C# anh xa lop FoodDetail vao view nay)
   ===================================================================== */
CREATE VIEW vw_FoodDetails
AS
SELECT f.FoodId, f.FoodName, f.Price, f.Stock, f.ImageUrl, f.IsActive,
       c.CategoryId, c.CategoryName,
       r.RestaurantId, r.RestaurantName,
       AVG(CAST(rv.Rating AS float)) AS AvgRating,
       COUNT(rv.ReviewId)            AS ReviewCount
FROM Foods f
JOIN Categories c   ON f.CategoryId = c.CategoryId
JOIN Restaurants r  ON f.RestaurantId = r.RestaurantId
LEFT JOIN Reviews rv ON rv.FoodId = f.FoodId
GROUP BY f.FoodId, f.FoodName, f.Price, f.Stock, f.ImageUrl, f.IsActive,
         c.CategoryId, c.CategoryName, r.RestaurantId, r.RestaurantName;
GO

/* =====================================================================
   5. STORED PROCEDURE
   ===================================================================== */

-- Kieu bang truyen gio hang vao sp_PlaceOrder (code C# truyen qua DataTable, TypeName = dbo.OrderItemType)
CREATE TYPE dbo.OrderItemType AS TABLE (
    FoodId   int NOT NULL PRIMARY KEY,
    Quantity int NOT NULL CHECK (Quantity > 0)
);
GO

-- 5.1 Dat hang tron goi: don + chi tiet + voucher, trong 1 transaction
--     Ma loi THROW (50001, 50002, 50003) duoc OrderController.TranslateSqlError dich sang tieng Viet.
CREATE PROCEDURE sp_PlaceOrder
    @UserId          int,
    @ShippingAddress nvarchar(255),
    @PaymentMethod   nvarchar(20) = N'COD',
    @VoucherCode     varchar(30)  = NULL,
    @Items           dbo.OrderItemType READONLY,
    @OrderId         int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRY
        BEGIN TRAN;

        IF NOT EXISTS (SELECT 1 FROM @Items)
            THROW 50001, N'Giỏ hàng trống.', 1;

        IF EXISTS (SELECT 1 FROM @Items i
                   LEFT JOIN Foods f ON f.FoodId = i.FoodId AND f.IsActive = 1
                   WHERE f.FoodId IS NULL)
            THROW 50002, N'Có món không tồn tại hoặc đã ngừng bán.', 1;

        INSERT INTO Orders (UserId, ShippingAddress, PaymentMethod, Status)
        VALUES (@UserId, @ShippingAddress, @PaymentMethod, N'Pending');
        SET @OrderId = SCOPE_IDENTITY();

        -- trigger se tru kho va cap nhat TotalAmount
        INSERT INTO OrderDetails (OrderId, FoodId, Quantity, UnitPrice)
        SELECT @OrderId, i.FoodId, i.Quantity, f.Price
        FROM @Items i JOIN Foods f ON f.FoodId = i.FoodId;

        IF @VoucherCode IS NOT NULL
        BEGIN
            DECLARE @VoucherId int, @Percent int, @Max decimal(18,2), @Total decimal(18,2), @Discount decimal(18,2);
            SELECT @Total = TotalAmount FROM Orders WHERE OrderId = @OrderId;

            SELECT @VoucherId = VoucherId, @Percent = DiscountPercent, @Max = MaxDiscount
            FROM Vouchers
            WHERE Code = @VoucherCode AND IsActive = 1 AND Quantity > 0
              AND SYSDATETIME() BETWEEN StartDate AND EndDate
              AND MinOrderAmount <= @Total;

            IF @VoucherId IS NULL
                THROW 50003, N'Voucher không hợp lệ hoặc không đủ điều kiện.', 1;

            SET @Discount = CASE WHEN @Total * @Percent / 100 > @Max THEN @Max ELSE @Total * @Percent / 100 END;

            UPDATE Orders SET VoucherId = @VoucherId, DiscountAmount = @Discount WHERE OrderId = @OrderId;
            UPDATE Vouchers SET Quantity = Quantity - 1 WHERE VoucherId = @VoucherId;
        END

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO

-- 5.2 "Thong minh": goi y mon theo nhom user hay dat, chua tung dat, diem danh gia cao
--     (HomeController goi procedure nay cho khach da dang nhap)
CREATE PROCEDURE sp_RecommendFoods
    @UserId int,
    @Top    int = 5
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top)
           v.FoodId, v.FoodName, v.Price, v.CategoryName, v.RestaurantName,
           ISNULL(v.AvgRating, 0) AS AvgRating, v.ReviewCount
    FROM vw_FoodDetails v
    WHERE v.IsActive = 1 AND v.Stock > 0
      AND v.CategoryId IN (SELECT TOP 3 f2.CategoryId
                           FROM Orders o
                           JOIN OrderDetails d ON o.OrderId = d.OrderId
                           JOIN Foods f2 ON d.FoodId = f2.FoodId
                           WHERE o.UserId = @UserId
                           GROUP BY f2.CategoryId
                           ORDER BY SUM(d.Quantity) DESC)
      AND v.FoodId NOT IN (SELECT d.FoodId
                           FROM Orders o JOIN OrderDetails d ON o.OrderId = d.OrderId
                           WHERE o.UserId = @UserId)
    ORDER BY ISNULL(v.AvgRating, 0) DESC, v.ReviewCount DESC;
END;
GO

/* =====================================================================
   6. DU LIEU MAU
   Mat khau demo: 123456 -> SHA-256 hex in hoa (khop SecurityHelper.HashPassword trong code C#).
   Thuc te nen dung PBKDF2/BCrypt co salt.
   Tai khoan: admin (quan tri) | an, binh, chi, dung, em (khach hang) - mat khau deu la 123456
   ===================================================================== */
INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, Address, Role) VALUES
('admin',  CONVERT(varchar(64), HASHBYTES('SHA2_256','123456'), 2), N'Quản trị hệ thống', 'admin@foodapp.vn', '0900000001', N'1 Nguyễn Huệ, Q1, TP.HCM', 'Admin'),
('an',     CONVERT(varchar(64), HASHBYTES('SHA2_256','123456'), 2), N'Nguyễn Văn An',    'an@gmail.com',    '0900000002', N'12 Lê Lợi, Q1, TP.HCM', 'Customer'),
('binh',   CONVERT(varchar(64), HASHBYTES('SHA2_256','123456'), 2), N'Trần Thị Bình',    'binh@gmail.com',  '0900000003', N'45 Cách Mạng Tháng 8, Q3, TP.HCM', 'Customer'),
('chi',    CONVERT(varchar(64), HASHBYTES('SHA2_256','123456'), 2), N'Lê Minh Chi',      'chi@gmail.com',   '0900000004', N'78 Võ Văn Tần, Q3, TP.HCM', 'Customer'),
('dung',   CONVERT(varchar(64), HASHBYTES('SHA2_256','123456'), 2), N'Phạm Quốc Dũng',   'dung@gmail.com',  '0900000005', N'9 Hoàng Diệu, Q4, TP.HCM', 'Customer'),
('em',     CONVERT(varchar(64), HASHBYTES('SHA2_256','123456'), 2), N'Võ Thanh Em',      'em@gmail.com',    '0900000006', N'33 Nguyễn Trãi, Q5, TP.HCM', 'Customer');

INSERT INTO Categories (CategoryName, Description) VALUES
(N'Cơm',            N'Các món cơm phần'),
(N'Phở & Bún',      N'Phở, bún các loại'),
(N'Bánh mì',        N'Bánh mì nóng giòn'),
(N'Đồ uống',        N'Trà sữa, cà phê, nước ép'),
(N'Pizza & Burger', N'Đồ ăn nhanh kiểu Tây'),
(N'Tráng miệng',    N'Chè, bánh ngọt');

INSERT INTO Restaurants (RestaurantName, Address, Phone, OpenTime, CloseTime) VALUES
(N'Cơm Tấm Sài Gòn',  N'25 Đinh Tiên Hoàng, Q1', '0281111111', '06:00', '21:00'),
(N'Phở Hòa Pasteur',  N'260C Pasteur, Q3',       '0282222222', '06:00', '22:00'),
(N'Bánh Mì Bếp Ong',  N'15 Lý Tự Trọng, Q1',     '0283333333', '05:30', '20:00'),
(N'Tiệm Đồ Uống & Tráng Miệng', N'88 Hai Bà Trưng, Q1', '0284444444', '08:00', '23:00'),
(N'Tiệm Pizza & Burger',  N'101 Nguyễn Thị Minh Khai, Q3', '0285555555', '10:00', '23:00');

-- ImageUrl khop cac file anh trong thu muc Content/img cua project
INSERT INTO Foods (FoodName, CategoryId, RestaurantId, Price, Stock, Description, ImageUrl) VALUES
(N'Cơm tấm sườn bì chả', 1, 1,  45000, 100, N'Sườn nướng, bì, chả trứng', '/Content/img/com-tam.jpg'),
(N'Cơm gà xối mỡ',       1, 1,  50000, 100, N'Gà chiên giòn xối mỡ hành', '/Content/img/com-ga.jpg'),
(N'Phở bò tái',          2, 2,  55000,  80, N'Phở bò tái nạm', '/Content/img/pho-bo.jpg'),
(N'Bún bò Huế',          2, 2,  50000,  80, N'Bún bò cay đậm đà', '/Content/img/bun-bo.jpg'),
(N'Bún thịt nướng',      2, 3,  45000,  80, N'Bún thịt nướng chả giò', '/Content/img/bun-thit-nuong.jpg'),
(N'Bánh mì thịt nướng',  3, 3,  25000, 150, N'Thịt nướng, rau, dưa leo', '/Content/img/banh-mi-tn.jpg'),
(N'Bánh mì chả cá',      3, 3,  22000, 150, N'Chả cá chiên nóng', '/Content/img/banh-mi-ca.jpg'),
(N'Trà sữa trân châu',   4, 4,  35000, 200, N'Trà sữa trân châu đường đen', '/Content/img/tra-sua.jpg'),
(N'Cà phê sữa đá',       4, 4,  25000, 200, N'Cà phê phin truyền thống', '/Content/img/ca-phe.jpg'),
(N'Nước cam ép',         4, 4,  30000, 120, N'Cam vắt tươi', '/Content/img/nuoc-cam.jpg'),
(N'Pizza hải sản',       5, 5, 129000,  50, N'Pizza size M phô mai tôm mực', '/Content/img/pizza.jpg'),
(N'Burger bò phô mai',   5, 5,  69000,  60, N'Bò nướng, phô mai cheddar', '/Content/img/burger.jpg'),
(N'Gà rán giòn',         5, 5,  59000,  60, N'3 miếng gà rán', '/Content/img/ga-ran.jpg'),
(N'Chè Thái',            6, 4,  28000,  90, N'Chè Thái trái cây', '/Content/img/che-thai.jpg'),
(N'Bánh flan',           6, 4,  18000, 100, N'Flan caramel', '/Content/img/flan.png');

-- Voucher co hieu luc den het nam 2027 de dung duoc ngay (co the sua trong Quan tri > Voucher)
INSERT INTO Vouchers (Code, DiscountPercent, MaxDiscount, MinOrderAmount, StartDate, EndDate, Quantity) VALUES
('FOOD10',   10,  20000,  50000, '2026-01-01', '2027-12-31', 100),
('FREESHIP',100,  15000,  80000, '2026-01-01', '2027-12-31',  50),
('WELCOME', 20,  30000,  60000, '2026-01-01', '2027-12-31', 200),
('LUNCH15', 15,  25000, 100000, '2026-06-01', '2027-12-31',  80),
('WEEKEND', 25,  50000, 150000, '2026-06-01', '2027-12-31',  30);

-- Don hang (OrderId 1..7 do IDENTITY). Cac cot tien: TotalAmount do trigger tinh.
INSERT INTO Orders (UserId, VoucherId, OrderDate, Status, ShippingAddress, PaymentMethod, DiscountAmount) VALUES
(2, NULL, '2026-06-05 11:45', N'Completed',  N'12 Lê Lợi, Q1, TP.HCM',  N'COD',     0),
(3, NULL, '2026-06-18 18:20', N'Completed',  N'45 Cách Mạng Tháng 8, Q3', N'Momo',   0),
(2, 1,    '2026-07-09 12:10', N'Completed',  N'12 Lê Lợi, Q1, TP.HCM',  N'Banking', 15500),
(4, NULL, '2026-08-14 19:30', N'Completed',  N'78 Võ Văn Tần, Q3',      N'COD',     0),
(5, NULL, '2026-09-20 13:00', N'Delivering', N'9 Hoàng Diệu, Q4',       N'Momo',    0),
(3, NULL, '2026-10-12 20:15', N'Pending',    N'45 Cách Mạng Tháng 8, Q3', N'COD',    0),
(2, NULL, '2026-10-25 12:00', N'Completed',  N'12 Lê Lợi, Q1, TP.HCM',  N'COD',     0);

INSERT INTO OrderDetails (OrderId, FoodId, Quantity, UnitPrice, Note) VALUES
(1, 1,  2,  45000, N'Ít cơm'),
(1, 8,  1,  35000, NULL),
(2, 3,  1,  55000, NULL),
(2, 9,  2,  25000, N'Ít đường'),
(2, 14, 1,  28000, NULL),
(3, 1,  1,  45000, NULL),
(3, 2,  1,  50000, NULL),
(3, 10, 2,  30000, NULL),
(4, 11, 1, 129000, NULL),
(4, 12, 2,  69000, NULL),
(4, 8,  2,  35000, N'Thêm trân châu'),
(5, 6,  2,  25000, NULL),
(5, 9,  2,  25000, NULL),
(6, 4,  1,  50000, N'Không hành'),
(6, 5,  1,  45000, NULL),
(7, 13, 2,  59000, NULL),
(7, 15, 2,  18000, NULL),
(7, 8,  1,  35000, NULL);

INSERT INTO Reviews (FoodId, UserId, Rating, Comment) VALUES
(1,  2, 5, N'Sườn nướng thơm, giao nhanh'),
(8,  2, 4, N'Trà sữa ngon, hơi ngọt'),
(3,  3, 5, N'Nước phở đậm vị'),
(9,  3, 4, N'Cà phê đậm'),
(11, 4, 5, N'Pizza nhiều topping'),
(12, 4, 4, N'Burger vừa ăn'),
(2,  2, 4, N'Gà giòn'),
(6,  5, 5, N'Bánh mì nóng giòn'),
(1,  3, 5, N'Cơm ngon, đáng tiền');

INSERT INTO Favorites (UserId, FoodId) VALUES
(2,1),(2,8),(3,3),(3,9),(4,11),(5,6),(6,15);

-- Doi gia vai mon de trigger ghi PriceHistory
UPDATE Foods SET Price = 47000 WHERE FoodId = 1;
UPDATE Foods SET Price = 52000 WHERE FoodId = 2;
UPDATE Foods SET Price = 57000 WHERE FoodId = 3;
UPDATE Foods SET Price = 36000 WHERE FoodId = 8;
UPDATE Foods SET Price = 135000 WHERE FoodId = 11;
GO

/* =====================================================================
   7. KIEM TRA NHANH (chi doc, khong thay doi du lieu)
   Ket qua mong doi: Users 6 | Categories 6 | Restaurants 5 | Foods 15 | Vouchers 5
                     Orders 7 | OrderDetails 18 | Reviews 9 | Favorites 7 | PriceHistory 5
   ===================================================================== */
SELECT N'Users' AS TableName, COUNT(*) AS Total FROM Users
UNION ALL SELECT N'Categories',   COUNT(*) FROM Categories
UNION ALL SELECT N'Restaurants',  COUNT(*) FROM Restaurants
UNION ALL SELECT N'Foods',        COUNT(*) FROM Foods
UNION ALL SELECT N'Vouchers',     COUNT(*) FROM Vouchers
UNION ALL SELECT N'Orders',       COUNT(*) FROM Orders
UNION ALL SELECT N'OrderDetails', COUNT(*) FROM OrderDetails
UNION ALL SELECT N'Reviews',      COUNT(*) FROM Reviews
UNION ALL SELECT N'Favorites',    COUNT(*) FROM Favorites
UNION ALL SELECT N'PriceHistory', COUNT(*) FROM PriceHistory;
GO

/* =====================================================================
   8. CAU TRUY VAN MAU (de trong comment de khong lam doi du lieu khi chay script;
      boi den tung doan roi chay rieng neu muon thu - tuong ung LINQ trong cac Controller)
   =====================================================================

-- Q1. Mon an theo nhom (HomeController.Index)
DECLARE @categoryId int = 4;
SELECT f.FoodId, f.FoodName, r.RestaurantName, c.CategoryName, f.Price
FROM Foods f
JOIN Categories c  ON f.CategoryId = c.CategoryId
JOIN Restaurants r ON f.RestaurantId = r.RestaurantId
WHERE c.CategoryId = @categoryId AND f.IsActive = 1
ORDER BY f.FoodName;

-- Q2. Tim kiem + phan trang (HomeController.Index)
DECLARE @keyword nvarchar(100) = N'bún', @page int = 1, @pageSize int = 9;
SELECT f.FoodId, f.FoodName, f.Price
FROM Foods f
WHERE f.IsActive = 1 AND f.FoodName LIKE N'%' + @keyword + N'%'
ORDER BY f.FoodId
OFFSET (@page - 1) * @pageSize ROWS FETCH NEXT @pageSize ROWS ONLY;

-- Q3. Lich su don hang cua mot nguoi dung (OrderController.History)
DECLARE @userId int = 2;
SELECT o.OrderId, o.OrderDate, o.Status, f.FoodName, d.Quantity, d.UnitPrice,
       d.Quantity * d.UnitPrice AS LineTotal
FROM Orders o
JOIN OrderDetails d ON o.OrderId = d.OrderId
JOIN Foods f        ON d.FoodId = f.FoodId
WHERE o.UserId = @userId
ORDER BY o.OrderDate DESC;

-- Q4. Doanh thu theo thang (AdminController.Dashboard)
SELECT YEAR(o.OrderDate) AS Nam, MONTH(o.OrderDate) AS Thang,
       SUM(d.Quantity * d.UnitPrice) AS DoanhThu
FROM Orders o
JOIN OrderDetails d ON o.OrderId = d.OrderId
WHERE o.Status = N'Completed'
GROUP BY YEAR(o.OrderDate), MONTH(o.OrderDate)
ORDER BY Nam, Thang;

-- Q5. Top 5 mon ban chay (AdminController.Dashboard, bo qua don da huy)
SELECT TOP 5 f.FoodId, f.FoodName, SUM(d.Quantity) AS TongSoLuong
FROM OrderDetails d
JOIN Orders o ON d.OrderId = o.OrderId
JOIN Foods f  ON d.FoodId = f.FoodId
WHERE o.Status <> N'Cancelled'
GROUP BY f.FoodId, f.FoodName
ORDER BY TongSoLuong DESC;

-- Q6. Diem danh gia trung binh moi mon (view vw_FoodDetails)
SELECT FoodId, FoodName, AvgRating, ReviewCount FROM vw_FoodDetails;

-- Q7. Dat hang bang stored procedure (kem voucher) - CO THAY DOI DU LIEU (tru kho, tru luot voucher)
DECLARE @items dbo.OrderItemType, @newOrderId int;
INSERT INTO @items (FoodId, Quantity) VALUES (1, 2), (8, 1);
EXEC sp_PlaceOrder @UserId = 6, @ShippingAddress = N'33 Nguyễn Trãi, Q5',
     @PaymentMethod = N'Momo', @VoucherCode = 'FOOD10', @Items = @items, @OrderId = @newOrderId OUTPUT;
SELECT * FROM Orders WHERE OrderId = @newOrderId;

-- Q8. Goi y mon cho user 2 (HomeController.Index)
EXEC sp_RecommendFoods @UserId = 2, @Top = 4;

-- Q9. Xem lich su gia (do trigger ghi)
SELECT * FROM PriceHistory;

*/
