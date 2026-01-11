using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpensesTracker.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExpensesTracker.Services
{
    public interface ITransactionService
    {
        Task<FileResult> ExportToCsvAsync(List<Transaction> transactions);
        Task<FileResult> ExportToExcelAsync(List<Transaction> transactions);
        Task<FileResult> ExportToPdfAsync(List<Transaction> transactions);
        Task<List<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<decimal> GetTotalByCategoryAsync(int categoryId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<Transaction>> SearchTransactionsAsync(string query, int? categoryId, DateTime? startDate, DateTime? endDate);
    }
}