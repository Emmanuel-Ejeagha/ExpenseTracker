using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpensesTracker.Models;

public class Transaction
{
    public int TransactionId { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    [Range(1,int.MaxValue,ErrorMessage = "Amount should be greater than 0")]
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    [Range(1,int.MaxValue,ErrorMessage = "Please select a category.")]
    public int CategoryId { get; set; }
    public Category? Category { get; set; }


    [NotMapped]
    public string? CategoryTitleWithIcon
    {
        get
        {
            return Category == null ? "" : Category.Icon + " " + Category.Title;
        }
    }

    [NotMapped]
    public string? FormattedAmount
    {
        get
        {
            return ((Category == null || Category.Type == "Expense") ? "- " : "+ ") + Amount.ToString("'₦'###,###,##0");

        }
    }
}
