# Portfolio Monitor - Guyton-Klinger Withdrawal Strategy

A Windows Forms (.NET) application designed to visualize portfolio performance against a 2-week moving average. This tool assists in executing the Guyton-Klinger withdrawal strategy by tracking when the portfolio's composite value falls below specific thresholds relative to its moving average.

## Features

### 1. Interactive Visualization
* **Dynamic Charting:** Plots "Composite Percent" (Lime Green) vs. "2-Week Moving Average" (Orange Red) over time.
* **Threshold Indicators:**
    * **90% Line (Purple Dashed):** Visual warning indicator.
    * **80% Line (Purple Solid):** Critical action indicator.
* **Dark Mode UI:** High-contrast dark theme with neon data lines for easy readability.
* **Smart Zooming:**
    * Left-click and drag to zoom into specific X or Y axes ranges.
    * **Auto-Intervals:** X-Axis automatically switches from Monthly intervals (default) to Weekly intervals when zoomed in.
* **Crosshair Cursor:** Cyan dashed crosshairs track mouse movement for precise value reading.

### 2. Data Filtering Dialog
* **Startup Configuration:** Launches a dialog box upon opening to select:
    * **Tax Type:** (e.g., Tax Free, Tax Deferred, Taxable).
    * **Date Range:** Defaults to the last 2 years.
* **State Retention:** Remembering settings when re-opening the dialog during a session.

### 3. Strategy Context
* **Withdrawal Strategy Display:** Automatically fetches and displays the current strategy name (via stored procedure `usp_GetWithdrawalStrategy`).
* **Latest Values:** Displays the most recent Composite and Moving Average percentages directly on the chart footer.

### 4. Usability
* **Double-Click:** Double-clicking the chart re-opens the Filter Dialog to change parameters without restarting the app.
* **Right-Click:** Instantly resets the zoom level.
* **Reset Button:** A dedicated button to reset the view if the mouse controls are inconvenient.

---

## Technical Requirements

* **Framework:** .NET Framework 4.7.2+ or .NET Core 3.1+ / .NET 6+ (Windows Forms)
* **Database:** Microsoft SQL Server (LocalDB or Standard)
* **NuGet Packages:**
    * `Microsoft.Data.SqlClient`
    * `System.Drawing.Common` (if using .NET Core/6+)

### Database Schema
The application relies on the `Guyton-Klinger-Withdrawals` database with the following objects:

1.  **View:** `[dbo].[vw_CompositePortfolio_MovingAverage]`
    * Columns: `TaxType`, `Closing` (Date), `CompositePercent` (0-100), `MovingAverage_2Week_Percent` (0-100).
2.  **Stored Procedure:** `[dbo].[usp_GetWithdrawalStrategy]`
    * Returns: A single string (Scalar) representing the current strategy name.

    * All the required database schema objects can be created using the DDL kept in this GitHub repository: [Guyton-Klinger-Withdrawals](https://github.com/CaveArnold/Guyton-Klinger-Withdrawals)

---

## Installation & Setup

1.  **Clone/Download:** Download the source code to your local machine.
2.  **Configure Connection:**
    Open `Program.cs` and update the `connectionString` variable to point to your SQL Server instance:
    ```csharp
    private string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Guyton-Klinger-Withdrawals;Integrated Security=True;TrustServerCertificate=True;";
    ```
3.  **Build:** Open the solution in Visual Studio and build the project.
4.  **Run:** Press `F5` or run the executable.

---

## Usage Guide

1.  **Launch:** On startup, select your desired **Tax Type** and **Date Range** in the popup dialog.
2.  **Analyze:**
    * Observe where the Green line (Price) crosses the Red line (Average).
    * Watch for dips below the Purple threshold lines (90% / 80%).
    * Read the exact values at the bottom left of the screen.
3.  **Investigate:** Drag a box around a specific dip to see weekly data points.
4.  **Reset/Change:** Right-click to reset the view, or double-click to load a different Tax Type.