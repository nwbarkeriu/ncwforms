# JobCompare - Reconciliation Audit Dashboard

A comprehensive ASP.NET Core Blazor Server application for automating reconciliation audit processes between QuickBooks and Avionte systems.

## Features

### 📊 Core Functionality
- **Excel File Processing**: Upload and process QuickBooks, Avionte, and Deposit Detail Excel files
- **Automated Reconciliation**: Compare data between QB and Avionte systems
- **Variance Detection**: Identify and analyze discrepancies between systems
- **Interactive Dashboard**: Real-time processing with status updates

### 📋 Client Summary
- Complete client reconciliation with VLOOKUP-style results
- **Sortable Columns**: Click any column header to sort data
- **Advanced Filtering**: Type-to-filter functionality for all data columns
- Excel-style 13-column layout matching original audit format
- User input fields for billing notes and status tracking

### ⚠️ Variance Analysis
- Detailed variance tracking with sortable and filterable columns
- Color-coded variance highlighting (red for major, yellow for minor)
- Employee and job site breakdowns
- Variance type categorization

### 📊 Hierarchical Pivot Tables
- **4-Grid Layout**: Professional dashboard with responsive design
- **Excel-Style Grouping**: Expandable/collapsible client groups with +/- controls
- **Real-time Interaction**: Click to expand/collapse data groups
- **Multiple Pivot Views**:
  - QB Name/Item analysis
  - Avionte Bill To Name/Employee breakdown
  - QB Rep/Client relationships
  - Employee/Job Site combinations

### 💳 Payment Methods
- Deposit detail integration
- Payment method classification (Check, ACH, etc.)
- Optional deposit file processing

## Technical Stack

- **Framework**: ASP.NET Core 7.0 Blazor Server
- **Excel Processing**: ClosedXML library
- **UI Framework**: Bootstrap 5 with custom styling
- **Data Processing**: LINQ with hierarchical grouping
- **File Handling**: Multi-file upload with 10MB limit per file

## Getting Started

### Prerequisites
- .NET 7.0 SDK or later
- Visual Studio 2022 or VS Code

### Installation
1. Clone the repository
2. Navigate to the project directory
3. Restore dependencies: `dotnet restore`
4. Build the project: `dotnet build`
5. Run the application: `dotnet run`

### Usage
1. Navigate to `/recon` in your browser
2. Upload your QuickBooks Excel file
3. Upload your Avionte Excel file
4. Optionally upload a Deposit Detail file
5. Click "Process Reconciliation"
6. Explore the results in the interactive dashboard

## File Formats

### QuickBooks File Expected Columns
- Name, Item, Rep, Employee, Job Site, Amount, Date, etc.

### Avionte File Expected Columns
- Bill To Name, Name, Item Bill, Employee details, etc.

### Deposit Detail File (Optional)
- Client Name, Payment Method, Amount, Date, Check Number

## Features Highlights

### Sorting & Filtering
- **Client Summary**: All columns sortable, inline filters for text and numeric data
- **Variance Analysis**: Advanced filtering by client, employee, job site, amounts
- **Real-time Updates**: Instant filtering as you type

### Hierarchical Data
- **Excel-style Pivot Tables**: Group by client with expand/collapse functionality
- **Color-coded Controls**: Blue (+/-) for QB data, Green for Rep data, Cyan for Avionte
- **Responsive Design**: Works on desktop, tablet, and mobile devices

## Project Structure
```
├── Models/           # Data models and structures
├── Services/         # Business logic and Excel processing
├── Pages/           # Blazor pages and components
├── Shared/          # Shared components and layouts
└── wwwroot/         # Static assets
```

## Contributing
This is a specialized reconciliation tool for accounting workflows. For feature requests or bug reports, please create an issue.

## License
Private project - All rights reserved.
