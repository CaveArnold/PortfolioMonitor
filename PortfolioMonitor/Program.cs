/* * Developer: Cave Arnold
 * AI Assistant: Gemini
 * Date: January 21, 2026
 * Version: 1.0.0
 * 
 * ==============================================================================================
 * PORTFOLIO MONITOR - GUYTON-KLINGER WITHDRAWAL STRATEGY
 * ==============================================================================================
 * * A Windows Forms (.NET) application designed to visualize portfolio performance against a 
 * 2-week moving average. This tool assists in executing the Guyton-Klinger withdrawal strategy 
 * by tracking when the portfolio's composite value falls below specific thresholds relative 
 * to its moving average.
 * 
 * * ----------------------------------------------------------------------------------------------
 * FEATURES
 * ----------------------------------------------------------------------------------------------
 * * 1. INTERACTIVE VISUALIZATION
 * - Dynamic Charting: Plots "Composite Percent" (Lime Green) vs. "2-Week Moving Average" 
 * (Orange Red) over time.
 * - Threshold Indicators:
 * * 90% Line (Purple Dashed): Visual warning indicator.
 * * 80% Line (Medium Purple Solid): Critical action indicator.
 * - Dark Mode UI: High-contrast dark theme with neon data lines for easy readability.
 * - Smart Zooming:
 * * Left-click and drag to zoom into specific X or Y axes ranges.
 * * Auto-Intervals: X-Axis automatically switches from Monthly intervals (default) 
 * to Weekly intervals when zoomed in.
 * - Crosshair Cursor: Cyan dashed crosshairs track mouse movement for precise reading.
 * * 2. DATA FILTERING DIALOG
 * - Startup Configuration: Launches a dialog box upon opening to select:
 * * Tax Type: (e.g., Tax Free, Tax Deferred, Taxable).
 * * Date Range: Defaults to the last 2 years.
 * - State Retention: Remembers settings when re-opening the dialog during a session.
 * * 3. STRATEGY CONTEXT
 * - Withdrawal Strategy Display: Automatically fetches and displays the current strategy 
 * name (via stored procedure `usp_GetWithdrawalStrategy`).
 * - Latest Values: Displays the most recent Composite and Moving Average percentages 
 * directly on the chart footer.
 * * 4. USABILITY
 * - Double-Click: Re-opens the Filter Dialog to change parameters without restarting.
 * - Right-Click: Instantly resets the zoom level.
 * - Reset Button: Dedicated button to reset the view.
 * 
 * * ----------------------------------------------------------------------------------------------
 * TECHNICAL REQUIREMENTS
 * ----------------------------------------------------------------------------------------------
 * * - Framework: .NET Framework 4.7.2+ or .NET Core 3.1+ / .NET 6+ (Windows Forms)
 * - Database: Microsoft SQL Server (LocalDB or Standard)
 * - NuGet Packages:
 * * Microsoft.Data.SqlClient
 * * System.Drawing.Common (if using .NET Core/6+)
 * * Database Schema Requirements:
 * 1. View: [dbo].[vw_CompositePortfolio_MovingAverage]
 * - Columns: TaxType, Closing (Date), CompositePercent, MovingAverage_2Week_Percent
 * 2. Stored Procedure: [dbo].[usp_GetWithdrawalStrategy]
 * - Returns: Scalar string representing the strategy name.
 * 
 * * ----------------------------------------------------------------------------------------------
 * USAGE GUIDE
 * ----------------------------------------------------------------------------------------------
 * * 1. Launch: Select your desired Tax Type and Date Range in the popup dialog.
 * 2. Analyze: Observe where the Green line (Price) crosses the Red line (Average).
 * Watch for dips below the Purple threshold lines (90% / 80%).
 * 3. Investigate: Drag a box around a specific dip to see weekly data points.
 * 4. Reset/Change: Right-click to reset, double-click chart to change parameters.
 * * ==============================================================================================
 */

