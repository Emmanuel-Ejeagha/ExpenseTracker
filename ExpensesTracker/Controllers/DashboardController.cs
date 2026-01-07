using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpensesTracker.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        // private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            this._context = context;
        }
        public async Task<ActionResult> Index()
        {
            // Last 7 day
            DateTime startDate = DateTime.Today.AddDays(-6);
            DateTime endDate = DateTime.Today;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x => x.Category)
                .Where(x => x.Date >= startDate && x.Date <= endDate)
                .ToListAsync();

            // Total Income
            decimal TotalIncome = SelectedTransactions
                .Where(x => x.Category.Type == "Income")
                .Sum(x => x.Amount);
            ViewBag.TotalIncome = TotalIncome.ToString("'₦'###,###,##0");

            // TOtal Expenses
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

            // Spline Chart - Icome vs Expense
            // Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(x => new SplineChartData()
                {
                    day = x.First().Date.ToString("dd-MM"),
                    income = x.Sum(y => y.Amount)
                }).ToList();

            // Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(x => new SplineChartData()
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

            return View();
        }
    }
    public class SplineChartData
    {
        public string day;
        public decimal income;
        public decimal expense;
    }
}
