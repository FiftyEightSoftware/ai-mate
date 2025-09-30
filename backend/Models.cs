using System.Text.Json.Serialization;

// DTO records for backend APIs
public record VoiceData([property: JsonPropertyName("dataUrl")] string DataUrl);
public record VerifyResponse(bool ok, double score);
public record DashboardInvoice(string id, string customer, decimal amount, DateTime dueDate, string status);
public record DashboardCashPoint(DateTime date, decimal amount);
public record DashboardResponse(decimal outstandingTotal, decimal overdueTotal, decimal dueSoonTotal, decimal paidLast30, List<DashboardInvoice> invoices, List<DashboardCashPoint> projectedCashFlow);

public record MarkPaidPayload(DateTime? paidDate);
public record AddPaymentPayload(string invoiceId, double amount, DateTime? paidAt);
