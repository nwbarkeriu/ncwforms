using JobCompareApp.Models;
using System.Text.RegularExpressions;

namespace JobCompareApp.Services
{
    public static class ReconService
    {
        public static ReconResults ProcessRecon(List<QuickBooksEntry> qbEntries, List<AvionteEntry> avionteEntries, List<DepositDetailEntry>? depositDetails = null)
        {
            var results = new ReconResults();

            // Generate QB Pivot Tables (flat)
            results.QBNameItemPivot = GenerateQBNameItemPivot(qbEntries);
            results.QBRepClientPivot = GenerateQBRepClientPivot(qbEntries);
            results.QBInvoicePivot = GenerateQBInvoicePivot(qbEntries);
            results.QBEmployeeJobSitePivot = GenerateQBEmployeeJobSitePivot(qbEntries);

            // Generate Avionte Pivot Tables (flat)
            results.AvionteBillToNamePivot = GenerateAvionteBillToNamePivot(avionteEntries);

            // Generate Hierarchical Pivot Tables (Excel-style)
            results.QBNameItemHierarchical = GenerateQBNameItemHierarchical(qbEntries);
            results.QBRepClientHierarchical = GenerateQBRepClientHierarchical(qbEntries);
            results.AvionteBillToNameHierarchical = GenerateAvionteBillToNameHierarchical(avionteEntries);

            // Generate Client Summaries
            results.ClientSummaries = GenerateClientSummaries(qbEntries, avionteEntries, depositDetails);

            // Generate Variance Analysis
            results.Variances = GenerateVarianceAnalysis(qbEntries, avionteEntries);

            // Store deposit details
            results.DepositDetails = depositDetails ?? new List<DepositDetailEntry>();

            return results;
        }

        private static List<PivotResult> GenerateQBNameItemPivot(List<QuickBooksEntry> entries)
        {
            return entries
                .GroupBy(e => new { e.Name, e.Item })
                .Select(g => new PivotResult
                {
                    Key = $"{g.Key.Name} - {g.Key.Item}",
                    Amount = g.Sum(x => x.SalesPrice),
                    Quantity = g.Count(),
                    Count = g.Count()
                })
                .OrderBy(p => p.Key)
                .ToList();
        }

        private static List<PivotResult> GenerateQBRepClientPivot(List<QuickBooksEntry> entries)
        {
            return entries
                .Where(e => !string.IsNullOrEmpty(e.Rep))
                .GroupBy(e => new { e.Rep, e.Name })
                .Select(g => new PivotResult
                {
                    Key = $"{g.Key.Rep} - {g.Key.Name}",
                    Amount = g.Sum(x => x.SalesPrice),
                    Quantity = g.Count(),
                    Count = g.Count()
                })
                .OrderBy(p => p.Key)
                .ToList();
        }

        private static List<PivotResult> GenerateQBInvoicePivot(List<QuickBooksEntry> entries)
        {
            return entries
                .GroupBy(e => e.Name)
                .Select(g => new PivotResult
                {
                    Key = g.Key,
                    Amount = g.Sum(x => x.SalesPrice),
                    Quantity = g.Count(),
                    Count = g.GroupBy(x => x.Type).Count() // Approximate invoice count
                })
                .OrderBy(p => p.Key)
                .ToList();
        }

        private static List<PivotResult> GenerateQBEmployeeJobSitePivot(List<QuickBooksEntry> entries)
        {
            return entries
                .Where(e => !string.IsNullOrEmpty(e.Memo))
                .GroupBy(e => new { Employee = e.Item, JobSite = e.Memo })
                .Select(g => new PivotResult
                {
                    Key = $"{g.Key.Employee} - {g.Key.JobSite}",
                    Amount = g.Sum(x => x.SalesPrice),
                    Quantity = g.Count(),
                    Count = g.Count()
                })
                .OrderBy(p => p.Key)
                .ToList();
        }

        private static List<PivotResult> GenerateAvionteBillToNamePivot(List<AvionteEntry> entries)
        {
            return entries
                .GroupBy(e => new { e.BillToName, e.Name })
                .Select(g => new PivotResult
                {
                    Key = $"{g.Key.BillToName} - {g.Key.Name}",
                    Amount = g.Sum(x => x.ItemBill),
                    Quantity = g.Count(),
                    Count = g.Count()
                })
                .OrderBy(p => p.Key)
                .ToList();
        }

