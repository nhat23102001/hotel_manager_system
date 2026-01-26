CREATE DATABASE HotelManagementDB;
GO
USE HotelManagementDB;
GO

-- đăng nhâp - role
CREATE TABLE Users (
    Id INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL, -- Admin | Manager | Client
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

-- thông tin cá nhân
CREATE TABLE UserProfiles (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL,
    FullName NVARCHAR(150),
    PhoneNumber NVARCHAR(20),
    Email NVARCHAR(150),
    Address NVARCHAR(255),
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

-- loại phòng
CREATE TABLE RoomTypes (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255),
    BasePrice DECIMAL(18,2),
    MaxPeople INT,
    IsActive BIT DEFAULT 1
);
GO

-- phòng
CREATE TABLE Rooms (
    Id INT IDENTITY PRIMARY KEY,
    RoomCode NVARCHAR(50) NOT NULL UNIQUE,
    RoomName NVARCHAR(150),
    RoomTypeId INT NOT NULL,
    PricePerNight DECIMAL(18,2) NOT NULL,
    MaxPeople INT,
    Status NVARCHAR(20) DEFAULT 'Available', -- Available | Maintenance
    Description NVARCHAR(MAX),
    ImageUrl NVARCHAR(255),
    IsActive BIT DEFAULT 1,
    FOREIGN KEY (RoomTypeId) REFERENCES RoomTypes(Id)
);
GO

-- tin tức
CREATE TABLE Blogs (
    Id INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Slug NVARCHAR(255),
    Summary NVARCHAR(500),
    Content NVARCHAR(MAX),
    Thumbnail NVARCHAR(255),
    AuthorId INT,
    IsPublished BIT DEFAULT 1,
    PublishedAt DATETIME,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (AuthorId) REFERENCES Users(Id)
);
GO

-- đặt phòng
CREATE TABLE Bookings (
    Id INT IDENTITY PRIMARY KEY,
    BookingCode NVARCHAR(50) NOT NULL UNIQUE,

    UserId INT NOT NULL, 
    GuestName NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(20),
    Email NVARCHAR(150),
    Address NVARCHAR(255),

    BookingDate DATETIME DEFAULT GETDATE(),
    CheckInDate DATETIME NOT NULL,
    CheckOutDate DATETIME NOT NULL,

    TotalNights INT NOT NULL,

    SubTotal DECIMAL(18,2) NOT NULL,
    VAT DECIMAL(18,2) NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,

    Status NVARCHAR(20) DEFAULT 'Pending', -- Pending | Confirmed | Cancelled
    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

-- danh sách phòng được book
CREATE TABLE BookingDetails (
    Id INT IDENTITY PRIMARY KEY,
    BookingId INT NOT NULL,
    RoomId INT NOT NULL,
    PricePerNight DECIMAL(18,2) NOT NULL,

    FOREIGN KEY (BookingId) REFERENCES Bookings(Id),
    FOREIGN KEY (RoomId) REFERENCES Rooms(Id)
);
GO

-- liên hệ
CREATE TABLE Contacts (
    Id INT IDENTITY PRIMARY KEY,

    Name NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,

    Status NVARCHAR(20) DEFAULT 'New', 
    -- New | Replied | Closed

    ReplyContent NVARCHAR(MAX),
    RepliedBy INT NULL,
    RepliedAt DATETIME NULL,

    CreatedAt DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (RepliedBy) REFERENCES Users(Id)
);


-- loại dịch vụ 
CREATE TABLE ServiceTypes (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);


-- danh sách dịch vụ 
CREATE TABLE Services (
    Id INT IDENTITY PRIMARY KEY,
    ServiceTypeId INT NOT NULL,

    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(255),

    UnitPrice DECIMAL(18,2) NOT NULL,
    Unit NVARCHAR(50), 
    -- hour | combo | time | item

    IsActive BIT DEFAULT 1,

    FOREIGN KEY (ServiceTypeId) REFERENCES ServiceTypes(Id)
);


-- dịch vụ đi kèm 
CREATE TABLE BookingServices (
    Id INT IDENTITY PRIMARY KEY,

    BookingId INT NOT NULL,
    ServiceId INT NOT NULL,

    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,

    FOREIGN KEY (BookingId) REFERENCES Bookings(Id),
    FOREIGN KEY (ServiceId) REFERENCES Services(Id)
);