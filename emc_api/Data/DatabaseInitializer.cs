using Microsoft.Data.Sqlite;
using Dapper;

namespace emc_api.Data
{
    public interface IDatabaseInitializer
    {
        Task InitializeAsync();
    }

    public class DatabaseInitializer : IDatabaseInitializer,IDisposable, IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public DatabaseInitializer(SqliteConnection conn)
        {
            _connection = conn;
        }

        public async Task InitializeAsync()
        {
            await _connection.OpenAsync();
            
            var sql = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserName TEXT NOT NULL,
                    MachineName TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    Team TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    LoginAt TEXT NOT NULL,
                    UNIQUE(UserName, MachineName)
                );

                CREATE TABLE IF NOT EXISTS UserActivities (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    ApiUsed TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    FOREIGN KEY(UserId) REFERENCES Users(Id)
                );

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