        // Hierarchical (Excel-style) Pivot Generation Methods
        private static List<HierarchicalPivotGroup> GenerateQBNameItemHierarchical(List<QuickBooksEntry> entries)
        {
            return entries
                .GroupBy(e => e.Name) // Group by client name first
                .Select(clientGroup => new HierarchicalPivotGroup
                {
                    GroupName = clientGroup.Key,
                    TotalAmount = clientGroup.Sum(x => x.SalesPrice),
                    TotalCount = clientGroup.Count(),
                    IsExpanded = false, // Start collapsed
                    Children = clientGroup
                        .GroupBy(x => x.Item) // Then group by item within each client
                        .Select(itemGroup => new PivotResult
                        {
                            Key = itemGroup.Key,
                            Amount = itemGroup.Sum(x => x.SalesPrice),
                            Quantity = itemGroup.Count(),
                            Count = itemGroup.Count()
                        })
                        .OrderBy(x => x.Key)
                        .ToList()
                })
                .OrderBy(g => g.GroupName)
                .ToList();
        }

        private static List<HierarchicalPivotGroup> GenerateQBRepClientHierarchical(List<QuickBooksEntry> entries)
        {
            return entries
                .Where(e => !string.IsNullOrEmpty(e.Rep))
                .GroupBy(e => e.Rep) // Group by rep first
                .Select(repGroup => new HierarchicalPivotGroup
                {
                    GroupName = repGroup.Key,
                    TotalAmount = repGroup.Sum(x => x.SalesPrice),
                    TotalCount = repGroup.Count(),
                    IsExpanded = false,
                    Children = repGroup
                        .GroupBy(x => x.Name) // Then group by client name within each rep
                        .Select(clientGroup => new PivotResult
                        {
                            Key = clientGroup.Key,
                            Amount = clientGroup.Sum(x => x.SalesPrice),
                            Quantity = clientGroup.Count(),
                            Count = clientGroup.Count()
                        })
                        .OrderBy(x => x.Key)
                        .ToList()
                })
                .OrderBy(g => g.GroupName)
                .ToList();
        }

        private static List<HierarchicalPivotGroup> GenerateAvionteBillToNameHierarchical(List<AvionteEntry> entries)
        {
            return entries
                .GroupBy(e => e.BillToName) // Group by client name first
                .Select(clientGroup => new HierarchicalPivotGroup
                {
                    GroupName = clientGroup.Key,
                    TotalAmount = clientGroup.Sum(x => x.ItemBill),
                    TotalCount = clientGroup.Count(),
                    IsExpanded = false,
                    Children = clientGroup
                        .GroupBy(x => x.Name) // Then group by employee name within each client
                        .Select(empGroup => new PivotResult
                        {
                            Key = empGroup.Key,
                            Amount = empGroup.Sum(x => x.ItemBill),
                            Quantity = empGroup.Count(),
                            Count = empGroup.Count()
                        })
                        .OrderBy(x => x.Key)
                        .ToList()
                })
                .OrderBy(g => g.GroupName)
                .ToList();
        }

        private static List<ClientSummary> GenerateClientSummaries(List<QuickBooksEntry> qbEntries, List<AvionteEntry> avionteEntries, List<DepositDetailEntry>? depositDetails)
        {
            var qbByClient = qbEntries.GroupBy(e => e.Name).ToDictionary(g => g.Key, g => g.ToList());
            var avionteByClient = avionteEntries.GroupBy(e => e.BillToName).ToDictionary(g => g.Key, g => g.ToList());
            var depositByClient = depositDetails?.GroupBy(e => e.ClientName).ToDictionary(g => g.Key, g => g.FirstOrDefault()) ?? new Dictionary<string, DepositDetailEntry?>();

            var allClients = qbByClient.Keys.Union(avionteByClient.Keys).Distinct();

            return allClients.Select(client => new ClientSummary
            {
                ClientName = client, // This maps to "Quickbooks" column
                QBBalance = qbByClient.ContainsKey(client) ? qbByClient[client].Sum(x => x.SalesPrice) : 0,
                AviClientName = client, // "Avi" column (same as QB for now)
                AviBalance = avionteByClient.ContainsKey(client) ? avionteByClient[client].Sum(x => x.ItemBill) : 0,
                PaymentType = depositByClient.ContainsKey(client) && depositByClient[client] != null ? depositByClient[client]!.PaymentMethod : "Research pymt method",
                Team = "", // Not available in current Avionte data structure
                PRRep = "", // Not available in current Avionte data structure
                SendType = "", // Not available in current data
                AccountType = "", // Not available in current data
                BillingNotes = "", // User input field
                Notes = "",
                Status = "",
                QBInvoiceCount = qbByClient.ContainsKey(client) ? qbByClient[client].GroupBy(x => x.Type).Count() : 0,
                AvionteRecordCount = avionteByClient.ContainsKey(client) ? avionteByClient[client].Count : 0,
                Employees = avionteByClient.ContainsKey(client) ? avionteByClient[client].Select(x => x.Name).Distinct().ToList() : new List<string>(),
                JobSites = qbByClient.ContainsKey(client) ? qbByClient[client].Where(x => !string.IsNullOrEmpty(x.Memo)).Select(x => x.Memo).Distinct().ToList() : new List<string>()
            }).OrderBy(s => s.ClientName).ToList();
        }

