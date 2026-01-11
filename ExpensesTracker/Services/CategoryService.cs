using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpensesTracker.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetAllCategoriesAsync(bool includeTransactionCount = false)
        {
            var query = _context.Categories.AsQueryable();

            if (includeTransactionCount)
            {
                query = query.Select(c => new Category
                {
                    CategoryId = c.CategoryId,
                    Title = c.Title,
                    Icon = c.Icon,
                    Type = c.Type,
                    TransactionCount = c.Transactions.Count,
                    TotalAmount = c.Transactions.Sum(t => t.Amount)
                });
            }

            return await query
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.Transactions)
                .FirstOrDefaultAsync(c => c.CategoryId == id);
        }

        public async Task<bool> CreateCategoryAsync(Category category)
        {
            try
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateCategoryAsync(Category category)
        {
            try
            {
                _context.Categories.Update(category);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                var category = await GetCategoryByIdAsync(id);
                if (category == null)
                    return false;

                if (category.Transactions?.Any() == true)
                    return false;

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CategoryHasTransactionsAsync(int id)
        {
            return await _context.Transactions
                .AnyAsync(t => t.CategoryId == id);
        }

        public async Task<List<Category>> GetCategoriesByTypeAsync(string type)
        {
            return await _context.Categories
                .Where(c => c.Type == type)
                .OrderBy(c => c.Title)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetCategoryStatisticsAsync()
        {
            return new Dictionary<string, int>
            {
                ["Total"] = await _context.Categories.CountAsync(),
                ["Income"] = await _context.Categories.CountAsync(c => c.Type == "Income"),
                ["Expense"] = await _context.Categories.CountAsync(c => c.Type == "Expense"),
                ["WithTransactions"] = await _context.Categories
                    .CountAsync(c => c.Transactions.Any()),
                ["WithoutTransactions"] = await _context.Categories
                    .CountAsync(c => !c.Transactions.Any())
            };
        }
    }
}