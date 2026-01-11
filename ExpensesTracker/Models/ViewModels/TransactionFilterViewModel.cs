using System;
using System.ComponentModel.DataAnnotations;

namespace ExpensesTracker.Models.ViewModels
{
    public class TransactionFilterViewModel
    {
        [Display(Name = "Search")]
        public string? SearchTerm { get; set; }

        [Display(Name = "Category")]
        public int? CategoryId { get; set; }

        [Display(Name = "Type")]
        public string? Type { get; set; }

        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Min Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be positive")]
        public decimal? MinAmount { get; set; }

        [Display(Name = "Max Amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Amount must be positive")]
        public decimal? MaxAmount { get; set; }

        [Display(Name = "Sort By")]
        public string? SortBy { get; set; } = "Date";

        [Display(Name = "Sort Order")]
        public string? SortOrder { get; set; } = "desc";

        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;
    }
}