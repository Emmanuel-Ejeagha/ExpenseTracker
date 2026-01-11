using System;
using System.ComponentModel.DataAnnotations;

namespace ExpensesTracker.Models.ViewModels
{
    public class QuickTransactionViewModel
    {
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        [Display(Name = "Amount")]
        public decimal Amount { get; set; }

        [StringLength(200, ErrorMessage = "Note cannot exceed 200 characters")]
        [Display(Name = "Note")]
        public string? Note { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime? Date { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Type")]
        public string? Type { get; set; }
    }
}