using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;

namespace ExpensesTracker.Controllers
{
    public class TransactionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(ILogger<TransactionController> logger)
        {
            this._logger = logger;
        }

        public TransactionController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Transaction
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Transactions.Include(t => t.Category);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Transaction/AddOrEdit
        public IActionResult AddOrEdit(int id = 0)
        {
            PopulateCategories();
            if (id == 0)
                return View(new Transaction());
            else
                return View(_context.Transactions.Find(id));
        }

        // POST: Transaction/AddOrEdit
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddOrEdit([Bind("TransactionId,Amount,Note,Date,CategoryId")] Transaction transaction)
        {
            var category = await _context.Categories.FindAsync(transaction.CategoryId);
            if (category == null)
            {
                ModelState.AddModelError("Category", "Selected category does not exist");
            }

            if (ModelState.IsValid)
            {
                try
                {

                    if (transaction.TransactionId == 0)
                        _context.Add(transaction);
                    else
                        _context.Update(transaction);

                    await _context.SaveChangesAsync();

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = true, message = "Transaction saved successfully" });
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "Unable to save transactions");
                    _logger.LogError(ex, "Error saving transaction");
                }
            }

            PopulateCategories();
            return View(transaction);
        }

        // Search and filter
        public async Task<IActionResult> Search(string searchTerm,
            int? categoryId,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount)
        {
            var query = _context.Transactions
                .Include(c => c.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(t => t.Note.Contains(searchTerm));

            if (categoryId.HasValue && categoryId > 0)
                query = query.Where(t => t.CategoryId == categoryId);


            var results = await query.ToListAsync();
            return PartialView("_TransactionList", results);
        }   
        
        public async Task<IActionResult> Filtered(TransactionFilterViewModel filter)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .AsQueryable();

            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
                query = query.Where(t => t.CategoryId == filter.CategoryId);

            if (filter.StartDate.HasValue)
                query = query.Where(t => t.Date >= filter.StartDate);

            if (filter.EndDate.HasValue)
                query = query.Where(c => c.Date == filter.EndDate);

            // Sorting
            query = filter.SortBy switch
            {
                "amount" => filter.SortDesc ? query.OrderByDescending(t => t.Amount) : query.OrderBy(t => t.Amount),
                "date" => filter.SortDesc ? query.OrderByDescending(t => t.Date) : query.OrderBy(t => t.Date),
                _ => query.OrderByDescending(t => t.Date)
            };

            var results = await query.ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_TransactionList", results);

            return View("Index", results);
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
            var transaction = await _context.Transactions.FindAsync(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [NonAction]
        public void PopulateCategories()
        {
            var CategoryCollection = _context.Categories.ToList();
            Category DefaultCategory = new Category()
            {
                CategoryId = 0,
                Title = "Choose a Category"
            };
            CategoryCollection.Insert(0, DefaultCategory);
            ViewBag.Categories = CategoryCollection;
        }
    }
}
