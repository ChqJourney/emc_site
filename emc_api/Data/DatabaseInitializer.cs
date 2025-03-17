using Microsoft.Data.Sqlite;
using Dapper;

namespace emc_api.Data
{
    public interface IDatabaseInitializer
    {
        Task InitializeAsync();
    }

    public class DatabaseInitializer : IDatabaseInitializer, IDisposable, IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly string _dbName;
        public DatabaseInitializer(SqliteConnection conn, string dbName)
        {
            _connection = conn;
            _dbName = dbName;
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
            string sql = "";
            switch (_dbName)
            {
                case "User":
                    sql = @"
                CREATE TABLE IF NOT EXISTS Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserName VARCHAR(50) NOT NULL UNIQUE,
    FullName VARCHAR(100),
    MachineName VARCHAR(50),
    Team VARCHAR(50),
    Role VARCHAR(20) NOT NULL CHECK(Role IN ('Engineer', 'Admin', 'Manager','User')) DEFAULT 'User',
    PasswordHash CHAR(60) NOT NULL, -- BCrypt哈希固定长度60
    RefreshToken CHAR(88), -- JWT Refresh Token标准长度
    RefreshTokenExpiryTime number NOT NULL DEFAULT 0, -- JWT Refresh Token过期时间
    CreatedAt DATETIME NOT NULL DEFAULT (datetime('now')),
    LastLoginAt DATETIME,
    IsActive BOOLEAN NOT NULL DEFAULT 1
);
-- 创建索引
CREATE INDEX IF NOT EXISTS IX_Users_UserName ON Users (UserName);
CREATE INDEX IF NOT EXISTS IX_Users_RefreshToken ON Users (RefreshToken);
                ";
                    break;
                case "Biz":
                    sql = @"
                CREATE TABLE IF NOT EXISTS reservations (
	id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    reservation_date varchar(24) NOT NULL,
    time_slot varchar(24) NOT NULL,
    station_id number NOT NULL,
    client_name varchar(256),
    product_name TEXT,
	tests TEXT NOT NULL,
	job_no varchar(128),
  project_engineer varchar(128) NOT NULL,
  testing_engineer varchar(128) NOT NULL,
    purpose_description TEXT,
    contact_name varchar(128),
    contact_phone varchar(128),
    sales varchar(128),
	updated_On datetime DEFAULT (strftime('%Y-%m-%d %H:%M:%f','now','localtime')),
	created_On datetime DEFAULT (datetime('now','localtime')),
    reservate_by varchar(128) NOT NULL,
    reservation_status varchar(128) DEFAULT ('normal')
	);

                CREATE TABLE IF NOT EXISTS stations (
	id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
     name text,
	 short_name text,
    description text,
	photo_path text,
    status text,
	updated_On datetime DEFAULT (strftime('%Y-%m-%d %H:%M:%f','now','localtime')),
	created_On datetime DEFAULT (datetime('now','localtime'))
	);
    CREATE TABLE IF NOT EXISTS visitings  (
	id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
     visit_user text,
	 visit_machine text,
	 visit_count NUMERIC,
		last_visit_time datetime DEFAULT (datetime('now','localtime'))
	);
    CREATE TABLE IF NOT EXISTS s_events (
id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
name text NOT NULL,
from_date text NOT NULL,
to_date text NOT NULL,
station_id INTEGER,
created_On datetime DEFAULT (datetime('now','localtime')),
updated_On datetime DEFAULT (strftime('%Y-%m-%d %H:%M:%f','now','localtime')),
created_By TEXT NOT NULL,
updated_By TEXT
);

    ";
                    break;
            }


            await _connection.ExecuteAsync(sql);
        }

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}

// CREATE TRIGGER UpdateTriggerForReservations AFTER UPDATE on reservations
// for EACH ROW
// BEGIN
//   UPDATE reservations SET updated_On=(strftime('%Y-%m-%d %H:%M:%f','now','localtime')) where id=OLD.Id;
// END;
// CREATE TRIGGER IF NOT EXISTS UpdateTriggerForSevents AFTER UPDATE on s_events
// for EACH ROW
// BEGIN
// UPDATE s_events SET updated_On=(strftime('%Y-%m-%d %H:%M:%f','now','localtime')) where id=OLD.Id;
// END;
// CREATE TRIGGER UpdateTriggerForStations AFTER UPDATE on stations
// for EACH ROW
// BEGIN
//   UPDATE stations SET updated_On=(strftime('%Y-%m-%d %H:%M:%f','now','localtime')) where id=OLD.Id;
// END;