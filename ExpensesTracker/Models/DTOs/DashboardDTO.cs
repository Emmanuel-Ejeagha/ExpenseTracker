using System;

namespace ExpensesTracker.Models.DTOs;

public class DashboardDTO
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public int IncomeCount { get; set; }
    public int ExpenseCount { get; set; }
    public decimal AverageDailyExpense { get; set; }
    public Transaction? LargestExpense { get; set; }
    public Transaction? LargestIncome { get; set; }
    public List<object> ExpenseByCategory { get; set; } = new List<object>();
    public List<object> DailySummary { get; set; } = new List<object>();
    public List<object> MonthlyComparison { get; set; } = new List<object>();
    public List<Transaction> RecentTransactions { get; set; } = new List<Transaction>();
    public List<object> TopExpenseCategories { get; set; } = new List<object>();
    public List<object> TopIncomeCategories { get; set; } = new List<object>();
}