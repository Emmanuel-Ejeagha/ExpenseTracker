using System;

namespace ExpensesTracker.Models.DTOs;

public class DailySummaryDTO
{
    public string Day { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Net { get; set; }
}
