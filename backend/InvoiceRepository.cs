using Microsoft.Data.Sqlite;
using System.Data;

public sealed class InvoiceRepository
{
    private readonly SqliteConnection _conn;
    public InvoiceRepository(SqliteConnection conn)
    {
        _conn = conn;
        EnsureSchema();
    }

    public async Task<bool> MarkInvoicePaidAsync(string id, DateOnly paidDate)
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        await con.OpenAsync();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "UPDATE invoices SET status='paid', paidDate=$paid WHERE id=$id";
        cmd.Parameters.AddWithValue("$paid", paidDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$id", id);
        var n = await cmd.ExecuteNonQueryAsync();
        return n > 0;
    }

    public async Task<bool> AddPaymentAsync(string invoiceId, decimal amount, DateOnly paidAt)
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        await con.OpenAsync();
        using var tx = con.BeginTransaction();
        // insert payment
        using (var ins = con.CreateCommand())
        {
            ins.CommandText = "INSERT INTO payments(id, invoiceId, amount, paidAt) VALUES($id,$invoiceId,$amount,$paidAt)";
            ins.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
            ins.Parameters.AddWithValue("$invoiceId", invoiceId);
            ins.Parameters.AddWithValue("$amount", amount);
            ins.Parameters.AddWithValue("$paidAt", paidAt.ToString("yyyy-MM-dd"));
            await ins.ExecuteNonQueryAsync();
        }
        // mark invoice paid
        using (var up = con.CreateCommand())
        {
            up.CommandText = "UPDATE invoices SET status='paid', paidDate=$paid WHERE id=$id";
            up.Parameters.AddWithValue("$paid", paidAt.ToString("yyyy-MM-dd"));
            up.Parameters.AddWithValue("$id", invoiceId);
            await up.ExecuteNonQueryAsync();
        }
        tx.Commit();
        return true;
    }

    private void EnsureSchema()
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS invoices(
  id TEXT PRIMARY KEY,
  customer TEXT NOT NULL,
  amount REAL NOT NULL,
  status TEXT NOT NULL,
  issueDate TEXT,
  dueDate TEXT,
  paidDate TEXT
);
CREATE INDEX IF NOT EXISTS idx_invoices_due ON invoices(dueDate);
CREATE INDEX IF NOT EXISTS idx_invoices_status ON invoices(status);

