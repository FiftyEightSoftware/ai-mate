using System.Net.Http.Json;
using ai_mate_blazor.Models;

namespace ai_mate_blazor.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    // Jobs
    public Task<List<Job>?> GetJobsAsync(CancellationToken ct = default)
        => _http.GetFromJsonAsync<List<Job>>("/api/jobs", ct);

    public async Task<Job?> CreateJobAsync(string title, string status = "Upcoming", decimal? quotedPrice = null, CancellationToken ct = default)
    {
        var job = new Job { Title = title, Status = status, QuotedPrice = quotedPrice };
        var resp = await _http.PostAsJsonAsync("/api/jobs", job, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Job>(cancellationToken: ct);
    }

    // Dashboard
    public record DashboardInvoice(string id, string customer, decimal amount, DateTime dueDate, string status);
    public record DashboardCashPoint(DateTime date, decimal amount);
    public record DashboardResponse(decimal outstandingTotal, decimal overdueTotal, decimal dueSoonTotal, decimal paidLast30, List<DashboardInvoice> invoices, List<DashboardCashPoint> projectedCashFlow);

    public Task<DashboardResponse?> GetDashboardAsync(CancellationToken ct = default)
        => _http.GetFromJsonAsync<DashboardResponse>("/api/dashboard", ct);

    public Task<DashboardResponse?> GetDashboardAsync(int weeks, CancellationToken ct = default)
        => _http.GetFromJsonAsync<DashboardResponse>($"/api/dashboard?weeks={weeks}", ct);

    // Invoices
    public record InvoiceDto(string id, string customer, decimal amount, string? status, DateTime? issueDate, DateTime? dueDate, DateTime? paidDate);

    public Task<List<InvoiceDto>?> GetInvoicesAsync(string? status = null, CancellationToken ct = default)
        => _http.GetFromJsonAsync<List<InvoiceDto>>(string.IsNullOrWhiteSpace(status) ? "/api/invoices" : $"/api/invoices?status={Uri.EscapeDataString(status)}", ct);

    public Task<InvoiceDto?> GetInvoiceAsync(string id, CancellationToken ct = default)
        => _http.GetFromJsonAsync<InvoiceDto>($"/api/invoices/{Uri.EscapeDataString(id)}", ct);

    public async Task<bool> ImportInvoicesAsync(List<InvoiceDto> items, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/import/invoices", items, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> MarkInvoicePaidAsync(string id, DateTime? paidDate = null, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"/api/invoices/{Uri.EscapeDataString(id)}/mark-paid", new { paidDate }, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> AddPaymentAsync(string invoiceId, decimal amount, DateTime? paidAt = null, CancellationToken ct = default)
    {
        var payload = new { invoiceId, amount, paidAt };
        var resp = await _http.PostAsJsonAsync("/api/payments", payload, ct);
        return resp.IsSuccessStatusCode;
    }

    // Payments
    public record PaymentDto(string id, string invoiceId, decimal amount, DateTime paidAt);

    public async Task<List<PaymentDto>?> GetPaymentsAsync(DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        string url = "/api/payments";
        if (from.HasValue && to.HasValue)
        {
            url += $"?from={Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd"))}&to={Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd"))}";
        }
        var raw = await _http.GetFromJsonAsync<List<Dictionary<string, object?>>>(url, ct);
        if (raw is null) return new List<PaymentDto>();
        var list = new List<PaymentDto>();
        foreach (var r in raw)
        {
            var id = Convert.ToString(r.GetValueOrDefault("id")) ?? string.Empty;
            var invoiceId = Convert.ToString(r.GetValueOrDefault("invoiceId")) ?? string.Empty;
            var amount = Convert.ToDecimal(r.GetValueOrDefault("amount") ?? 0m);
            var paidAtStr = Convert.ToString(r.GetValueOrDefault("paidAt"));
            DateTime paidAt = DateTime.TryParse(paidAtStr, out var dt) ? dt : DateTime.Today;
            list.Add(new PaymentDto(id, invoiceId, amount, paidAt));
        }
        return list;
    }
}
