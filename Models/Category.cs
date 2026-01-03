using System;
using System.ComponentModel.DataAnnotations;

namespace ExpensesTracker.Models;

public enum TransactionType
{
    Expense,
    Income
}
public class Category
{
    public int CategoryId { get; set; }
    [Required]
    [MaxLength(50)]
    public string Title { get; set; }
    [MaxLength(50)]
    public string Icon { get; set; } = "";
    public TransactionType Type { get; set; } = TransactionType.Expense;

    public ICollection<Transaction>? Transactions { get; set; }

}
