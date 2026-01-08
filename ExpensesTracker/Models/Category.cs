using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ExpensesTracker.Models;


public class Category
{
    public int CategoryId { get; set; }
    [Required(ErrorMessage = "Title is required")]
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
}
