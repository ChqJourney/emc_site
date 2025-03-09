CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName VARCHAR(50) NOT NULL UNIQUE,
    FullName VARCHAR(100),
    MachineName VARCHAR(50),
    Team VARCHAR(50),
    Role VARCHAR(20) NOT NULL CHECK(Role IN ('User', 'Admin', 'Manager')) DEFAULT 'User',
    PasswordHash CHAR(60) NOT NULL, -- BCrypt哈希固定长度60
    RefreshToken CHAR(88), -- JWT Refresh Token标准长度
    RefreshTokenExpiryTime number NOT NULL DEFAULT 0, -- JWT Refresh Token过期时间
    CreatedAt DATETIME NOT NULL DEFAULT (datetime('now')),
    LastLoginAt DATETIME,
    IsActive BOOLEAN NOT NULL DEFAULT 1
);
-- 创建索引
CREATE INDEX IX_Users_UserName ON Users (UserName);
CREATE INDEX IX_Users_RefreshToken ON Users (RefreshToken);