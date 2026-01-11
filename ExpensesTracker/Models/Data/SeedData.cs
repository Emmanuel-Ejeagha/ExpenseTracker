using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpensesTracker.Models.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(AppDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Only seed if there are no categories
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    // Income Categories
                    new Category { Title = "Salary", Icon = "💰", Type = "Income" },
                    new Category { Title = "Freelance", Icon = "💼", Type = "Income" },
                    new Category { Title = "Investment", Icon = "📈", Type = "Income" },
                    new Category { Title = "Business", Icon = "🏢", Type = "Income" },
                    new Category { Title = "Gifts", Icon = "🎁", Type = "Income" },
                    
                    // Expense Categories
                    new Category { Title = "Food & Dining", Icon = "🍔", Type = "Expense" },
                    new Category { Title = "Transportation", Icon = "🚗", Type = "Expense" },
                    new Category { Title = "Shopping", Icon = "🛍️", Type = "Expense" },
                    new Category { Title = "Entertainment", Icon = "🎬", Type = "Expense" },
                    new Category { Title = "Bills & Utilities", Icon = "💡", Type = "Expense" },
                    new Category { Title = "Healthcare", Icon = "🏥", Type = "Expense" },
                    new Category { Title = "Education", Icon = "📚", Type = "Expense" },
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
                Console.WriteLine("Categories seeded successfully.");
            }

            // Only seed sample transactions if none exist AND we're in development
            if (!await context.Transactions.AnyAsync())
            {
                var categories = await context.Categories.ToListAsync();
                var random = new Random();
                var transactions = new List<Transaction>();

                // Generate a few sample transactions
                for (int i = 0; i < 10; i++)
                {
                    var date = DateTime.Today.AddDays(-random.Next(0, 30));
                    var category = categories[random.Next(categories.Count)];
                    var amount = category.Type == "Income" ? 
                        random.Next(10000, 100000) : 
                        random.Next(100, 50000);
                    
                    var transaction = new Transaction
                    {
                        Amount = amount,
                        Note = GetSampleNote(category.Title, category.Type == "Income"),
                        Date = date,
                        CategoryId = category.CategoryId
                    };

                    transactions.Add(transaction);
                }

                await context.Transactions.AddRangeAsync(transactions);
                await context.SaveChangesAsync();
                Console.WriteLine("Sample transactions seeded successfully.");
            }
        }

        private static string GetSampleNote(string category, bool isIncome)
        {
            if (isIncome)
            {
                var notes = new[]
                {
                    $"Received {category} payment",
                    $"Monthly {category} income",
                    $"Payment for {category} services",
                    $"Client payment - {category}"
                };
                return notes[new Random().Next(notes.Length)];
            }
            else
            {
                var notes = new[]
                {
                    $"Spent on {category}",
                    $"Monthly {category} expense",
                    $"Payment for {category}",
                    $"Bought {category} items"
                };
                return notes[new Random().Next(notes.Length)];
            }
        }
    }
}