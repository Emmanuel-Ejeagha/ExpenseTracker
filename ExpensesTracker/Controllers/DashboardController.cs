using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ExpensesTracker.Controllers
{
    // [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            AppDbContext context,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ActionResult> Index(string period = "week", DateTime? customStart = null, DateTime? customEnd = null)
        {
            try
            {
                DateTime startDate, endDate;
                var now = DateTime.Today;

                // Determine date range
                switch (period.ToLower())
                {
                    case "today":
                        startDate = endDate = now;
                        break;
                    case "week":
                        startDate = now.AddDays(-6);
                        endDate = now;
                        break;
                    case "month":
                        startDate = now.AddDays(-29);
                        endDate = now;
                        break;
                    case "year":
                        startDate = now.AddDays(-364);
                        endDate = now;
                        break;
                    case "custom" when customStart.HasValue && customEnd.HasValue:
                        startDate = customStart.Value;
                        endDate = customEnd.Value;
                        break;
                    default:
                        startDate = now.AddDays(-6);
                        endDate = now;
                        break;
                }

                // Get selected transactions
                List<Transaction> SelectedTransactions = await _context.Transactions
                    .Include(x => x.Category)
                    .Where(x => x.Date >= startDate && x.Date <= endDate)
                    .ToListAsync();

                // Total Income
                decimal TotalIncome = SelectedTransactions
                    .Where(x => x.Category.Type == "Income")
                    .Sum(x => x.Amount);
                ViewBag.TotalIncome = TotalIncome.ToString("'₦'###,###,##0");

                // Total Expenses
                decimal TotalExpense = SelectedTransactions
                    .Where(x => x.Category.Type == "Expense")
                    .Sum(x => x.Amount);
                ViewBag.TotalExpense = TotalExpense.ToString("'₦'###,###,##0");

                // Balance
                decimal Balance = TotalIncome - TotalExpense;
                ViewBag.Balance = Balance.ToString("'₦'###,###,##0");

                // Doughnut Chart - Expense By Category
                ViewBag.DoughnutChartData = SelectedTransactions
                    .Where(x => x.Category.Type == "Expense")
                    .GroupBy(y => y.Category.CategoryId)
                    .Select(z => new
                    {
                        categoryTitleWithIcon = z.First().Category.Icon + " " + z.First().Category.Title,
                        amount = z.Sum(x => x.Amount),
                        formattedAmount = z.Sum(x => x.Amount).ToString("'₦'###,###,##0")
                    }).ToList()
                    .OrderBy(x => x.amount);

                // Spline Chart - Income vs Expense
                // Income
                var IncomeSummary = SelectedTransactions
                    .Where(i => i.Category.Type == "Income")
                    .GroupBy(j => j.Date)
                    .Select(x => new
                    {
                        day = x.First().Date.ToString("dd-MM"),
                        income = x.Sum(y => y.Amount)
                    }).ToList();

                // Expense
                var ExpenseSummary = SelectedTransactions
                    .Where(i => i.Category.Type == "Expense")
                    .GroupBy(j => j.Date)
                    .Select(x => new
                    {
                        day = x.First().Date.ToString("dd-MM"),
                        expense = x.Sum(y => y.Amount)
                    }).ToList();

                // Combine Income & Expense
                string[] LastDays = Enumerable.Range(0, 7)
                    .Select(i => startDate.AddDays(i).ToString("dd-MM"))
                    .ToArray();

                ViewBag.SplineChartData = from day in LastDays
                                          join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                          from income in dayIncomeJoined.DefaultIfEmpty()
                                          join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                          from expense in expenseJoined.DefaultIfEmpty()
                                          select new
                                          {
                                              day = day,
                                              income = income == null ? 0 : income.income,
                                              expense = expense == null ? 0 : expense.expense,
                                          };

                // Recent Transactions
                ViewBag.RecentTransactions = await _context.Transactions
                    .Include(i => i.Category)
                    .OrderByDescending(j => j.Date)
                    .Take(5)
                    .ToListAsync();

                // Additional stats
                ViewBag.IncomeCount = SelectedTransactions.Count(x => x.Category.Type == "Income");
                ViewBag.ExpenseCount = SelectedTransactions.Count(x => x.Category.Type == "Expense");
                ViewBag.PeriodLabel = GetPeriodLabel(period, startDate, endDate);
                ViewBag.Period = period;
                ViewBag.StartDate = startDate.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate.ToString("yyyy-MM-dd");

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

                ViewBag.BarChartData = new List<object>
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

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading the dashboard.";

                // Return empty data on error
                ViewBag.TotalIncome = "₦0";
                ViewBag.TotalExpense = "₦0";
                ViewBag.Balance = "₦0";
                ViewBag.DoughnutChartData = new List<object>();
                ViewBag.SplineChartData = new List<object>();
                ViewBag.RecentTransactions = new List<Transaction>();

                return View();
            }
        }

        // API: Get dashboard data for AJAX updates
        [HttpGet]
        public async Task<IActionResult> GetDashboardData(string period = "week", DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                DateTime start, end;
                var now = DateTime.Today;

                if (startDate.HasValue && endDate.HasValue)
                {
                    start = startDate.Value;
                    end = endDate.Value;
                }
                else
                {
                    switch (period.ToLower())
                    {
                        case "today":
                            start = end = now;
                            break;
                        case "week":
                            start = now.AddDays(-6);
                            end = now;
                            break;
                        case "month":
                            start = now.AddDays(-29);
                            end = now;
                            break;
                        case "year":
                            start = now.AddDays(-364);
                            end = now;
                            break;
                        default:
                            start = now.AddDays(-6);
                            end = now;
                            break;
                    }
                }

                var transactions = await _context.Transactions
                    .Include(x => x.Category)
                    .Where(x => x.Date >= start && x.Date <= end)
                    .ToListAsync();

                var data = new
                {
                    TotalIncome = transactions.Where(x => x.Category?.Type == "Income").Sum(x => x.Amount),
                    TotalExpense = transactions.Where(x => x.Category?.Type == "Expense").Sum(x => x.Amount),
                    IncomeCount = transactions.Count(x => x.Category?.Type == "Income"),
                    ExpenseCount = transactions.Count(x => x.Category?.Type == "Expense"),
                    Balance = transactions.Where(x => x.Category?.Type == "Income").Sum(x => x.Amount) -
                             transactions.Where(x => x.Category?.Type == "Expense").Sum(x => x.Amount)
                };

                return Json(new
                {
                    success = true,
                    data,
                    periodLabel = GetPeriodLabel(period, start, end)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return Json(new { success = false, message = "Error loading dashboard data" });
            }
        }

        private string GetPeriodLabel(string period, DateTime start, DateTime end)
        {
            return period.ToLower() switch
            {
                "today" => "Today",
                "week" => "Last 7 Days",
                "month" => "Last 30 Days",
                "year" => "Last 12 Months",
                "custom" => $"{start:MMM dd} - {end:MMM dd, yyyy}",
                _ => $"{start:MMM dd} - {end:MMM dd, yyyy}"
            };
        }
    }
}