using System;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace PortfolioMonitor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (FilterDialog dlg = new FilterDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new PortfolioForm(dlg.SelectedTaxType, dlg.StartDate, dlg.EndDate));
                }
            }
        }
    }

    // --- DIALOG CLASS ---
    public class FilterDialog : Form
    {
        public string SelectedTaxType { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }

        private ComboBox cboTaxType;
        private DateTimePicker dtpStart;
        private DateTimePicker dtpEnd;
        private Button btnLoad;

        private string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Guyton-Klinger-Withdrawals;Integrated Security=True;TrustServerCertificate=True;";

        public FilterDialog(string currentTaxType = null, DateTime? currentStart = null, DateTime? currentEnd = null)
        {
            this.Text = "Select Chart Parameters";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            Label lblTax = new Label() { Text = "Tax Type:", Location = new Point(20, 20), AutoSize = true };
            cboTaxType = new ComboBox() { Location = new Point(120, 17), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblStart = new Label() { Text = "Start Date:", Location = new Point(20, 60), AutoSize = true };
            dtpStart = new DateTimePicker() { Location = new Point(120, 57), Width = 200, Format = DateTimePickerFormat.Short };

            Label lblEnd = new Label() { Text = "End Date:", Location = new Point(20, 100), AutoSize = true };
            dtpEnd = new DateTimePicker() { Location = new Point(120, 97), Width = 200, Format = DateTimePickerFormat.Short };

            btnLoad = new Button() { Text = "Load Chart", Location = new Point(120, 150), Width = 100, Height = 40, DialogResult = DialogResult.OK };
            btnLoad.Click += (s, e) => SaveSelections();

            this.Controls.AddRange(new Control[] { lblTax, cboTaxType, lblStart, dtpStart, lblEnd, dtpEnd, btnLoad });

            this.AcceptButton = btnLoad;

            LoadTaxTypes(currentTaxType);

            dtpStart.Value = currentStart ?? DateTime.Now.AddYears(-2);
            dtpEnd.Value = currentEnd ?? DateTime.Now;
        }

        private void LoadTaxTypes(string selectedToSelect)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "SELECT DISTINCT TaxType FROM [dbo].[vw_CompositePortfolio_MovingAverage] ORDER BY TaxType";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cboTaxType.Items.Add(reader["TaxType"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading Tax Types: " + ex.Message);
            }

            if (!string.IsNullOrEmpty(selectedToSelect) && cboTaxType.Items.Contains(selectedToSelect))
            {
                cboTaxType.SelectedItem = selectedToSelect;
            }
            else if (cboTaxType.Items.Contains("Tax Free"))
            {
                cboTaxType.SelectedItem = "Tax Free";
            }
            else if (cboTaxType.Items.Count > 0)
            {
                cboTaxType.SelectedIndex = 0;
            }
        }

        private void SaveSelections()
        {
            SelectedTaxType = cboTaxType.SelectedItem?.ToString() ?? "";
            StartDate = dtpStart.Value;
            EndDate = dtpEnd.Value;
        }
    }

    // --- MAIN CHART FORM ---
    public class PortfolioForm : Form
    {
        private Chart chart1;
        private Button btnResetZoom;

        private string connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=Guyton-Klinger-Withdrawals;Integrated Security=True;TrustServerCertificate=True;";

        private string _filterTaxType;
        private DateTime _filterStartDate;
        private DateTime _filterEndDate;

        private readonly Color clrBackground = Color.FromArgb(30, 30, 30);
        private readonly Color clrChartArea = Color.Black;
        private readonly Color clrText = Color.WhiteSmoke;
        private readonly Color clrGrid = Color.FromArgb(50, 50, 50);
        private readonly Color clrLine1 = Color.Lime;
        private readonly Color clrLine2 = Color.OrangeRed;

        public PortfolioForm(string taxType, DateTime start, DateTime end)
        {
            _filterTaxType = taxType;
            _filterStartDate = start;
            _filterEndDate = end;

            InitializeComponent();
            LoadPortfolioData();
        }

        private void InitializeComponent()
        {
            this.Text = "Portfolio Monitor - Guyton-Klinger";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = clrBackground;
            this.ForeColor = clrText;

            btnResetZoom = new Button();
            btnResetZoom.Text = "Reset Zoom";
            btnResetZoom.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - 140, 12);
            btnResetZoom.Size = new Size(100, 30);
            btnResetZoom.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnResetZoom.FlatStyle = FlatStyle.Flat;
            btnResetZoom.ForeColor = clrText;
            btnResetZoom.BackColor = Color.FromArgb(60, 60, 60);
            btnResetZoom.Click += BtnResetZoom_Click;
            this.Controls.Add(btnResetZoom);

            chart1 = new Chart();
            chart1.Dock = DockStyle.Fill;
            chart1.BackColor = clrBackground;

            ChartArea chartArea = new ChartArea("MainArea");
            chartArea.BackColor = clrChartArea;

            // --- CUSTOM POSITIONING FOR CHART AREA ---
            // Allows us to reserve specific space at the bottom for side-by-side titles/legend
            chartArea.Position.Auto = false;
            chartArea.Position.X = 0;
            chartArea.Position.Y = 10; // Leave space for Top Title
            chartArea.Position.Width = 98; // Use full width
            chartArea.Position.Height = 78; // Leave space at bottom (12% reserved)

            // -- X-AXIS --
            chartArea.AxisX.LabelStyle.Format = "MM/dd/yyyy";
            chartArea.AxisX.LabelStyle.ForeColor = clrText;
            chartArea.AxisX.LineColor = Color.Gray;
            chartArea.AxisX.MajorGrid.LineColor = clrGrid;

            // UPDATED: Automatic Intervals
            chartArea.AxisX.Interval = 0; // Auto
            chartArea.AxisX.IntervalType = DateTimeIntervalType.Auto;
            chartArea.AxisX.LabelStyle.Interval = 0; // Auto Labels
            chartArea.AxisX.LabelStyle.IntervalType = DateTimeIntervalType.Auto;

            // -- Y-AXIS --
            chartArea.AxisY.LabelStyle.Format = "P0";
            chartArea.AxisY.LabelStyle.ForeColor = clrText;
            chartArea.AxisY.LineColor = Color.Gray;
            chartArea.AxisY.MajorGrid.LineColor = clrGrid;
            chartArea.AxisY.Interval = 0.1;

            // -- THRESHOLDS --
            StripLine limit90 = new StripLine();
            limit90.Interval = 0;
            limit90.IntervalOffset = 0.9;
            limit90.StripWidth = 0.0;
            limit90.BorderColor = Color.MediumPurple;
            limit90.BorderWidth = 3;
            limit90.BorderDashStyle = ChartDashStyle.Dash;
            chartArea.AxisY.StripLines.Add(limit90);

            StripLine limit80 = new StripLine();
            limit80.Interval = 0;
            limit80.IntervalOffset = 0.8;
            limit80.StripWidth = 0.0;
            limit80.BorderColor = Color.MediumPurple;
            limit80.BorderWidth = 3;
            limit80.BorderDashStyle = ChartDashStyle.Solid;
            chartArea.AxisY.StripLines.Add(limit80);

            // -- ZOOMING --
            chartArea.CursorX.IsUserEnabled = true;
            chartArea.CursorX.IsUserSelectionEnabled = true;
            chartArea.CursorX.Interval = 0;
            chartArea.AxisX.ScaleView.Zoomable = true;
            chartArea.AxisX.ScrollBar.IsPositionedInside = false;

            chartArea.CursorY.IsUserEnabled = true;
            chartArea.CursorY.IsUserSelectionEnabled = true;
            chartArea.CursorY.Interval = 0;
            chartArea.AxisY.ScaleView.Zoomable = true;
            chartArea.AxisY.ScrollBar.IsPositionedInside = false;

            chart1.ChartAreas.Add(chartArea);

            // -- LEGEND --
            Legend legend = new Legend("MainLegend");
            legend.BackColor = Color.FromArgb(50, 50, 50);
            legend.ForeColor = clrText;
            legend.BorderColor = Color.White;
            legend.BorderWidth = 1;
            legend.ShadowOffset = 2;
            legend.Font = new Font("Arial", 18, FontStyle.Bold); // 18pt

            // --- CUSTOM POSITIONING FOR LEGEND ---
            // Place it manually in the bottom-right corner
            legend.Position.Auto = false;
            legend.Position.X = 75;  // Start at 75% width
            legend.Position.Y = 88;  // Bottom area
            legend.Position.Width = 25;
            legend.Position.Height = 12;

            chart1.Legends.Add(legend);

            // -- EVENTS --
            chart1.MouseMove += Chart1_MouseMove;
            chart1.MouseClick += Chart1_MouseClick;
            chart1.DoubleClick += Chart1_DoubleClick;

            this.Controls.Add(chart1);
            btnResetZoom.BringToFront();
        }

        private void Chart1_DoubleClick(object sender, EventArgs e)
        {
            using (FilterDialog dlg = new FilterDialog(_filterTaxType, _filterStartDate, _filterEndDate))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    _filterTaxType = dlg.SelectedTaxType;
                    _filterStartDate = dlg.StartDate;
                    _filterEndDate = dlg.EndDate;

                    LoadPortfolioData();
                    PerformZoomReset();
                }
            }
        }

        private void LoadPortfolioData()
        {
            DataTable dt = new DataTable();
            string strategyName = "Unknown";

            string queryData = @"
                SELECT TaxType, Closing, 
                       (CompositePercent / 100.0) AS CompositePercent, 
                       (MovingAverage_2Week_Percent / 100.0) AS MovingAverage_2Week_Percent 
                FROM [dbo].[vw_CompositePortfolio_MovingAverage] 
                WHERE TaxType = @TaxType 
                  AND Closing >= @StartDate 
                  AND Closing <= @EndDate
                ORDER BY Closing ASC";

            string querySP = "EXEC [dbo].[usp_GetWithdrawalStrategy]";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlCommand cmdSP = new SqlCommand(querySP, conn))
                    {
                        var result = cmdSP.ExecuteScalar();
                        if (result != null) strategyName = result.ToString();
                    }

                    using (SqlCommand cmdData = new SqlCommand(queryData, conn))
                    {
                        cmdData.Parameters.AddWithValue("@TaxType", _filterTaxType);
                        cmdData.Parameters.AddWithValue("@StartDate", _filterStartDate);
                        cmdData.Parameters.AddWithValue("@EndDate", _filterEndDate);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(cmdData))
                        {
                            adapter.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to database:\n" + ex.Message, "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var chartArea = chart1.ChartAreas[0];
            chart1.Titles.Clear();

            // 1. Top Title (Tax Type)
            Title titleTop = new Title(_filterTaxType);
            titleTop.Font = new Font("Comic Sans MS", 18, FontStyle.Bold);
            titleTop.ForeColor = Color.Lime;
            // Manual positioning to ensure it stays at top
            titleTop.Position.Auto = false;
            titleTop.Position.X = 0;
            titleTop.Position.Y = 0;
            titleTop.Position.Width = 100;
            titleTop.Position.Height = 10;
            chart1.Titles.Add(titleTop);


            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("No data found.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get Latest Values
            DataRow lastRow = dt.Rows[dt.Rows.Count - 1];
            double latestComposite = lastRow["CompositePercent"] != DBNull.Value
                ? Convert.ToDouble(lastRow["CompositePercent"]) : 0;
            double latestMA = lastRow["MovingAverage_2Week_Percent"] != DBNull.Value
                ? Convert.ToDouble(lastRow["MovingAverage_2Week_Percent"]) : 0;

            // 2. Combine Strategy + Latest Values into one Bottom-Left Title
            // This ensures they stay together.
            string bottomText = $"Withdrawal Strategy : {strategyName}\nLatest Composite: {latestComposite:P2}   Latest Moving Average: {latestMA:P2}";

            Title titleBottom = new Title(bottomText);
            titleBottom.Font = new Font("Comic Sans MS", 18, FontStyle.Bold); // 18pt
            titleBottom.ForeColor = Color.DeepSkyBlue;
            titleBottom.Alignment = ContentAlignment.TopLeft; // Align Left

            // --- CUSTOM POSITIONING FOR BOTTOM TEXT ---
            // Place it in the Bottom Left, next to the Legend
            titleBottom.Position.Auto = false;
            titleBottom.Position.X = 2;   // Start near left edge
            titleBottom.Position.Y = 88;  // Bottom area (Same Y as Legend)
            titleBottom.Position.Width = 73; // Take up 73% width
            titleBottom.Position.Height = 12;

            chart1.Titles.Add(titleBottom);


            // --- Y-AXIS SCALING ---
            double minVal = double.MaxValue;
            double maxVal = double.MinValue;

            foreach (DataRow row in dt.Rows)
            {
                if (row["CompositePercent"] != DBNull.Value)
                {
                    double val = Convert.ToDouble(row["CompositePercent"]);
                    if (val < minVal) minVal = val;
                    if (val > maxVal) maxVal = val;
                }
                if (row["MovingAverage_2Week_Percent"] != DBNull.Value)
                {
                    double val = Convert.ToDouble(row["MovingAverage_2Week_Percent"]);
                    if (val < minVal) minVal = val;
                    if (val > maxVal) maxVal = val;
                }
            }

            if (minVal == double.MaxValue) minVal = 0;
            if (maxVal == double.MinValue) maxVal = 1.0;

            double axisMin = Math.Floor(minVal * 10) / 10.0;
            double axisMax = Math.Ceiling(maxVal * 10) / 10.0;
            if (axisMax <= axisMin) axisMax = axisMin + 0.1;

            chartArea.AxisY.Minimum = axisMin;
            chartArea.AxisY.Maximum = axisMax;

            // --- SERIES ---
            chart1.Series.Clear();

            Series priceSeries = new Series("Composite %");
            priceSeries.ChartType = SeriesChartType.Line;
            priceSeries.Color = clrLine1;
            priceSeries.BorderWidth = 2;
            priceSeries.XValueType = ChartValueType.DateTime;
            priceSeries.ToolTip = "Date: #VALX{d}\nValue: #VALY{P2}";
            priceSeries.Points.DataBind(dt.DefaultView, "Closing", "CompositePercent", null);
            chart1.Series.Add(priceSeries);

            Series maSeries = new Series("2-Week Moving Average %");
            maSeries.ChartType = SeriesChartType.Line;
            maSeries.Color = clrLine2;
            maSeries.BorderWidth = 2;
            maSeries.XValueType = ChartValueType.DateTime;
            maSeries.ToolTip = "Date: #VALX{d}\nAvg: #VALY{P2}";
            maSeries.Points.DataBind(dt.DefaultView, "Closing", "MovingAverage_2Week_Percent", null);
            chart1.Series.Add(maSeries);
        }

        private void BtnResetZoom_Click(object sender, EventArgs e)
        {
            PerformZoomReset();
        }

        private void Chart1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                PerformZoomReset();
            }
        }

        private void PerformZoomReset()
        {
            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset();
        }

        private void Chart1_MouseMove(object sender, MouseEventArgs e)
        {
            var chartArea = chart1.ChartAreas[0];
            try
            {
                chartArea.CursorX.SetCursorPixelPosition(new Point(e.X, e.Y), true);
                chartArea.CursorY.SetCursorPixelPosition(new Point(e.X, e.Y), true);
            }
            catch { }
        }
    }
}