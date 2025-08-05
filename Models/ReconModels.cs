namespace JobCompareApp.Models
{
    public class PivotResult
    {
        public string Key { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Quantity { get; set; }
        public int Count { get; set; }
    }

    public class HierarchicalPivotGroup
    {
        public string GroupName { get; set; } = string.Empty; // e.g., "AME Inc."
        public decimal TotalAmount { get; set; }
        public int TotalCount { get; set; }
        public List<PivotResult> Children { get; set; } = new(); // Individual items within the group
        public bool IsExpanded { get; set; } = false; // For UI state management
    }

    public class ClientSummary
    {
        public string ClientName { get; set; } = string.Empty; // This maps to "Quickbooks" column
        public decimal QBBalance { get; set; } // "QB Balance" 
        public string AviClientName { get; set; } = string.Empty; // "Avi" column (might be different from QB name)
        public decimal AviBalance { get; set; } // "Avi Balance"
        public decimal Variance => QBBalance - AviBalance; // "Variance"
        public string PaymentType { get; set; } = "Research pymt method"; // "Pymt type" 
        public string Team { get; set; } = string.Empty; // "Team"
        public string PRRep { get; set; } = string.Empty; // "PR Rep"
        public string SendType { get; set; } = string.Empty; // "send type"
        public string AccountType { get; set; } = string.Empty; // "Account type"
        public string BillingNotes { get; set; } = string.Empty; // "Billing Notes (USER INPUT MEMO FIELD)"
        public string Notes { get; set; } = string.Empty; // "Notes"
        public string Status { get; set; } = string.Empty; // "Status"
        
        // Keep some original properties for internal use
        public int QBInvoiceCount { get; set; }
        public int AvionteRecordCount { get; set; }
        public List<string> Employees { get; set; } = new();
        public List<string> JobSites { get; set; } = new();
    }

    public class VarianceEntry
    {
        public string ClientName { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string JobSite { get; set; } = string.Empty;
        public decimal QBAmount { get; set; }
        public decimal AvionteAmount { get; set; }
        public decimal Variance => QBAmount - AvionteAmount;
        public string VarianceType { get; set; } = string.Empty; // "Hours", "Rate", "Total", "Name Mismatch"
        public string Notes { get; set; } = string.Empty;
    }

    public class DepositDetailEntry
    {
        public string ClientName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty; // "Check" or "ACH"
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string CheckNumber { get; set; } = string.Empty;
    }

    public class ReconResults
    {
        // Original flat pivot tables (kept for backward compatibility)
        public List<PivotResult> QBNameItemPivot { get; set; } = new();
        public List<PivotResult> QBRepClientPivot { get; set; } = new();
        public List<PivotResult> QBInvoicePivot { get; set; } = new();
        public List<PivotResult> QBEmployeeJobSitePivot { get; set; } = new();
        public List<PivotResult> AvionteBillToNamePivot { get; set; } = new();
        
        // New hierarchical pivot tables (Excel-style)
        public List<HierarchicalPivotGroup> QBNameItemHierarchical { get; set; } = new();
        public List<HierarchicalPivotGroup> QBRepClientHierarchical { get; set; } = new();
        public List<HierarchicalPivotGroup> AvionteBillToNameHierarchical { get; set; } = new();
        
        public List<ClientSummary> ClientSummaries { get; set; } = new();
        public List<VarianceEntry> Variances { get; set; } = new();
        public List<DepositDetailEntry> DepositDetails { get; set; } = new();
        public List<QuickBooksEntry> FilteredRecords { get; set; } = new();
        public List<AvionteEntry> ExcludedRecords { get; set; } = new();
    }
}
