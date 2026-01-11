using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExpensesTracker.Models;

[Table("Categories")]
public class Category
{
    [Key]
    [Display(Name = "Category ID")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Category name is required")]
    [StringLength(50, ErrorMessage = "Category name cannot exceed 50 characters")]
    [Display(Name = "Category Name")]
    public string Title { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Icon cannot exceed 50 characters")]
    [Display(Name = "Icon")]
    public string Icon { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type is required")]
    [RegularExpression("^(Income|Expense)$", ErrorMessage = "Type must be either Income or Expense")]
    [Display(Name = "Type")]
    public string Type { get; set; } = "Expense";

    [NotMapped]
    [Display(Name = "Category")]
    public string TitleWithIcon => $"{Icon} {Title}";

    [NotMapped]
    [Display(Name = "Transaction Count")]
    public int TransactionCount { get; set; }

    [NotMapped]
    [Display(Name = "Total Amount")]
    [DataType(DataType.Currency)]
    public decimal TotalAmount { get; set; }

    // Navigation property
    [JsonIgnore]
    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    // Helper methods
    public string GetTypeBadgeClass()
    {
        return Type.ToLower() switch
        {
            "income" => "badge bg-success",
            "expense" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }

    public string GetTypeIcon()
    {
        return Type.ToLower() switch
        {
            "income" => "📈",
            "expense" => "📉",
            _ => "📊"
        };
    }

    public bool CanDelete()
    {
        return !Transactions.Any();
    }
}