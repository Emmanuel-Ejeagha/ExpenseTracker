using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;
using ExpensesTracker.Models.ViewModels;
using ExpensesTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpensesTracker.Controllers
{
    // [Authorize]
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            AppDbContext context, 
            ITransactionService transactionService,
            ILogger<TransactionController> logger)
        {
            _context = context;
            _transactionService = transactionService;
            _logger = logger;
        }

        // GET: Transaction
        public async Task<IActionResult> Index(
            string search = "", 
            int? categoryId = null,
            string type = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            string sortBy = "Date",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var query = _context.Transactions
                    .Include(t => t.Category)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(t => t.Note != null && t.Note.Contains(search));

                if (categoryId.HasValue && categoryId > 0)
                    query = query.Where(t => t.CategoryId == categoryId);

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(t => t.Category != null && t.Category.Type == type);

                if (startDate.HasValue)
                    query = query.Where(t => t.Date >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(t => t.Date <= endDate.Value.AddDays(1).AddTicks(-1));

                if (minAmount.HasValue)
                    query = query.Where(t => t.Amount >= minAmount.Value);

                if (maxAmount.HasValue)
                    query = query.Where(t => t.Amount <= maxAmount.Value);

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply sorting
                query = sortOrder.ToLower() == "desc" 
                    ? query.OrderBy($"{sortBy} DESC") 
                    : query.OrderBy(sortBy);

                // Apply pagination
                var transactions = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Prepare view data
                PopulateCategories();
                ViewBag.Search = search;
                ViewBag.CategoryId = categoryId;
                ViewBag.Type = type;
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.MinAmount = minAmount;
                ViewBag.MaxAmount = maxAmount;
                ViewBag.SortBy = sortBy;
                ViewBag.SortOrder = sortOrder;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // Types for filter dropdown
                ViewBag.TransactionTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "All Types" },
                    new SelectListItem { Value = "Income", Text = "Income" },
                    new SelectListItem { Value = "Expense", Text = "Expense" }
                };

                return View(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading transactions");
                TempData["ErrorMessage"] = "An error occurred while loading transactions.";
                return View(new List<Transaction>());
            }
        }

        // GET: Transaction/AddOrEdit
        public IActionResult AddOrEdit(int id = 0)
        {
            try
            {
                Transaction transaction;
                
                if (id == 0)
                {
                    transaction = new Transaction { Date = DateTime.Today };
                    ViewData["PageTitle"] = "Create New Transaction";
                }
                else
                {
                    transaction = _context.Transactions
                        .Include(t => t.Category)
                        .FirstOrDefault(t => t.TransactionId == id);
                        
                    if (transaction == null)
                    {
                        TempData["ErrorMessage"] = "Transaction not found.";
                        return RedirectToAction(nameof(Index));
                    }
                    ViewData["PageTitle"] = "Edit Transaction";
                }

                PopulateCategories();
                return View(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading transaction for edit");
                TempData["ErrorMessage"] = "An error occurred while loading the transaction.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Transaction/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,Amount,Note,Date,CategoryId")] Transaction transaction)
        {
            try
            {
                var category = await _context.Categories.FindAsync(transaction.CategoryId);
                if (category == null)
                {
                    ModelState.AddModelError("CategoryId", "Selected category does not exist");
                }

                if (ModelState.IsValid)
                {
                    if (transaction.TransactionId == 0)
                    {
                        _context.Add(transaction);
                        TempData["SuccessMessage"] = "Transaction created successfully!";
                    }
                    else
                    {
                        _context.Update(transaction);
                        TempData["SuccessMessage"] = "Transaction updated successfully!";
                    }

                    await _context.SaveChangesAsync();

                    // Return JSON for AJAX requests
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new 
                        { 
                            success = true, 
                            message = TempData["SuccessMessage"],
                            transactionId = transaction.TransactionId 
                        });
                    }

                    return RedirectToAction(nameof(Index));
                }

                PopulateCategories();
                return View(transaction);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error saving transaction");
                ModelState.AddModelError("", "Unable to save transaction. Please try again.");
                PopulateCategories();
                return View(transaction);
            }
        }

        // GET: Transaction/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(m => m.TransactionId == id);
                
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // GET: Transaction/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var transaction = await _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefaultAsync(m => m.TransactionId == id);
                
            if (transaction == null)
            {
                return NotFound();
            }

            return View(transaction);
        }

        // POST: Transaction/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var transaction = await _context.Transactions.FindAsync(id);
                if (transaction == null)
                {
                    return NotFound();
                }

                _context.Transactions.Remove(transaction);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Transaction deleted successfully!";
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = TempData["SuccessMessage"] });
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting transaction");
                TempData["ErrorMessage"] = "An error occurred while deleting the transaction.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // API: Quick Add Transaction (for modal/form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickAdd([FromBody] QuickTransactionViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new 
                    { 
                        success = false, 
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage) 
                    });
                }

                var transaction = new Transaction
                {
                    Amount = model.Amount,
                    Note = model.Note ?? string.Empty,
                    Date = model.Date ?? DateTime.Today,
                    CategoryId = model.CategoryId
                };

                _context.Add(transaction);
                await _context.SaveChangesAsync();

                return Json(new 
                { 
                    success = true, 
                    message = "Transaction added successfully!",
                    transaction = new
                    {
                        transaction.TransactionId,
                        transaction.Amount,
                        transaction.Note,
                        transaction.Date,
                        transaction.CategoryId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick add transaction");
                return Json(new { success = false, message = "Error adding transaction" });
            }
        }

        // API: Bulk delete transactions
        [HttpPost]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                {
                    return Json(new { success = false, message = "No transactions selected" });
                }

                var transactions = await _context.Transactions
                    .Where(t => ids.Contains(t.TransactionId))
                    .ToListAsync();

                _context.Transactions.RemoveRange(transactions);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = $"{transactions.Count} transactions deleted successfully!" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk delete");
                return Json(new { success = false, message = "Error deleting transactions" });
            }
        }

        // API: Export transactions
        [HttpGet]
        public async Task<IActionResult> Export(string format = "csv")
        {
            try
            {
                var transactions = await _context.Transactions
                    .Include(t => t.Category)
                    .OrderByDescending(t => t.Date)
                    .ToListAsync();

                return format.ToLower() switch
                {
                    "csv" => await _transactionService.ExportToCsvAsync(transactions),
                    "excel" => await _transactionService.ExportToExcelAsync(transactions),
                    "pdf" => await _transactionService.ExportToPdfAsync(transactions),
                    _ => BadRequest("Unsupported export format")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting transactions");
                TempData["ErrorMessage"] = "Error exporting transactions";
                return RedirectToAction(nameof(Index));
            }
        }

        [NonAction]
        public void PopulateCategories()
        {
            var categories = _context.Categories
                .OrderBy(c => c.Type)
                .ThenBy(c => c.Title)
                .Select(c => new 
                { 
                    c.CategoryId, 
                    TitleWithIcon = c.TitleWithIcon,
                    c.Type 
                })
                .ToList();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "TitleWithIcon");
        }
    }
}