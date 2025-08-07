using JobCompareApp.Models;
using JobCompareApp.Services;
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
                    Amount = g.Sum(x => x.Amount),
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
                    Amount = g.Sum(x => x.Amount),
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
                    Amount = g.Sum(x => x.Amount),
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
                    Amount = g.Sum(x => x.Amount),
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
                    TotalAmount = clientGroup.Sum(x => x.Amount),
                    TotalCount = clientGroup.Count(),
                    IsExpanded = false, // Start collapsed
                    Children = clientGroup
                        .GroupBy(x => x.Item) // Then group by item within each client
                        .Select(itemGroup => new PivotResult
                        {
                            Key = itemGroup.Key,
                            Amount = itemGroup.Sum(x => x.Amount),
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
                    TotalAmount = repGroup.Sum(x => x.Amount),
                    TotalCount = repGroup.Count(),
                    IsExpanded = false,
                    Children = repGroup
                        .GroupBy(x => x.Name) // Then group by client name within each rep
                        .Select(clientGroup => new PivotResult
                        {
                            Key = clientGroup.Key,
                            Amount = clientGroup.Sum(x => x.Amount),
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
                .GroupBy(e => e.BillToName) // Group by raw Avionte client name (as it should be)
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

            // Calculate amounts by client for the matching algorithm
            var qbAmountsByClient = qbByClient.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Sum(x => x.Amount));
            var avionteAmountsByClient = avionteByClient.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Sum(x => x.ItemBill));

            // Use the new client matching system with amount-based matching
            var qbClientNames = qbByClient.Keys;
            var avionteClientNames = avionteByClient.Keys;
            var clientMatches = ClientMatcher.CreateUnifiedMatches(qbClientNames, avionteClientNames, qbAmountsByClient, avionteAmountsByClient);

            return clientMatches.Values.Select(match => new ClientSummary
            {
                ClientName = match.QBName ?? match.UnifiedName, // This maps to "Quickbooks" column
                QBBalance = match.HasQBData && qbByClient.ContainsKey(match.QBName!) ? qbByClient[match.QBName!].Sum(x => x.Amount) : 0,
                AviClientName = match.AvionteName ?? "", // "Avi" column (show actual Avionte name)
                AviBalance = match.HasAvionteData && avionteByClient.ContainsKey(match.AvionteName!) ? avionteByClient[match.AvionteName!].Sum(x => x.ItemBill) : 0,
                PaymentType = GetPaymentTypeForMatch(match, depositByClient),
                Team = "", // Not available in current Avionte data structure
                PRRep = "", // Not available in current Avionte data structure
                SendType = "", // Not available in current data
                AccountType = "", // Not available in current data
                BillingNotes = "", // User input field
                Notes = GetMatchNotes(match),
                Status = "",
                QBInvoiceCount = match.HasQBData && qbByClient.ContainsKey(match.QBName!) ? qbByClient[match.QBName!].GroupBy(x => x.Type).Count() : 0,
                AvionteRecordCount = match.HasAvionteData && avionteByClient.ContainsKey(match.AvionteName!) ? avionteByClient[match.AvionteName!].Count : 0,
                Employees = match.HasAvionteData && avionteByClient.ContainsKey(match.AvionteName!) ? avionteByClient[match.AvionteName!].Select(x => x.Name).Distinct().ToList() : new List<string>(),
                JobSites = match.HasQBData && qbByClient.ContainsKey(match.QBName!) ? qbByClient[match.QBName!].Where(x => !string.IsNullOrEmpty(x.Memo)).Select(x => x.Memo).Distinct().ToList() : new List<string>()
            }).OrderBy(s => s.ClientName).ToList();
        }

        private static string GetPaymentTypeForMatch(ClientMatch match, Dictionary<string, DepositDetailEntry?> depositByClient)
        {
            // Try to find payment type by QB name first, then Avionte name
            if (match.HasQBData && depositByClient.ContainsKey(match.QBName!) && depositByClient[match.QBName!] != null)
                return depositByClient[match.QBName!]!.PaymentMethod;
            
            if (match.HasAvionteData && depositByClient.ContainsKey(match.AvionteName!) && depositByClient[match.AvionteName!] != null)
                return depositByClient[match.AvionteName!]!.PaymentMethod;

            return "Research pymt method";
        }

        private static string GetMatchNotes(ClientMatch match)
        {
            return match.MatchType switch
            {
                MatchType.ExceptionRule => "Matched via exception rule",
                MatchType.AmountZeroOut => "Matched via amounts that zero out + similar names (likely same client)",
                MatchType.Normalized => "Matched after name normalization",
                MatchType.FirstWords => "Matched on first words",
                MatchType.Fuzzy => "Fuzzy match (contains)",
                MatchType.QBOnly => "QB only - no Avionte match",
                MatchType.AvionteOnly => "Avionte only - no QB match",
                _ => ""
            };
        }

        private static List<VarianceEntry> GenerateVarianceAnalysis(List<QuickBooksEntry> qbEntries, List<AvionteEntry> avionteEntries)
        {
            var variances = new List<VarianceEntry>();

            var qbByClient = qbEntries.GroupBy(e => e.Name).ToDictionary(g => g.Key, g => g.ToList());
            var avionteByClient = avionteEntries.GroupBy(e => e.BillToName).ToDictionary(g => g.Key, g => g.ToList());

            // Calculate amounts by client for the matching algorithm
            var qbAmountsByClient = qbByClient.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Sum(x => x.Amount));
            var avionteAmountsByClient = avionteByClient.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Sum(x => x.ItemBill));

            // STEP 1: Use client matching system to identify matched clients
            var qbClientNames = qbByClient.Keys;
            var avionteClientNames = avionteByClient.Keys;
            var clientMatches = ClientMatcher.CreateUnifiedMatches(qbClientNames, avionteClientNames, qbAmountsByClient, avionteAmountsByClient);

            // STEP 2: For each matched client, perform detailed employee-level analysis
            foreach (var clientMatch in clientMatches.Values.Where(m => m.IsPerfectMatch))
            {
                var qbEntries_client = qbByClient[clientMatch.QBName!];
                var avionteEntries_client = avionteByClient[clientMatch.AvionteName!];

                // Calculate employee amounts for this client
                var qbAmountsByEmployee = qbEntries_client.GroupBy(x => x.Item).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
                var avionteAmountsByEmployee = avionteEntries_client.GroupBy(x => x.Name).ToDictionary(g => g.Key, g => g.Sum(x => x.ItemBill));

                // STEP 3: Match employees within this client using enhanced employee matching with context
                var qbEmployeeNames = qbAmountsByEmployee.Keys;
                var avionteEmployeeNames = avionteAmountsByEmployee.Keys;
                
                // Use enhanced matching that considers client context and amounts
                var employeeMatches = new Dictionary<string, EmployeeMatch>(StringComparer.OrdinalIgnoreCase);
                var processedAvionte = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Process QB employees first with enhanced context-aware matching
                foreach (var qbEmpName in qbEmployeeNames)
                {
                    var qbAmount = qbAmountsByEmployee[qbEmpName];
                    
                    // Try enhanced matching with context
                    var avionteMatch = EmployeeMatcher.FindBestEmployeeMatchWithContext(
                        qbEmpName, 
                        avionteEmployeeNames.Where(a => !processedAvionte.Contains(a)), 
                        clientMatch.UnifiedName,
                        qbAmount,
                        avionteAmountsByEmployee);
                    
                    var unifiedName = qbEmpName; // Use QB name as unified

                    if (avionteMatch != null)
                    {
                        var avionteAmount = avionteAmountsByEmployee[avionteMatch];
                        employeeMatches[unifiedName] = new EmployeeMatch
                        {
                            UnifiedName = unifiedName,
                            QBName = qbEmpName,
                            AvionteName = avionteMatch,
                            QBAmount = qbAmount,
                            AvionteAmount = avionteAmount,
                            MatchType = GetEnhancedEmployeeMatchType(qbEmpName, avionteMatch, qbAmount, avionteAmount)
                        };
                        processedAvionte.Add(avionteMatch);
                    }
                    else
                    {
                        employeeMatches[unifiedName] = new EmployeeMatch
                        {
                            UnifiedName = unifiedName,
                            QBName = qbEmpName,
                            AvionteName = null,
                            QBAmount = qbAmount,
                            AvionteAmount = 0,
                            MatchType = EmployeeMatchType.QBOnly
                        };
                    }
                }

                // Process remaining unmatched Avionte employees
                foreach (var avionteEmpName in avionteEmployeeNames.Where(a => !processedAvionte.Contains(a)))
                {
                    var avionteAmount = avionteAmountsByEmployee[avionteEmpName];
                    employeeMatches[avionteEmpName] = new EmployeeMatch
                    {
                        UnifiedName = avionteEmpName,
                        QBName = null,
                        AvionteName = avionteEmpName,
                        QBAmount = 0,
                        AvionteAmount = avionteAmount,
                        MatchType = EmployeeMatchType.AvionteOnly
                    };
                }

                // STEP 4: Generate variances only for TRUE mismatches
                foreach (var empMatch in employeeMatches.Values)
                {
                    // Only create variance entries for significant differences
                    if (empMatch.HasSignificantVariance)
                    {
                        // Determine variance type based on matching status
                        string varianceType;
                        string notes;

                        if (empMatch.MatchType == EmployeeMatchType.QBOnly)
                        {
                            varianceType = "QB Only Employee";
                            notes = $"Employee exists only in QuickBooks for client {clientMatch.UnifiedName}";
                        }
                        else if (empMatch.MatchType == EmployeeMatchType.AvionteOnly)
                        {
                            varianceType = "Avionte Only Employee";
                            notes = $"Employee exists only in Avionte for client {clientMatch.UnifiedName}";
                        }
                        else if (empMatch.AmountsZeroOut)
                        {
                            // Skip this - amounts zero out, so no real variance
                            continue;
                        }
                        else
                        {
                            varianceType = "Employee Amount Variance";
                            notes = $"Employee amounts differ: QB ${empMatch.QBAmount:N2} vs Avionte ${empMatch.AvionteAmount:N2} (Match: {empMatch.MatchType})";
                        }

                        variances.Add(new VarianceEntry
                        {
                            ClientName = clientMatch.UnifiedName,
                            EmployeeName = empMatch.UnifiedName,
                            JobSite = "Multiple",
                            QBAmount = empMatch.QBAmount,
                            AvionteAmount = empMatch.AvionteAmount,
                            VarianceType = varianceType,
                            Notes = notes
                        });
                    }
                }

                // STEP 5: Check for client-level variance (after accounting for employee matches)
                var clientQBTotal = qbAmountsByEmployee.Values.Sum();
                var clientAvionteTotal = avionteAmountsByEmployee.Values.Sum();
                var clientVariance = clientQBTotal - clientAvionteTotal;

                // Only add client-level variance if there's a significant difference
                // and it's not just due to unmatched employees (which we already counted above)
                if (Math.Abs(clientVariance) > 0.01m)
                {
                    var matchedEmployeeVariance = employeeMatches.Values
                        .Where(em => em.IsPerfectMatch && !em.AmountsZeroOut)
                        .Sum(em => em.Variance);

                    var unmatchedVariance = employeeMatches.Values
                        .Where(em => !em.IsPerfectMatch)
                        .Sum(em => em.Variance);

                    // If the total client variance is significantly different from the sum of employee variances,
                    // there might be a client-level issue
                    var accountedVariance = matchedEmployeeVariance + unmatchedVariance;
                    var unexplainedVariance = clientVariance - accountedVariance;

                    if (Math.Abs(unexplainedVariance) > 0.01m)
                    {
                        variances.Add(new VarianceEntry
                        {
                            ClientName = clientMatch.UnifiedName,
                            EmployeeName = "Client Total",
                            JobSite = "All",
                            QBAmount = clientQBTotal,
                            AvionteAmount = clientAvionteTotal,
                            VarianceType = "Client Level Variance",
                            Notes = $"Client total variance: {clientVariance:C} (Match: {clientMatch.MatchType}) - Unexplained: {unexplainedVariance:C}"
                        });
                    }
                }
            }

            // STEP 6: Add variances for completely unmatched clients
            foreach (var clientMatch in clientMatches.Values.Where(m => !m.IsPerfectMatch))
            {
                if (clientMatch.MatchType == MatchType.QBOnly)
                {
                    var qbAmount = qbByClient[clientMatch.QBName!].Sum(x => x.Amount);
                    variances.Add(new VarianceEntry
                    {
                        ClientName = clientMatch.QBName!,
                        EmployeeName = "Total",
                        JobSite = "All",
                        QBAmount = qbAmount,
                        AvionteAmount = 0,
                        VarianceType = "QB Only Client",
                        Notes = "Client exists only in QuickBooks"
                    });
                }
                else if (clientMatch.MatchType == MatchType.AvionteOnly)
                {
                    var avionteAmount = avionteByClient[clientMatch.AvionteName!].Sum(x => x.ItemBill);
                    variances.Add(new VarianceEntry
                    {
                        ClientName = clientMatch.AvionteName!,
                        EmployeeName = "Total",
                        JobSite = "All",
                        QBAmount = 0,
                        AvionteAmount = avionteAmount,
                        VarianceType = "Avionte Only Client",
                        Notes = "Client exists only in Avionte"
                    });
                }
            }

            return variances.OrderBy(v => v.ClientName).ThenBy(v => v.EmployeeName).ToList();
        }

        /// <summary>
        /// Enhanced employee match type detection with context awareness
        /// </summary>
        private static EmployeeMatchType GetEnhancedEmployeeMatchType(string qbName, string avionteName, decimal qbAmount, decimal avionteAmount)
        {
            if (string.Equals(qbName, avionteName, StringComparison.OrdinalIgnoreCase))
                return EmployeeMatchType.Exact;

            var normalizedQB = EmployeeMatcher.NormalizeEmployeeName(qbName);
            var normalizedAvionte = EmployeeMatcher.NormalizeEmployeeName(avionteName);

            if (string.Equals(normalizedQB, normalizedAvionte, StringComparison.OrdinalIgnoreCase))
                return EmployeeMatchType.Normalized;

            // Check if amounts zero out (indicating likely same person)
            var amountsZeroOut = Math.Abs(Math.Abs(qbAmount) - Math.Abs(avionteAmount)) < 0.01m && 
                               Math.Abs(qbAmount + avionteAmount) < 0.01m;

            if (amountsZeroOut)
            {
                // If amounts zero out, it's likely the same person even with name differences
                return EmployeeMatchType.Fuzzy; // Could add a new type like "AmountBasedMatch"
            }

            return EmployeeMatchType.Fuzzy;
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
                if (headerRow == null)
                    throw new InvalidOperationException("No data found in the worksheet");
                    
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
