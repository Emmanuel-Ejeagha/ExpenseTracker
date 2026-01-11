using System.Collections.Generic;

namespace ExpensesTracker.Models.ViewModels
{
    public class MenuItemViewModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Url { get; set; }
        public string IconCss { get; set; }
        public bool Separator { get; set; }
        public List<MenuItemViewModel> Items { get; set; }
    }
}