using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace ExpensesTracker.Models;

[Table("Transactions")]
public class Transaction
{
    [Key]
    [Display(Name = "Transaction ID")]
    public int TransactionId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    [Column(TypeName = "decimal(18,2)")]
    [DataType(DataType.Currency)]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters")]
    [Display(Name = "Note/Description")]
    public string? Note { get; set; }

    [Required(ErrorMessage = "Date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Category is required")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    // Navigation property
    [ForeignKey("CategoryId")]
    [Display(Name = "Category")]
    public virtual Category? Category { get; set; }

    // Computed properties
    [NotMapped]
    [Display(Name = "Category")]
    public string? CategoryTitleWithIcon => Category?.TitleWithIcon ?? "Uncategorized";

    [NotMapped]
    [Display(Name = "Amount")]
    public string FormattedAmount
    {
        get
        {
            var isExpense = Category?.Type == "Expense";
            var sign = isExpense ? "- " : "+ ";
            var amount = Amount.ToString("C", CultureInfo.CreateSpecificCulture("en-NG"));
            return sign + amount;
        }
    }

    [NotMapped]
    [Display(Name = "Type")]
    public string Type => Category?.Type ?? "Unknown";

    [NotMapped]
    public string? CategoryIcon => Category?.Icon;

    [NotMapped]
    public string DateFormatted => Date.ToString("MMM dd, yyyy");

    [NotMapped]
    public string DateShort => Date.ToString("MM/dd");

    [NotMapped]
    public string TimeAgo
    {
        get
        {
            var timeSpan = DateTime.Today - Date;
            if (timeSpan.TotalDays < 1)
                return "Today";
            if (timeSpan.TotalDays < 2)
                return "Yesterday";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";
            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }
    }

    // Helper methods
    public string GetAmountColorClass()
    {
        return Type.ToLower() switch
        {
            "income" => "text-success",
            "expense" => "text-danger",
            _ => "text-muted"
        };
    }

    public string GetTypeBadgeClass()
    {
        return Type.ToLower() switch
        {
            "income" => "badge bg-success",
            "expense" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }

    public bool IsRecent()
    {
        return (DateTime.Today - Date).TotalDays <= 7;
    }
}