        private static List<VarianceEntry> GenerateVarianceAnalysis(List<QuickBooksEntry> qbEntries, List<AvionteEntry> avionteEntries)
        {
            var variances = new List<VarianceEntry>();

            // Group by client and compare totals
            var qbByClient = qbEntries.GroupBy(e => e.Name).ToDictionary(g => g.Key, g => g.ToList());
            var avionteByClient = avionteEntries.GroupBy(e => e.BillToName).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var client in qbByClient.Keys.Union(avionteByClient.Keys).Distinct())
            {
                var qbAmount = qbByClient.ContainsKey(client) ? qbByClient[client].Sum(x => x.SalesPrice) : 0;
                var avionteAmount = avionteByClient.ContainsKey(client) ? avionteByClient[client].Sum(x => x.ItemBill) : 0;
                var variance = qbAmount - avionteAmount;

                if (Math.Abs(variance) > 0.01m) // Only include significant variances
                {
                    variances.Add(new VarianceEntry
                    {
                        ClientName = client,
                        EmployeeName = "Total",
                        JobSite = "All",
                        QBAmount = qbAmount,
                        AvionteAmount = avionteAmount,
                        VarianceType = "Total",
                        Notes = $"Client total variance: {variance:C}"
                    });
                }
            }

            // Add detailed employee-level variances
            foreach (var client in qbByClient.Keys.Intersect(avionteByClient.Keys))
            {
                var qbEmployees = qbByClient[client].GroupBy(x => x.Item).ToDictionary(g => g.Key, g => g.Sum(x => x.SalesPrice));
                var avionteEmployees = avionteByClient[client].GroupBy(x => x.Name).ToDictionary(g => g.Key, g => g.Sum(x => x.ItemBill));

                foreach (var employee in qbEmployees.Keys.Union(avionteEmployees.Keys).Distinct())
                {
                    var qbEmpAmount = qbEmployees.ContainsKey(employee) ? qbEmployees[employee] : 0;
                    var avionteEmpAmount = avionteEmployees.ContainsKey(employee) ? avionteEmployees[employee] : 0;
                    var empVariance = qbEmpAmount - avionteEmpAmount;

                    if (Math.Abs(empVariance) > 0.01m)
                    {
                        variances.Add(new VarianceEntry
                        {
                            ClientName = client,
                            EmployeeName = employee,
                            JobSite = "Multiple",
                            QBAmount = qbEmpAmount,
                            AvionteAmount = avionteEmpAmount,
                            VarianceType = "Employee",
                            Notes = $"Employee variance: {empVariance:C}"
                        });
                    }
                }
            }

            return variances.OrderBy(v => v.ClientName).ThenBy(v => v.EmployeeName).ToList();
        }

        public static List<DepositDetailEntry> ProcessDepositDetail(byte[] fileBytes)
        {
            var entries = new List<DepositDetailEntry>();

            try
            {
                using var stream = new MemoryStream(fileBytes);
                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                var headerRow = worksheet.FirstRowUsed();
                var columnMap = new Dictionary<string, int>();
                foreach (var cell in headerRow.CellsUsed())
                {
                    var header = cell.GetString().Trim();
                    columnMap[header] = cell.Address.ColumnNumber;
                }

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    if (string.IsNullOrWhiteSpace(row.Cell(columnMap["Type"]).GetString()))
                        continue;

                    var clientName = row.Cell(columnMap["Name"]).GetString().Trim();
                    var amount = row.Cell(columnMap["Amount"]).GetValue<decimal>();
                    var date = row.Cell(columnMap["Date"]).GetDateTime();
                    
                    // Determine payment method
                    string paymentMethod = "ACH";
                    string checkNumber = "";
                    
                    if (columnMap.ContainsKey("Num"))
                    {
                        var numValue = row.Cell(columnMap["Num"]).GetString().Trim();
                        if (!string.IsNullOrEmpty(numValue))
                        {
                            paymentMethod = "Check";
                            checkNumber = numValue;
                        }
                    }

                    entries.Add(new DepositDetailEntry
                    {
                        ClientName = clientName,
                        PaymentMethod = paymentMethod,
                        Amount = amount,
                        Date = date,
                        CheckNumber = checkNumber
                    });
                }
            }
            catch (Exception)
            {
                // Return empty list if processing fails
            }

            return entries.OrderBy(e => e.ClientName).ToList();
        }
    }
}
