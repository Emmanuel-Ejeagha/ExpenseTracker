using System;

namespace ExpensesTracker.Models.DTOs;

public class CategorySummaryDTO
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string FormattedAmount { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}