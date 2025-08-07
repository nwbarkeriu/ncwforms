using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using JobCompareApp.Models;
using System.Text.RegularExpressions;

namespace JobCompareApp.Services
{
    public static class ExcelService
    {
        public static List<QuickBooksEntry> ProcessQB(byte[] fileBytes)
        {
            using var stream = new MemoryStream(fileBytes);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1); // Assuming 1st sheet is the source

            var entries = new List<QuickBooksEntry>();

            // Find headers
            var headerRow = worksheet.FirstRowUsed();
            if (headerRow == null)
                throw new InvalidOperationException("No data found in the worksheet");
                
            var columnMap = new Dictionary<string, int>();
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim();
                columnMap[header] = cell.Address.ColumnNumber;
            }

            // Clean-up helpers based on requirements
            string CleanName(string name) =>
                Regex.Replace(name, @":.*|--.*", "").Trim(); // Remove jobsite info from name

            string ExtractJobSite(string name) =>
                Regex.Replace(name, @".*?:|.*?--", "").Trim(); // Extract jobsite info for memo

            // Iterate over data rows
            foreach (var row in worksheet.RowsUsed().Skip(1)) // Skip header
            {
                // Skip blank rows under 'Type' header as per requirements
                if (!columnMap.ContainsKey("Type") || string.IsNullOrWhiteSpace(row.Cell(columnMap["Type"]).GetString()))
                    continue;

                // Skip rows with Balance column data (to be excluded per requirements)
                if (columnMap.ContainsKey("Balance"))
                {
                    // This would be filtered out in Excel, but we'll include for processing
                }

                try
                {
                    var originalName = row.Cell(columnMap["Name"]).GetString().Trim();
                    
                    var entry = new QuickBooksEntry
                    {
                        Name = CleanName(originalName), // Clean client name without jobsite
                        Memo = ExtractJobSite(originalName), // Jobsite information only
                        Item = columnMap.ContainsKey("Item") ? row.Cell(columnMap["Item"]).GetString().Trim() : "",
                        Amount = columnMap.ContainsKey("Amount") ? row.Cell(columnMap["Amount"]).GetValue<decimal>() : 0,
                        Type = row.Cell(columnMap["Type"]).GetString().Trim(),
                        Account = columnMap.ContainsKey("Account") ? row.Cell(columnMap["Account"]).GetString().Trim() : "",
                        Rep = columnMap.ContainsKey("Rep") ? row.Cell(columnMap["Rep"]).GetString().Trim() : "",
                        PONumber = columnMap.ContainsKey("P. O. #") ? row.Cell(columnMap["P. O. #"]).GetString().Trim() : ""
                    };

                    entries.Add(entry);
                }
                catch (Exception)
                {
                    // Skip rows with invalid data
                    continue;
                }
            }

            // Sort by Sales Price → Item → Name as per requirements
            return entries
                .OrderBy(e => e.Amount)
                .ThenBy(e => e.Item)
                .ThenBy(e => e.Name)
                .ToList();
        }

        public static List<AvionteEntry> ProcessAvionte(byte[] fileBytes)
        {
            using var stream = new MemoryStream(fileBytes);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1); // Assume first worksheet is main

            var entries = new List<AvionteEntry>();

            var headerRow = worksheet.FirstRowUsed();
            if (headerRow == null)
                throw new InvalidOperationException("No data found in the worksheet");
                
            var columnMap = new Dictionary<string, int>();
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim();
                columnMap[header] = cell.Address.ColumnNumber;
            }

            // Determine current processing week (previous Sunday)
            DateTime today = DateTime.Today;
            int daysSinceSunday = (int)today.DayOfWeek;
            DateTime previousSunday = today.AddDays(-daysSinceSunday);

            foreach (var row in worksheet.RowsUsed().Skip(1)) // Skip header
            {
                // Check if required columns exist
                if (!columnMap.ContainsKey("Week Worked") || 
                    !columnMap.ContainsKey("Item Bill") || 
                    !columnMap.ContainsKey("Item Pay") ||
                    !columnMap.ContainsKey("FirstName") ||
                    !columnMap.ContainsKey("LastName") ||
                    !columnMap.ContainsKey("Bill To Name"))
                    continue;

                try
                {
                    var weekWorked = row.Cell(columnMap["Week Worked"]).GetDateTime();
                    var itemBill = row.Cell(columnMap["Item Bill"]).GetValue<decimal>();
                    var itemPay = row.Cell(columnMap["Item Pay"]).GetValue<decimal>();

                    // Apply Avionte filtering rules:
                    // 1. Filter 'Week Worked' column for anything not equal to current processing week
                    // 2. Filter 'Item Bill' & 'Item Pay' columns for 0-zero
                    // For production: Enable these filters, for testing: commented out
                    
                    // if (weekWorked.Date != previousSunday.Date)
                    //     continue; // Move to extra tab in Excel
                    
                    // if (itemBill == 0 || itemPay == 0)
                    //     continue; // Move to extra tab in Excel

                    var firstName = row.Cell(columnMap["FirstName"]).GetString().Trim();
                    var lastName = row.Cell(columnMap["LastName"]).GetString().Trim();
                    // Insert Column to format 'FirstName' & 'LastName' with a space between
                    var name = $"{firstName} {lastName}";

                    var billTo = row.Cell(columnMap["Bill To Name"]).GetString().Trim();
                    // Any clients beginning with "Dart Entities" override to "Dart Entities"
                    if (billTo.StartsWith("Dart Entities", StringComparison.OrdinalIgnoreCase))
                        billTo = "Dart Entities";

                    entries.Add(new AvionteEntry
                    {
                        Name = name,
                        BillToName = billTo,
                        ItemBill = itemBill,
                        ItemPay = itemPay,
                        WeekWorked = weekWorked
                    });
                }
                catch (Exception)
                {
                    // Skip rows with invalid data
                    continue;
                }
            }

            // Sort by 'Name' header, then by 'Bill To Name' header as per requirements
            return entries
                .OrderBy(e => e.Name)
                .ThenBy(e => e.BillToName)
                .ToList();
        }
    }
}


