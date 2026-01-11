using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using ExpensesTracker.Models;
using ExpensesTracker.Models.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Grid;

namespace ExpensesTracker.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FileResult> ExportToCsvAsync(List<Transaction> transactions)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Date,Category,Type,Amount,Note");
            
            // Data
            foreach (var transaction in transactions)
            {
                var line = $"\"{transaction.Date:yyyy-MM-dd}\"," +
                          $"\"{transaction.Category?.Title}\"," +
                          $"\"{transaction.Category?.Type}\"," +
                          $"\"{transaction.Amount}\"," +
                          $"\"{transaction.Note?.Replace("\"", "\"\"")}\"";
                csv.AppendLine(line);
            }
            
            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return new FileContentResult(bytes, "text/csv")
            {
                FileDownloadName = $"transactions-{DateTime.Now:yyyyMMdd}.csv"
            };
        }

        public async Task<FileResult> ExportToExcelAsync(List<Transaction> transactions)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Transactions");
            
            // Header
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Category";
            worksheet.Cell(1, 3).Value = "Type";
            worksheet.Cell(1, 4).Value = "Amount";
            worksheet.Cell(1, 5).Value = "Note";
            
            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            
            // Data
            int row = 2;
            foreach (var transaction in transactions)
            {
                worksheet.Cell(row, 1).Value = transaction.Date;
                worksheet.Cell(row, 1).Style.DateFormat.Format = "yyyy-MM-dd";
                worksheet.Cell(row, 2).Value = transaction.Category?.Title;
                worksheet.Cell(row, 3).Value = transaction.Category?.Type;
                worksheet.Cell(row, 4).Value = transaction.Amount;
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 5).Value = transaction.Note;
                row++;
            }
            
            worksheet.Columns().AdjustToContents();
            
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;
            
            return new FileContentResult(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                FileDownloadName = $"transactions-{DateTime.Now:yyyyMMdd}.xlsx"
            };
        }

        public async Task<FileResult> ExportToPdfAsync(List<Transaction> transactions)
        {
            using var pdfDocument = new PdfDocument();
            pdfDocument.PageSettings.Orientation = PdfPageOrientation.Landscape;
            
            // Add a page
            var page = pdfDocument.Pages.Add();
            
            // Create PDF grid
            var pdfGrid = new PdfGrid();
            
            // Add data
            var data = transactions.Select(t => new
            {
                Date = t.Date.ToString("yyyy-MM-dd"),
                Category = t.Category?.Title ?? "N/A",
                Type = t.Category?.Type ?? "N/A",
                Amount = t.Amount.ToString("C", CultureInfo.CreateSpecificCulture("en-NG")),
                Note = t.Note ?? ""
            }).ToList();
            
            pdfGrid.DataSource = data;
            
            // Style grid
            pdfGrid.Style.CellPadding = new PdfPaddings(5, 5, 5, 5);
            pdfGrid.Style.Font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
            
            // Draw grid
            pdfGrid.Draw(page, new PointF(10, 30));
            
            // Add title
            var title = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
            page.Graphics.DrawString("Transaction Report", title, PdfBrushes.Black, new PointF(10, 10));
            
            using var stream = new MemoryStream();
            pdfDocument.Save(stream);
            stream.Position = 0;
            
            return new FileContentResult(stream.ToArray(), "application/pdf")
            {
                FileDownloadName = $"transactions-{DateTime.Now:yyyyMMdd}.pdf"
            };
        }

        public async Task<List<Transaction>> GetTransactionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.TransactionId)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalByCategoryAsync(int categoryId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Transactions
                .Where(t => t.CategoryId == categoryId);
                
            if (startDate.HasValue)
                query = query.Where(t => t.Date >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(t => t.Date <= endDate.Value);
                
            return await query.SumAsync(t => t.Amount);
        }

        public async Task<List<Transaction>> SearchTransactionsAsync(string query, int? categoryId, DateTime? startDate, DateTime? endDate)
        {
            var dbQuery = _context.Transactions
                .Include(t => t.Category)
                .AsQueryable();
                
            if (!string.IsNullOrEmpty(query))
            {
                dbQuery = dbQuery.Where(t => 
                    t.Note.Contains(query) || 
                    t.Category.Title.Contains(query));
            }
            
            if (categoryId.HasValue && categoryId > 0)
                dbQuery = dbQuery.Where(t => t.CategoryId == categoryId);
                
            if (startDate.HasValue)
                dbQuery = dbQuery.Where(t => t.Date >= startDate.Value);
                
            if (endDate.HasValue)
                dbQuery = dbQuery.Where(t => t.Date <= endDate.Value);
                
            return await dbQuery
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }
    }
}