CREATE TABLE IF NOT EXISTS payments(
  id TEXT PRIMARY KEY,
  invoiceId TEXT NOT NULL,
  amount REAL NOT NULL,
  paidAt TEXT NOT NULL,
  FOREIGN KEY(invoiceId) REFERENCES invoices(id)
);
CREATE INDEX IF NOT EXISTS idx_payments_paidAt ON payments(paidAt);
";
        cmd.ExecuteNonQuery();
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(string? status = null)
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        await con.OpenAsync();
        using var cmd = con.CreateCommand();
        if (!string.IsNullOrWhiteSpace(status))
        {
            cmd.CommandText = "SELECT id, customer, amount, status, issueDate, dueDate, paidDate FROM invoices WHERE status = $st ORDER BY COALESCE(dueDate, issueDate) DESC";
            cmd.Parameters.AddWithValue("$st", status);
        }
        else
        {
            cmd.CommandText = "SELECT id, customer, amount, status, issueDate, dueDate, paidDate FROM invoices ORDER BY COALESCE(dueDate, issueDate) DESC";
        }
        var list = new List<InvoiceDto>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetString(0);
            var customer = reader.GetString(1);
            var amount = Convert.ToDecimal(reader.GetDouble(2));
            var st = reader.GetString(3);
            var issue = ParseDateOnly(reader.IsDBNull(4) ? null : reader.GetString(4));
            var due = ParseDateOnly(reader.IsDBNull(5) ? null : reader.GetString(5));
            var paid = ParseDateOnly(reader.IsDBNull(6) ? null : reader.GetString(6));
            list.Add(new InvoiceDto(id, customer, amount, st, issue, due, paid));
        }
        return list;
    }

    public async Task<List<InvoiceRow>> GetUnpaidInvoicesAsync()
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        await con.OpenAsync();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT id, customer, amount, dueDate FROM invoices WHERE status = 'unpaid' OR (status <> 'paid' AND (paidDate IS NULL OR paidDate = ''))";
        var list = new List<InvoiceRow>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetString(0);
            var customer = reader.GetString(1);
            var amount = Convert.ToDecimal(reader.GetDouble(2));
            var dueStr = reader.IsDBNull(3) ? null : reader.GetString(3);
            var due = ParseDateOnly(dueStr) ?? DateOnly.FromDateTime(DateTime.Today);
            list.Add(new InvoiceRow(id, customer, amount, due));
        }
        return list;
    }

    public async Task<decimal> GetPaidLastDaysAsync(int days)
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        await con.OpenAsync();
        using var cmd = con.CreateCommand();
        var since = DateOnly.FromDateTime(DateTime.Today).AddDays(-days).ToString("yyyy-MM-dd");
        cmd.CommandText = @"SELECT 
            COALESCE((SELECT SUM(amount) FROM payments WHERE paidAt >= $since),
                     (SELECT SUM(amount) FROM invoices WHERE status='paid' AND paidDate >= $since),
                     0)";
        cmd.Parameters.AddWithValue("$since", since);
        var sum = await cmd.ExecuteScalarAsync();
        if (sum is double d) return Convert.ToDecimal(d);
        if (sum is long l) return l;
        if (sum is decimal m) return m;
        if (sum is null) return 0m;
        return Convert.ToDecimal(sum);
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(string id)
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        await con.OpenAsync();
        using var cmd = con.CreateCommand();
        cmd.CommandText = "SELECT id, customer, amount, status, issueDate, dueDate, paidDate FROM invoices WHERE id = $id LIMIT 1";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var customer = reader.GetString(1);
            var amount = Convert.ToDecimal(reader.GetDouble(2));
            var st = reader.GetString(3);
            var issue = ParseDateOnly(reader.IsDBNull(4) ? null : reader.GetString(4));
            var due = ParseDateOnly(reader.IsDBNull(5) ? null : reader.GetString(5));
            var paid = ParseDateOnly(reader.IsDBNull(6) ? null : reader.GetString(6));
            return new InvoiceDto(id, customer, amount, st, issue, due, paid);
        }
        return null;
    }

    public async Task SeedIfEmptyAsync()
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        await con.OpenAsync();
        using (var check = con.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(1) FROM invoices";
            var count = Convert.ToInt32(await check.ExecuteScalarAsync());
            if (count > 0) return;
        }
        var rng = new Random();
        var today = DateOnly.FromDateTime(DateTime.Today);
        using var tx = con.BeginTransaction();

        // Realistic customer pool (SMB-like)
        var customers = new string[]
        {
            "Acme Builders", "Northwind Traders", "Globex Corp", "Initech Ltd",
            "Stark Industries", "Wayne Enterprises", "Wonka Works", "Pied Piper",
            "Hooli", "Umbrella Services", "Dunder Mifflin", "Vandelay Imports",
            "Soylent Foods", "Tyrell Analytics", "Blue Sun Logistics", "Aperture Labs",
            "Cyberdyne Systems", "Oceanic Air", "Gringotts Banking", "Monarch Studio",
            "Black Mesa", "Octan Energy", "Gekko Capital", "Paper Street Co",
            "Prestige Worldwide", "Massive Dynamic", "Nakatomi Trading", "Initrode",
            "Sterling Cooper", "Bluth Company"
        };

        // Determine total invoices target between 250–450 (configurable via env)
        int minInv = 250, maxInv = 450;
        if (int.TryParse(Environment.GetEnvironmentVariable("INVOICE_SEED_MIN"), out var envMin) && envMin > 0)
            minInv = envMin;
        if (int.TryParse(Environment.GetEnvironmentVariable("INVOICE_SEED_MAX"), out var envMax) && envMax >= minInv)
            maxInv = envMax;
        int totalTarget = rng.Next(minInv, maxInv + 1);

        // Distribute invoices across customers (2–8 per customer, capped by target)
        int created = 0;
        while (created < totalTarget)
        {
            foreach (var cust in customers)
            {
                int perCustomer = rng.Next(2, 9); // 2–8 invoices per customer
                for (int j = 0; j < perCustomer && created < totalTarget; j++)
                {
                    var id = Guid.NewGuid().ToString("N");

                    // Weighted amount tiers (skewed towards small/mid invoices)
                    decimal amount;
                    var tier = rng.NextDouble();
                    if (tier < 0.60) amount = Math.Round((decimal)rng.Next(100, 900) + (decimal)rng.NextDouble(), 2);          // small
                    else if (tier < 0.90) amount = Math.Round((decimal)rng.Next(900, 5000) + (decimal)rng.NextDouble(), 2);    // mid
                    else amount = Math.Round((decimal)rng.Next(5000, 20000) + (decimal)rng.NextDouble(), 2);                   // large

                    // Dates: issue within last ~120 days, due 14–45 days after issue
                    var issue = today.AddDays(-rng.Next(1, 120));
                    var due = issue.AddDays(rng.Next(14, 46));

                    // Status mix: 50–65% paid, 15–25% overdue, rest unpaid/upcoming
                    DateOnly? paid = null;
                    var roll = rng.NextDouble();
                    if (roll < 0.575) // paid
                    {
                        // paid sometime between issue and today; may be slightly after due
                        var paidCandidate = issue.AddDays(rng.Next(1, Math.Max(2, (today.DayNumber - issue.DayNumber))));
                        if (paidCandidate > today) paidCandidate = today;
                        paid = paidCandidate;
                    }
                    else if (roll < 0.575 + 0.20) // overdue (~20%)
                    {
                        // ensure due is before today
                        if (due >= today) due = today.AddDays(-rng.Next(1, 15));
                    }
                    else
                    {
                        // unpaid upcoming: push due to be in future (1–30 days)
                        if (due <= today) due = today.AddDays(rng.Next(1, 31));
                    }

                    var status = paid.HasValue ? "paid" : (due < today ? "overdue" : "unpaid");

                    using (var ins = con.CreateCommand())
                    {
                        ins.CommandText = "INSERT INTO invoices(id, customer, amount, status, issueDate, dueDate, paidDate) VALUES($id,$customer,$amount,$status,$issue,$due,$paid)";
                        ins.Parameters.AddWithValue("$id", id);
                        ins.Parameters.AddWithValue("$customer", cust);
                        ins.Parameters.AddWithValue("$amount", amount);
                        ins.Parameters.AddWithValue("$status", status);
                        ins.Parameters.AddWithValue("$issue", issue.ToString("yyyy-MM-dd"));
                        ins.Parameters.AddWithValue("$due", due.ToString("yyyy-MM-dd"));
                        ins.Parameters.AddWithValue("$paid", paid?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
                        await ins.ExecuteNonQueryAsync();
                    }

                    if (paid.HasValue)
                    {
                        using var pins = con.CreateCommand();
                        pins.CommandText = "INSERT INTO payments(id, invoiceId, amount, paidAt) VALUES($pid,$invoiceId,$amount,$paidAt)";
                        pins.Parameters.AddWithValue("$pid", Guid.NewGuid().ToString("N"));
                        pins.Parameters.AddWithValue("$invoiceId", id);
                        pins.Parameters.AddWithValue("$amount", amount);
                        pins.Parameters.AddWithValue("$paidAt", paid.Value.ToString("yyyy-MM-dd"));
                        await pins.ExecuteNonQueryAsync();
                    }

                    created++;
                }
                if (created >= totalTarget) break;
            }
        }
        tx.Commit();
    }

    public async Task ImportInvoicesAsync(IEnumerable<InvoiceDto> invoices)
    {
        using var con = new SqliteConnection(_conn.ConnectionString);
        await con.OpenAsync();
        using var tx = con.BeginTransaction();
        foreach (var inv in invoices)
        {
            using var up = con.CreateCommand();
            up.CommandText = @"INSERT INTO invoices(id, customer, amount, status, issueDate, dueDate, paidDate)
                              VALUES($id,$customer,$amount,$status,$issue,$due,$paid)
                              ON CONFLICT(id) DO UPDATE SET customer=$customer, amount=$amount, status=$status, issueDate=$issue, dueDate=$due, paidDate=$paid";
            up.Parameters.AddWithValue("$id", inv.Id);
            up.Parameters.AddWithValue("$customer", inv.Customer);
            up.Parameters.AddWithValue("$amount", inv.Amount);
            up.Parameters.AddWithValue("$status", inv.Status ?? (inv.PaidDate.HasValue ? "paid" : "unpaid"));
            up.Parameters.AddWithValue("$issue", inv.IssueDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            up.Parameters.AddWithValue("$due", inv.DueDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            up.Parameters.AddWithValue("$paid", inv.PaidDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
            await up.ExecuteNonQueryAsync();
        }
        tx.Commit();
    }

    private static DateOnly? ParseDateOnly(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateOnly.TryParse(s, out var d)) return d;
        if (DateTime.TryParse(s, out var dt)) return DateOnly.FromDateTime(dt);
        return null;
    }

    public readonly record struct InvoiceRow(string Id, string Customer, decimal Amount, DateOnly DueDate);
    public readonly record struct InvoiceDto(string Id, string Customer, decimal Amount, string? Status, DateOnly? IssueDate, DateOnly? DueDate, DateOnly? PaidDate);
}
