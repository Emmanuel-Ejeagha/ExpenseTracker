using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpensesTracker.Models;

namespace ExpensesTracker.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync(bool includeTransactionCount = false);
        Task<Category?> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(Category category);
        Task<bool> UpdateCategoryAsync(Category category);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> CategoryHasTransactionsAsync(int id);
        Task<List<Category>> GetCategoriesByTypeAsync(string type);
        Task<Dictionary<string, int>> GetCategoryStatisticsAsync();
    }
}