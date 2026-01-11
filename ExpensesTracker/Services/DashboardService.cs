using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;
using ExpensesTracker.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ExpensesTracker.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardDTO> GetDashboardDataAsync(DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .ToListAsync();

            var incomeTransactions = transactions.Where(t => t.Category?.Type == "Income").ToList();
            var expenseTransactions = transactions.Where(t => t.Category?.Type == "Expense").ToList();

            var totalDays = (endDate - startDate).TotalDays;
            
            var dashboardData = new DashboardDTO
            {
                TotalIncome = incomeTransactions.Sum(t => t.Amount),
                TotalExpense = expenseTransactions.Sum(t => t.Amount),
                IncomeCount = incomeTransactions.Count,
                ExpenseCount = expenseTransactions.Count,
                Balance = incomeTransactions.Sum(t => t.Amount) - expenseTransactions.Sum(t => t.Amount),
                RecentTransactions = transactions
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.TransactionId)
                    .Take(10)
                    .ToList(),
                LargestExpense = expenseTransactions.OrderByDescending(t => t.Amount).FirstOrDefault(),
                LargestIncome = incomeTransactions.OrderByDescending(t => t.Amount).FirstOrDefault(),
                AverageDailyExpense = expenseTransactions.Any() && totalDays > 0
                    ? expenseTransactions.Sum(t => t.Amount) / (decimal)totalDays 
                    : 0
            };

            // Expense by category
            dashboardData.ExpenseByCategory = expenseTransactions
                .GroupBy(t => t.Category)
                .Where(g => g.Key != null)
                .Select(g => new
                {
                    categoryId = g.Key!.CategoryId,
                    categoryTitleWithIcon = g.Key.TitleWithIcon ?? "Uncategorized",
                    amount = g.Sum(t => t.Amount),
                    formattedAmount = g.Sum(t => t.Amount).ToString("C", CultureInfo.CreateSpecificCulture("en-NG")),
                    count = g.Count(),
                    percentage = expenseTransactions.Sum(t => t.Amount) > 0 
                        ? (g.Sum(t => t.Amount) / expenseTransactions.Sum(t => t.Amount)) * 100 
                        : 0
                })
                .OrderByDescending(x => x.amount)
                .ToList<object>();

            // Daily summary
            var days = Enumerable.Range(0, (int)(endDate - startDate).TotalDays + 1)
                .Select(i => startDate.AddDays(i))
                .ToList();

            dashboardData.DailySummary = days
                .Select(day => new
                {
                    day = day.ToString("dd-MM"),
                    date = day.ToString("yyyy-MM-dd"),
                    income = incomeTransactions.Where(t => t.Date == day).Sum(t => t.Amount),
                    expense = expenseTransactions.Where(t => t.Date == day).Sum(t => t.Amount),
                    net = incomeTransactions.Where(t => t.Date == day).Sum(t => t.Amount) - 
                          expenseTransactions.Where(t => t.Date == day).Sum(t => t.Amount)
                })
                .ToList<object>();

            // Top categories
            dashboardData.TopExpenseCategories = expenseTransactions
                .GroupBy(t => t.Category)
                .Where(g => g.Key != null)
                .OrderByDescending(g => g.Sum(t => t.Amount))
                .Take(5)
                .Select(g => new
                {
                    category = g.Key?.Title ?? "Uncategorized",
                    amount = g.Sum(t => t.Amount),
                    count = g.Count()
                })
                .ToList<object>();

            dashboardData.TopIncomeCategories = incomeTransactions
                .GroupBy(t => t.Category)
                .Where(g => g.Key != null)
                .OrderByDescending(g => g.Sum(t => t.Amount))
                .Take(5)
                .Select(g => new
                {
                    category = g.Key?.Title ?? "Uncategorized",
                    amount = g.Sum(t => t.Amount),
                    count = g.Count()
                })
                .ToList<object>();

            // Monthly comparison
            var currentMonth = DateTime.Today;
            var previousMonth = currentMonth.AddMonths(-1);
            
            var currentMonthTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date.Month == currentMonth.Month && t.Date.Year == currentMonth.Year)
                .ToListAsync();
                
            var previousMonthTransactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date.Month == previousMonth.Month && t.Date.Year == previousMonth.Year)
                .ToListAsync();

            dashboardData.MonthlyComparison = new List<object>
            {
                new {
                    month = currentMonth.ToString("MMM yyyy"),
                    income = currentMonthTransactions.Where(t => t.Category?.Type == "Income").Sum(t => t.Amount),
                    expense = currentMonthTransactions.Where(t => t.Category?.Type == "Expense").Sum(t => t.Amount)
                },
                new {
                    month = previousMonth.ToString("MMM yyyy"),
                    income = previousMonthTransactions.Where(t => t.Category?.Type == "Income").Sum(t => t.Amount),
                    expense = previousMonthTransactions.Where(t => t.Category?.Type == "Expense").Sum(t => t.Amount)
                }
            };

            return dashboardData;
        }

        public async Task<object> GetFinancialSummaryAsync(int year)
        {
            var monthlyData = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date.Year == year)
                .GroupBy(t => new { t.Date.Month, t.Category!.Type })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Type = g.Key.Type,
                    Amount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var result = Enumerable.Range(1, 12)
                .Select(month => new
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month),
                    Income = monthlyData
                        .Where(x => x.Month == month && x.Type == "Income")
                        .Sum(x => x.Amount),
                    Expense = monthlyData
                        .Where(x => x.Month == month && x.Type == "Expense")
                        .Sum(x => x.Amount)
                })
                .ToList();

            return new
            {
                MonthlyData = result,
                YearlyIncome = result.Sum(x => x.Income),
                YearlyExpense = result.Sum(x => x.Expense),
                YearlyBalance = result.Sum(x => x.Income - x.Expense)
            };
        }

        public async Task<object> GetCategoryBreakdownAsync(string type, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Category!.Type == type);

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(t => t.Date <= endDate.Value);

            var data = await query
                .GroupBy(t => t.Category)
                .Where(g => g.Key != null)
                .Select(g => new
                {
                    Category = g.Key!.Title,
                    Icon = g.Key.Icon,
                    Amount = g.Sum(t => t.Amount),
                    Count = g.Count(),
                    Percentage = 0m // Will calculate after
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            var total = data.Sum(x => x.Amount);
            if (total > 0)
            {
                var recalculatedData = data.Select(x => new
                {
                    x.Category,
                    x.Icon,
                    x.Amount,
                    x.Count,
                    Percentage = (x.Amount / total) * 100
                }).ToList();
                
                return new
                {
                    Total = total,
                    Categories = recalculatedData,
                    Type = type
                };
            }

            return new
            {
                Total = total,
                Categories = data,
                Type = type
            };
        }

        public async Task<object> GetTrendDataAsync(DateTime startDate, DateTime endDate, string interval = "day")
        {
            var transactions = await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .ToListAsync();

            return interval.ToLower() switch
            {
                "week" => GetWeeklyTrend(transactions, startDate, endDate),
                "month" => GetMonthlyTrend(transactions, startDate, endDate),
                _ => GetDailyTrend(transactions, startDate, endDate)
            };
        }

        private object GetDailyTrend(List<Transaction> transactions, DateTime startDate, DateTime endDate)
        {
            var days = Enumerable.Range(0, (int)(endDate - startDate).TotalDays + 1)
                .Select(i => startDate.AddDays(i))
                .ToList();

            return days.Select(day => new
            {
                Date = day.ToString("yyyy-MM-dd"),
                Label = day.ToString("MMM dd"),
                Income = transactions.Where(t => t.Date == day && t.Category?.Type == "Income").Sum(t => t.Amount),
                Expense = transactions.Where(t => t.Date == day && t.Category?.Type == "Expense").Sum(t => t.Amount)
            }).ToList();
        }

        private object GetWeeklyTrend(List<Transaction> transactions, DateTime startDate, DateTime endDate)
        {
            var weeks = new List<object>();
            var currentDate = startDate;
            
            while (currentDate <= endDate)
            {
                var weekEnd = currentDate.AddDays(6) > endDate ? endDate : currentDate.AddDays(6);
                var weekTransactions = transactions
                    .Where(t => t.Date >= currentDate && t.Date <= weekEnd)
                    .ToList();

                weeks.Add(new
                {
                    Week = $"Week {GetWeekNumber(currentDate)}",
                    StartDate = currentDate.ToString("MMM dd"),
                    EndDate = weekEnd.ToString("MMM dd"),
                    Income = weekTransactions.Where(t => t.Category?.Type == "Income").Sum(t => t.Amount),
                    Expense = weekTransactions.Where(t => t.Category?.Type == "Expense").Sum(t => t.Amount)
                });

                currentDate = currentDate.AddDays(7);
            }

            return weeks;
        }

        private object GetMonthlyTrend(List<Transaction> transactions, DateTime startDate, DateTime endDate)
        {
            var months = new List<object>();
            var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
            
            while (currentDate <= endDate)
            {
                var monthEnd = new DateTime(currentDate.Year, currentDate.Month, 
                    DateTime.DaysInMonth(currentDate.Year, currentDate.Month));
                var monthTransactions = transactions
                    .Where(t => t.Date >= currentDate && t.Date <= monthEnd)
                    .ToList();

                months.Add(new
                {
                    Month = currentDate.ToString("MMM yyyy"),
                    Income = monthTransactions.Where(t => t.Category?.Type == "Income").Sum(t => t.Amount),
                    Expense = monthTransactions.Where(t => t.Category?.Type == "Expense").Sum(t => t.Amount)
                });

                currentDate = currentDate.AddMonths(1);
            }

            return months;
        }

        private int GetWeekNumber(DateTime date)
        {
            var culture = CultureInfo.CurrentCulture;
            return culture.Calendar.GetWeekOfYear(date, culture.DateTimeFormat.CalendarWeekRule, 
                culture.DateTimeFormat.FirstDayOfWeek);
        }
    }
}