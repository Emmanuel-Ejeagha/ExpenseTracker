using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpensesTracker.Models;

public class Transaction
{
    public int TransactionId { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;

    public int CategoryId { get; set; }
    public Category Category { get; set; }
}
