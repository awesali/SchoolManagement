// Backend section: application services and shared rules.
using SchoolManagement.Model;

namespace SchoolManagement.Service;

// Implements accounting rules application behavior.
public static class AccountingRules
{
    public static readonly string[] Types = { "Asset", "Liability", "Equity", "Income", "Expense" };
    public static readonly string[] Kinds =
    {
        "Journal",
        "Receipt",
        "Payment",
        "Expense",
        "Supplier bill",
        "Supplier payment",
        "Refund",
        "Opening",
    };

    public static string? Validate(
        DateTime date,
        string narration,
        IReadOnlyCollection<AccountingLine> lines,
        DateTime? lockedThrough,
        DateTime today
    )
    {
        if (date.Date < new DateTime(1900, 1, 1) || date.Date > today.Date)
            return "Choose a posting date between 1900 and today.";
        if (lockedThrough.HasValue && date.Date <= lockedThrough.Value.Date)
            return "This accounting period is locked.";
        if (string.IsNullOrWhiteSpace(narration) || narration.Length > 500)
            return "Enter a narration of up to 500 characters.";
        if (lines.Count < 2 || lines.Count > 100)
            return "A voucher needs between 2 and 100 lines.";
        if (
            lines.Any(x =>
                x.AccountId <= 0
                || x.Debit < 0
                || x.Credit < 0
                || (x.Debit > 0) == (x.Credit > 0)
                || x.Debit > 999999999999m
                || x.Credit > 999999999999m
                || decimal.Round(x.Debit, 2) != x.Debit
                || decimal.Round(x.Credit, 2) != x.Credit
            )
        )
            return "Each line needs an account and one positive debit or credit, with at most two decimal places.";
        if (lines.Sum(x => x.Debit) != lines.Sum(x => x.Credit))
            return "Total debits must equal total credits.";
        return null;
    }
}
