using Microsoft.Data.Sqlite;

namespace Backend;

/// <summary>
/// Handles database schema migrations and versioning
/// </summary>
public class DatabaseMigrations
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<DatabaseMigrations> _logger;

    public DatabaseMigrations(SqliteConnection connection, ILogger<DatabaseMigrations> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    /// <summary>
    /// Run all pending migrations
    /// </summary>
    public async Task MigrateAsync()
    {
        await EnsureMigrationTableExistsAsync();
        var currentVersion = await GetCurrentVersionAsync();
        
        _logger.LogInformation("Current database version: {Version}", currentVersion);

        var migrations = GetMigrations();
        
        foreach (var migration in migrations.Where(m => m.Version > currentVersion).OrderBy(m => m.Version))
        {
            _logger.LogInformation("Applying migration {Version}: {Name}", migration.Version, migration.Name);
            
            try
            {
                using var con = new SqliteConnection(_connection.ConnectionString);
                await con.OpenAsync();
                using var tx = con.BeginTransaction();
                
                await ExecuteMigrationAsync(con, migration.Sql);
                await UpdateVersionAsync(con, migration.Version, migration.Name);
                
                tx.Commit();
                _logger.LogInformation("Migration {Version} applied successfully", migration.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply migration {Version}", migration.Version);
                throw;
            }
        }
    }

    private async Task EnsureMigrationTableExistsAsync()
    {
        using var con = new SqliteConnection(_connection.ConnectionString);
        await con.OpenAsync();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS __migrations (
                version INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                applied_at TEXT NOT NULL
            )";
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> GetCurrentVersionAsync()
    {
        using var con = new SqliteConnection(_connection.ConnectionString);
        await con.OpenAsync();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM __migrations";
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task ExecuteMigrationAsync(SqliteConnection con, string sql)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task UpdateVersionAsync(SqliteConnection con, int version, string name)
    {
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO __migrations (version, name, applied_at)
            VALUES ($version, $name, $time)";
        cmd.Parameters.AddWithValue("$version", version);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$time", DateTimeOffset.UtcNow.ToString("O"));
        await cmd.ExecuteNonQueryAsync();
    }

    private List<Migration> GetMigrations()
    {
        return new List<Migration>
        {
            new Migration
            {
                Version = 1,
                Name = "Initial Schema",
                Sql = @"
                    CREATE TABLE IF NOT EXISTS invoices (
                        id TEXT PRIMARY KEY,
                        customer TEXT NOT NULL,
                        amount REAL NOT NULL,
                        dueDate TEXT NOT NULL,
                        paid INTEGER NOT NULL DEFAULT 0
                    );
                    
                    CREATE TABLE IF NOT EXISTS jobs (
                        id TEXT PRIMARY KEY,
                        title TEXT NOT NULL,
                        status TEXT,
                        quotedPrice REAL
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_invoices_dueDate ON invoices(dueDate);
                    CREATE INDEX IF NOT EXISTS idx_invoices_paid ON invoices(paid);
                    CREATE INDEX IF NOT EXISTS idx_jobs_status ON jobs(status);
                "
            },
            new Migration
            {
                Version = 2,
                Name = "Add Clients Table",
                Sql = @"
                    CREATE TABLE IF NOT EXISTS clients (
                        id TEXT PRIMARY KEY,
                        name TEXT NOT NULL,
                        email TEXT,
                        phone TEXT,
                        address TEXT,
                        created_at TEXT NOT NULL,
                        updated_at TEXT NOT NULL
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_clients_name ON clients(name);
                    CREATE INDEX IF NOT EXISTS idx_clients_email ON clients(email);
                "
            },
            new Migration
            {
                Version = 3,
                Name = "Add Expenses Table",
                Sql = @"
                    CREATE TABLE IF NOT EXISTS expenses (
                        id TEXT PRIMARY KEY,
                        description TEXT NOT NULL,
                        amount REAL NOT NULL,
                        category TEXT,
                        expense_date TEXT NOT NULL,
                        created_at TEXT NOT NULL
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_expenses_date ON expenses(expense_date);
                    CREATE INDEX IF NOT EXISTS idx_expenses_category ON expenses(category);
                "
            },
            new Migration
            {
                Version = 4,
                Name = "Add Quotes Table",
                Sql = @"
                    CREATE TABLE IF NOT EXISTS quotes (
                        id TEXT PRIMARY KEY,
                        quote_number TEXT NOT NULL,
                        client_id TEXT,
                        title TEXT NOT NULL,
                        amount REAL NOT NULL,
                        status TEXT NOT NULL,
                        valid_until TEXT,
                        created_at TEXT NOT NULL,
                        updated_at TEXT NOT NULL,
                        FOREIGN KEY (client_id) REFERENCES clients(id)
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_quotes_status ON quotes(status);
                    CREATE INDEX IF NOT EXISTS idx_quotes_client ON quotes(client_id);
                    CREATE UNIQUE INDEX IF NOT EXISTS idx_quotes_number ON quotes(quote_number);
                "
            },
            new Migration
            {
                Version = 5,
                Name = "Add User Preferences",
                Sql = @"
                    CREATE TABLE IF NOT EXISTS user_preferences (
                        key TEXT PRIMARY KEY,
                        value TEXT NOT NULL,
                        updated_at TEXT NOT NULL
                    );
                "
            }
        };
    }

    private class Migration
    {
        public int Version { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sql { get; set; } = string.Empty;
    }
}
