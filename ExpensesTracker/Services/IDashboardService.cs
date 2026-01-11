using System;
using System.Threading.Tasks;
using ExpensesTracker.Models.DTOs;

namespace ExpensesTracker.Services
{
    public interface IDashboardService
    {
        Task<DashboardDTO> GetDashboardDataAsync(DateTime startDate, DateTime endDate);
        Task<object> GetFinancialSummaryAsync(int year);
        Task<object> GetCategoryBreakdownAsync(string type, DateTime? startDate = null, DateTime? endDate = null);
        Task<object> GetTrendDataAsync(DateTime startDate, DateTime endDate, string interval = "day");
    }
}