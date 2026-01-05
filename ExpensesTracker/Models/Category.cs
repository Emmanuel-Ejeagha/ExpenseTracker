using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExpensesTracker.Models;


public class Category
{
    public int CategoryId { get; set; }
    [Required]
    [MaxLength(50)]
    public string Title { get; set; }
    [MaxLength(50)]
    public string Icon { get; set; } = "";
    public string Type { get; set; } = "Expense";
    [NotMapped]
    public string? TitleWithIcon {
        get
        {
            return this.Icon + " " + Title;
        } }

    public ICollection<Transaction>? Transactions { get; set; }

}
