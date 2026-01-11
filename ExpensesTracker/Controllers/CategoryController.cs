using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;
using ExpensesTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering; // ADD THIS LINE
using System.Linq.Dynamic.Core;

namespace ExpensesTracker.Controllers
{
    // [Authorize]
    public class CategoryController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(
            AppDbContext context, 
            ICategoryService categoryService,
            ILogger<CategoryController> logger)
        {
            _context = context;
            _categoryService = categoryService;
            _logger = logger;
        }

        // GET: Category
        public async Task<IActionResult> Index(string search = "", string sortBy = "Title", string sortOrder = "asc", int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.Categories.AsQueryable();

                // Search
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(c => 
                        c.Title.Contains(search) || 
                        c.Icon.Contains(search) || 
                        c.Type.Contains(search));
                }

                // Sorting
                query = sortOrder.ToLower() == "desc" 
                    ? query.OrderBy($"{sortBy} DESC") 
                    : query.OrderBy(sortBy);

                // Pagination
                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.Search = search;
                ViewBag.SortBy = sortBy;
                ViewBag.SortOrder = sortOrder;
                ViewBag.Page = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalCount = totalCount;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return View(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["ErrorMessage"] = "An error occurred while loading categories.";
                return View(new List<Category>());
            }
        }

        // GET: Category/AddOrEdit
        public IActionResult AddOrEdit(int id = 0)
        {
            try
            {
                Category category;
                
                if (id == 0)
                {
                    category = new Category { Type = "Expense" };
                    ViewData["PageTitle"] = "Create New Category";
                }
                else
                {
                    category = _context.Categories.Find(id);
                    if (category == null)
                    {
                        TempData["ErrorMessage"] = "Category not found.";
                        return RedirectToAction(nameof(Index));
                    }
                    ViewData["PageTitle"] = "Edit Category";
                }

                ViewBag.CategoryTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Expense", Text = "Expense" },
                    new SelectListItem { Value = "Income", Text = "Income" }
                };

                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category for edit");
                TempData["ErrorMessage"] = "An error occurred while loading the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Category/AddOrEdit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("CategoryId,Title,Icon,Type")] Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check for duplicate category name
                    var existingCategory = await _context.Categories
                        .Where(c => c.Title == category.Title && c.CategoryId != category.CategoryId)
                        .FirstOrDefaultAsync();

                    if (existingCategory != null)
                    {
                        ModelState.AddModelError("Title", "A category with this name already exists.");
                        ViewBag.CategoryTypes = new List<SelectListItem>
                        {
                            new SelectListItem { Value = "Expense", Text = "Expense" },
                            new SelectListItem { Value = "Income", Text = "Income" }
                        };
                        return View(category);
                    }

                    if (category.CategoryId == 0)
                    {
                        _context.Add(category);
                        TempData["SuccessMessage"] = "Category created successfully!";
                    }
                    else
                    {
                        _context.Update(category);
                        TempData["SuccessMessage"] = "Category updated successfully!";
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    // Return JSON for AJAX requests
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { 
                            success = true, 
                            message = TempData["SuccessMessage"],
                            categoryId = category.CategoryId 
                        });
                    }
                    
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.CategoryTypes = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Expense", Text = "Expense" },
                    new SelectListItem { Value = "Income", Text = "Income" }
                };
                
                return View(category);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error saving category");
                TempData["ErrorMessage"] = "An error occurred while saving the category.";
                return View(category);
            }
        }

        // GET: Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
                
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // GET: Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(m => m.CategoryId == id);
                
            if (category == null)
            {
                return NotFound();
            }

            // Check if category has transactions
            var hasTransactions = await _context.Transactions
                .AnyAsync(t => t.CategoryId == id);
                
            ViewBag.HasTransactions = hasTransactions;

            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                // Check for existing transactions
                var hasTransactions = await _context.Transactions
                    .AnyAsync(t => t.CategoryId == id);
                    
                if (hasTransactions)
                {
                    TempData["ErrorMessage"] = "Cannot delete category because it has associated transactions.";
                    return RedirectToAction(nameof(Delete), new { id });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Category deleted successfully!";
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = TempData["SuccessMessage"] });
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error deleting category");
                TempData["ErrorMessage"] = "An error occurred while deleting the category.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        // API: Get categories by type
        [HttpGet]
        public async Task<IActionResult> GetByType(string type)
        {
            try
            {
                var categories = await _context.Categories
                    .Where(c => c.Type == type)
                    .OrderBy(c => c.Title)
                    .Select(c => new { 
                        c.CategoryId, 
                        c.Title, 
                        c.Icon,
                        TitleWithIcon = c.TitleWithIcon 
                    })
                    .ToListAsync();
                    
                return Json(new { success = true, data = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories by type");
                return Json(new { success = false, message = "Error loading categories" });
            }
        }
    }
}