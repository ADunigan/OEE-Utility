using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Office.Tools.Ribbon;
using Num = MathNet.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.Drawing.Imaging;

namespace OEE_ExcelAddIn_2010
{
    public partial class OEE_Ribbon
    {
        //Establishes the worksheet variable as global dynamic
        dynamic ws;
        Process process;

        //Unit Op and Buffer Lists
        List<Unit_Op> unit_ops = new List<Unit_Op>();
        List<Buffer> buffers = new List<Buffer>();

        //Simulation parameters
        int Sim_Time_Minutes;
        int Sim_Time_Seconds;
        int Line_Rate;
        int line_speed = int.MaxValue;
        int num_runs;

        //Data evaluation
        double OEE100_Count;
        double prod_count;
        List<int[]> buffer_fill;
        List<int[]> defect_counts;
        List<double[]> oee;
        double[] run_oee;
        double max;
        double min;
        double stddev;
        double average;
        double high_quantile;

        //Simulation Statistics
        double msec_start;
        double simtime;

        //Various
        System.Windows.Media.Color haskell_blue = new System.Windows.Media.Color();        

        private void OEE_Ribbon_Load(object sender, RibbonUIEventArgs e)
        {
            //Subscriptions
            this.btn_Simulate.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btn_click_Simulate);

            //Setup
            haskell_blue.A = 255;
            haskell_blue.R = 53;
            haskell_blue.G = 120;
            haskell_blue.B = 168;
        }

        private void btn_click_Simulate(object sender, RibbonControlEventArgs e)
        {
            msec_start = DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds;
            //Get the active worksheet from Globals.
            ws = Globals.ThisAddIn.Application.ActiveSheet;

            //Set simulation run parameters
            num_runs = (int)ws.Cells[8, 22].Value;
            Sim_Time_Minutes = (int)ws.Cells[7, 22].Value;
            Sim_Time_Seconds = Sim_Time_Minutes * 60;
            Line_Rate = (int)ws.Cells[9, 12].Value;
            OEE100_Count = Line_Rate * Sim_Time_Minutes;
            prod_count = 0;

            //Initialize an empty manufacturing process and empty data
            process = new Process();
            buffer_fill = new List<int[]>();
            defect_counts = new List<int[]>();
            oee = new List<double[]>();
            run_oee = new double[num_runs];

            //The first row where the unit operations list begins
            //The index column is checked for a value before a unit op is added
            //The first cell in the index column without a value will break the loop
            int row = 15;
            int column = 1;
            int? index = (int?)ws.Cells[row, column].Value;

            while (index.HasValue)
            {
                //If there is a value > 0 in the buffer column then create buffer in process
                if (((int?)ws.Cells[row, 17].Value) > 0)
                {
                    Buffer newBuffer = new Buffer();
                    PopulateBuffer(newBuffer, row);
                    process.Activities.Add(newBuffer);
                    buffer_fill.Add(new int[Sim_Time_Minutes]);
                }
                else
                {
                    Unit_Op newOp = new Unit_Op();
                    PopulateUnitOp(newOp, row);
                    process.Activities.Add(newOp);
                    oee.Add(new double[num_runs]);
                    defect_counts.Add(new int[num_runs]);
                }

                //Iterates current row and redefines index
                row++;
                index = (int?)ws.Cells[row, column].Value;
            }                       

            Simulation();
        }
        
        private void Simulation()
        {
            //Initialize empty data arrays
            for(int run = 0; run < num_runs; run++)
            {
                StartActivities();
                prod_count = 0;
                for (int Time_Step = 0; Time_Step < Sim_Time_Seconds; Time_Step++)
                {
                    process.Step(Time_Step);

                    //Evaluate product count to find total number of products produced during run
                    if (((Unit_Op)process.Activities[process.Activities.Count - 1]).Running)
                    {
                        prod_count = prod_count + ((Unit_Op)process.Activities[process.Activities.Count - 1]).ActualSpeed / (double)60.0;
                    }

                    //Data collection function
                    //Live data is data that is iterative over the simulation process and must be pulled as it happens
                    //Live data is extracted at every minute time step
                    if (Time_Step % 60 == 0)
                    {
                        LiveDataCollection(Time_Step/60);
                    }
                }
                //Final data is data that is inherently captured as part of the process and does not need iterative calculation
                FinalDataCollection(run);
            }
            OverallDataAnalysis();
            GenerateReport();
        } 
        
        private void LiveDataCollection(int step)
        {
            //Collects data for each unit op and buffer individually
            int i = 0;  //Used to track Unit_Ops
            int j = 0;  //Used to track Buffers
            foreach (IOperations iop in process.Activities)
            {
                if(iop.IsUnitOp())
                {
                    //Collects running state
                    Unit_Op op = (Unit_Op)iop;
                    //machine_state[i][step] = Convert.ToInt32(op.Running);
                    i++;
                }
                else if(iop.IsBuffer())
                {
                    //Collects buffer fill count
                    Buffer buff = (Buffer)iop;
                    buffer_fill[j][step] = (int)buff.Buffer_Count;
                    j++;
                }
            }
        }

        private void FinalDataCollection(int run)
        {
            int i = 0;  //Used to track Unit_Ops
            int j = 0;  //Used to track Buffers
            int total_defects = 0;
            foreach (IOperations iop in process.Activities)
            {
                if (iop.IsUnitOp())
                {
                    Unit_Op op = (Unit_Op)iop;
                    oee[i][run] = (double)op.TotalUpTime / (double)(op.TotalUpTime + op.TotalDownTime);
                    defect_counts[i][run] = op.Defect_Count;
                    total_defects = total_defects + op.Defect_Count;
                    i++;
                }
                else if(iop.IsBuffer())
                {
                    j++;
                }
            }
            run_oee[run] = (prod_count - total_defects) / OEE100_Count;
        }

        //Performs statistical analysis on OEE performance
        private void OverallDataAnalysis()
        {
            simtime = Math.Round((DateTime.Now.Subtract(DateTime.MinValue.AddYears(1969)).TotalMilliseconds - msec_start)/1000, 5);

            max = Math.Round(run_oee.Max() * 100, 2);
            min = Math.Round(run_oee.Min() * 100, 2);
            stddev = Math.Round(run_oee.StandardDeviation() * 100, 2);            
            average = Math.Round(run_oee.Average() * 100, 2);
            high_quantile = Math.Round(1.65 * stddev + average, 2);
        }

        private void GenerateReport()
        {
            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            if ((bool)printDialog.ShowDialog().GetValueOrDefault())
            {
                FlowDocument flowDocument = new FlowDocument();
                Paragraph paragraph;
                Table table = new Table();
                TableRowGroup rg = new TableRowGroup();
                TableRow row = new TableRow();

                table.CellSpacing = 0;
                row.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                row.FontSize = 10;
                flowDocument.ColumnWidth = printDialog.PrintableAreaWidth;
                flowDocument.PageHeight = printDialog.PrintableAreaHeight;

                int columnCount = 9;

                //Report Header and Simulation Parameters and Settings
                //Title and Haskell Logo
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);                
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 36;
                paragraph.Inlines.Add(new Run("Simulation Report"));                

                BitmapImage bm = new BitmapImage(new Uri(@"C:\Users\acduniga\source\repos\OEE_Utility\OEE_ExcelAddIn_2010\Resources\Images\Haskell_Logo.png", UriKind.Absolute));
                System.Windows.Controls.Image image = new System.Windows.Controls.Image();
                image.Height = 50;
                image.Width = 300;
                image.Source = bm;
                Figure figure = new Figure();
                figure.Height = new FigureLength(100);
                figure.Width = new FigureLength(310);
                figure.HorizontalAnchor = FigureHorizontalAnchor.PageRight;
                figure.VerticalAnchor = FigureVerticalAnchor.PageTop;
                figure.Blocks.Add(new BlockUIContainer(image));

                paragraph.Inlines.Add(figure);
                flowDocument.Blocks.Add(paragraph);

                //Simulation Number of Runs
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("Number of Runs: {0}", num_runs));
                flowDocument.Blocks.Add(paragraph);

                //Simulation Minutes per Run
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("Minutes per Run: {0}", Sim_Time_Minutes));
                flowDocument.Blocks.Add(paragraph);

                //Simulation Start Time
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("Simulation Started at: {0}", DateTime.Now.ToString()));
                flowDocument.Blocks.Add(paragraph);

                //Simulation Computation Time
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("Simulation Computation Time: {0} seconds", simtime));
                flowDocument.Blocks.Add(paragraph);

                //
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.Inlines.Add(new Run(" "));
                flowDocument.Blocks.Add(paragraph);

                //Overall run OEE Statistics header
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 24;
                paragraph.Inlines.Add("OEE Performance Statistics");
                paragraph.TextAlignment = TextAlignment.Center;
                flowDocument.Blocks.Add(paragraph);

                //Average OEE
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("Average OEE: {0}%", average));
                paragraph.TextAlignment = TextAlignment.Center;
                flowDocument.Blocks.Add(paragraph);

                //Maximum OEE
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("Maximum OEE: {0}%", max));
                paragraph.TextAlignment = TextAlignment.Center;
                flowDocument.Blocks.Add(paragraph);

                //Minimum OEE
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("Minimum OEE: {0}%", min));
                paragraph.TextAlignment = TextAlignment.Center;
                flowDocument.Blocks.Add(paragraph);

                //Standard Deviation of OEEs
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("OEE Standard Deviation: {0}%", stddev));
                paragraph.TextAlignment = TextAlignment.Center;
                flowDocument.Blocks.Add(paragraph);

                //95% quantile of OEE
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                paragraph.FontSize = 14;
                paragraph.Inlines.Add(String.Format("95% OEE Quantile: {0}%", high_quantile));
                paragraph.TextAlignment = TextAlignment.Center;
                flowDocument.Blocks.Add(paragraph);

                //
                paragraph = new Paragraph();
                paragraph.Margin = new Thickness(0);
                paragraph.Inlines.Add(new Run(" "));
                flowDocument.Blocks.Add(paragraph);

                //Table of unit op data
                for (int i = 0; i < columnCount; i++)
                {
                    table.Columns.Add(new TableColumn());

                    switch (i)
                    {
                        case 0:
                            table.Columns[i].Name = "Unit_Op";
                            table.Columns[i].Width = new GridLength(140);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("Unit Op"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Left;
                            break;

                        case 1:
                            table.Columns[i].Name = "Design_Speed";
                            table.Columns[i].Width = new GridLength(70);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("Design Speed"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Right;
                            break;

                        case 2:
                            table.Columns[i].Name = "Actual_Speed";
                            table.Columns[i].Width = new GridLength(70);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("Actual Speed"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Right;
                            break;

                        case 3:
                            table.Columns[i].Name = "MTTR";
                            table.Columns[i].Width = new GridLength(40);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("MTTR"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Right;
                            break;

                        case 4:
                            table.Columns[i].Name = "MTBF";
                            table.Columns[i].Width = new GridLength(40);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("MTBF"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Right;
                            break;

                        case 5:
                            table.Columns[i].Name = "Availability_Percentage";
                            table.Columns[i].Width = new GridLength(70);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("Availability (%)"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Right;
                            break;

                        case 6:
                            table.Columns[i].Name = "Quality_Loss_Percentage";
                            table.Columns[i].Width = new GridLength(75);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("Quality Loss (%)"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Right;
                            break;

                        case 7:
                            table.Columns[i].Name = "Buffer";
                            table.Columns[i].Width = new GridLength(35);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("Buffer"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Right;
                            break;

                        case 8:
                            table.Columns[i].Name = "Average_OEE";
                            table.Columns[i].Width = new GridLength(60);
                            row.Cells.Add(new TableCell(new Paragraph(new Run("Average OEE"))));
                            row.Cells[i].BorderThickness = new Thickness(0, 0, 0, 0.5);
                            row.Cells[i].BorderBrush = new SolidColorBrush(Colors.DarkGray);
                            row.Cells[i].TextAlignment = TextAlignment.Right;
                            break;
                    }
                }
                rg.Rows.Add(row);

                int k = 0;  //Used to track Unit_Ops
                bool alternator = false;
                foreach (IOperations iop in process.Activities)
                {
                    row = new TableRow();
                    row.FontFamily = new System.Windows.Media.FontFamily("Calibri");
                    row.FontSize = 10;

                    if (iop.IsUnitOp())
                    {
                        
                        Unit_Op op = (Unit_Op)iop;
                        string temp_availability = Math.Round(((double)op.MTBF / (double)(op.MTTR + op.MTBF)) * 100, 2).ToString();
                        string temp_qualityloss = (op.QualityLoss * 100).ToString();
                        string temp_averageoee = Math.Round((oee[k].Average()) * 100, 2).ToString();
                        string[] precise_strings = UnifyStringPrecision(temp_availability, temp_qualityloss, temp_averageoee);

                        row.Cells.Add(new TableCell(new Paragraph(new Run(op.Name))));
                        row.Cells[0].TextAlignment = TextAlignment.Left;
                        row.Cells.Add(new TableCell(new Paragraph(new Run(op.DesignSpeed.ToString()))));
                        row.Cells[1].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run((op.DesignSpeed - op.SpeedLoss).ToString()))));
                        row.Cells[2].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run((op.MTTR/60).ToString()))));
                        row.Cells[3].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run((op.MTBF/60).ToString()))));
                        row.Cells[4].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run(precise_strings[0]))));
                        row.Cells[5].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run(precise_strings[1]))));
                        row.Cells[6].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run("-"))));
                        row.Cells[7].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run(precise_strings[2]))));
                        row.Cells[8].TextAlignment = TextAlignment.Right;
                        k++;
                    }
                    else if(iop.IsBuffer())
                    {
                        Buffer buff = (Buffer)iop;
                        row.Cells.Add(new TableCell(new Paragraph(new Run(buff.Name))));
                        row.Cells[0].TextAlignment = TextAlignment.Left;
                        row.Cells.Add(new TableCell(new Paragraph(new Run(buff.DesignSpeed.ToString()))));
                        row.Cells[1].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run(buff.DesignSpeed.ToString()))));
                        row.Cells[2].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run("-"))));
                        row.Cells[3].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run("-"))));
                        row.Cells[4].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run("-"))));
                        row.Cells[5].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run("-"))));
                        row.Cells[6].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run(buff.Buffer_Capacity.ToString()))));
                        row.Cells[7].TextAlignment = TextAlignment.Right;
                        row.Cells.Add(new TableCell(new Paragraph(new Run("-"))));
                        row.Cells[8].TextAlignment = TextAlignment.Right;
                    }

                    rg.Rows.Add(row);
                    if (!alternator)
                    {
                        //rg.Rows[rg.Rows.Count - 1].Background = new SolidColorBrush(Colors.LightGray);
                        alternator = true;
                    }
                    else
                    {
                        alternator = false;
                    }

                }

                table.CellSpacing = 0;
                table.RowGroups.Add(rg);

                int table_width = 0;
                foreach(TableColumn column in table.Columns)
                {
                    table_width = table_width + (int)column.Width.Value;
                }
                table.Margin = new Thickness(((int)flowDocument.ColumnWidth - table_width - 50) / 2, 0, 0, 0);
                flowDocument.Blocks.Add(table);

                for (int i = 0; i < buffer_fill.Count; i++)
                {
                    int[] xvals = new int[buffer_fill[i].Length];
                    for (int j = 0; j < buffer_fill[i].Length; j++)
                    {
                        xvals[j] = j + 1;
                    }

                    Chart buff_chart = new Chart();
                    buff_chart.Size = new System.Drawing.Size(1600, 450);
                    ChartArea area = new ChartArea();
                    area.Name = buffers[i].Name;
                    area.AxisX.Title = "Simulation Minutes";
                    area.AxisY.Title = "Buffer Fill (%)";
                    area.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
                    area.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
                    area.AxisX.LabelStyle.Font = new Font("Calibri", 10);
                    area.AxisY.LabelStyle.Font = new Font("Calibri", 10);
                    buff_chart.ChartAreas.Add(area);

                    Series series = new Series();
                    series.Name = "Buffer Fill (%)";
                    series.ChartType = SeriesChartType.Line;
                    series.XValueType = ChartValueType.Int32;
                    series.YValueType = ChartValueType.Auto;
                    buff_chart.Series.Add(series);

                    buff_chart.Titles.Add(buffers[i].Name);
                    buff_chart.Titles[0].Font = new Font("Calibra", 14, System.Drawing.FontStyle.Bold);
                    buff_chart.Titles[0].DockedToChartArea = buffers[i].Name;

                    double[] yvalues = buffer_fill[i].Select(x => (double)x / ((Buffer)process.Activities[3]).Buffer_Capacity).ToArray();

                    buff_chart.Series["Buffer Fill (%)"].Points.DataBindXY(xvals, yvalues);

                    buff_chart.Invalidate();
                    Bitmap chart_bmp = new Bitmap(buff_chart.Size.Width, buff_chart.Size.Height);
                    buff_chart.DrawToBitmap(chart_bmp, new Rectangle(0, 0, chart_bmp.Size.Width, chart_bmp.Size.Height));

                    BitmapImage chart_bi = new BitmapImage();
                    chart_bi = chart_bmp.ToBitmapImage();
                    System.Windows.Controls.Image chart_img = new System.Windows.Controls.Image();
                    chart_img.Height = 210;
                    chart_img.Width = 700;
                    chart_img.Source = chart_bi;
                    figure = new Figure();
                    figure.Height = new FigureLength(220);
                    figure.Width = new FigureLength(700);
                    figure.HorizontalAnchor = FigureHorizontalAnchor.ColumnCenter;
                    figure.Blocks.Add(new BlockUIContainer(chart_img));

                    paragraph = new Paragraph();
                    paragraph.Inlines.Add(figure);
                    flowDocument.Blocks.Add(paragraph);
                }

                DocumentPaginator paginator = ((IDocumentPaginatorSource)flowDocument).DocumentPaginator;
                printDialog.PrintDocument(paginator, "Simulation Report " + DateTime.Now.ToString());
            }
        }

        private string[] UnifyStringPrecision(params string[] strings)
        {
            for(int i = 0; i < strings.Length; i++)
            {
                if (strings[i].Length < 5)
                {
                    if (strings[i].Contains('.') && strings[i][0].Equals("0"))
                    {
                        double temp = Convert.ToDouble(strings[i]);
                        for(int j = 0; j < 5 - strings[i].Length; j++)
                        {
                            temp = temp / 10;
                        }
                        strings[i] = temp.ToString();
                    }
                    else if(strings[i].Contains('.') && !strings[i][0].Equals("0"))
                    {
                        string temp = null;
                        for (int j = 0; j < 5 - strings[i].Length; j++)
                        {
                            temp = String.Concat(temp, "0");
                        }
                        strings[i] = String.Concat(strings[i], temp);
                    }
                    else
                    {
                        string temp = ".";
                        for (int j = 0; j < 5 - 1 - strings[i].Length; j++)  // - 1 allows for the "." already in temp
                        {
                            temp = String.Concat(temp, "0");
                        }
                        strings[i] = String.Concat(strings[i], temp);
                    }
                }
            }

            return strings;
        }

        private void StartActivities()
        {
            for (int r = 0; r < num_runs; r++)
            {
                for (int i = 0; i < process.Activities.Count; i++)
                {
                    if (process.Activities[i].IsUnitOp())
                    {
                        Unit_Op op = (Unit_Op)process.Activities[i];
                        op.Running = true;
                        op.Sim_UpTime();
                        op.Sim_DownTime();
                        op.Sim_DefectTime();
                        op.SetpointSpeed = line_speed;
                        op.Defect_Count = 0;
                    }
                    else if (process.Activities[i].IsBuffer())
                    {
                        Buffer buffer = (Buffer)process.Activities[i];
                        buffer.Buffer_Count = 0;
                        buffer.SetpointSpeed = line_speed;
                    }
                }
            }
        }

        private void PopulateUnitOp(Unit_Op op, int row)
        {
            op.Name = ws.Cells[row, 2].Value.ToString();
            op.DesignSpeed = (int)ws.Cells[row, 10].Value;
            op.SpeedLoss = (int)ws.Cells[row, 5].Value;
            op.MTTR = (int)ws.Cells[row, 13].Value * 60;
            op.MTBF = (int)ws.Cells[row, 14].Value * 60;
            op.QualityLoss = (double)ws.Cells[row, 16].Value;

            if(op.DesignSpeed < line_speed)
            {
                line_speed = op.DesignSpeed;
            }

            unit_ops.Add(op);
        }

        private void PopulateBuffer(Buffer buffer, int row)
        {
            buffer.Name = ws.Cells[row, 2].Value.ToString();
            buffer.DesignSpeed = (int)ws.Cells[row, 10].Value;
            buffer.Buffer_Capacity = (int)ws.Cells[row, 17].Value;

            if(buffer.DesignSpeed < line_speed)
            {
                line_speed = buffer.DesignSpeed;
            }

            buffers.Add(buffer);
        }
    }
}

#region ParallelFor
//public partial class OEE_Ribbon
//{
//    //Establishes the worksheet variable as global dynamic
//    dynamic ws;
//    List<Process> process;

//    //Simulation parameters
//    int Sim_Time_Minutes;
//    int Sim_Time_Seconds;
//    int Line_Rate;
//    int line_speed = int.MaxValue;
//    int num_runs;

//    //Data evaluation
//    double OEE100_Count;
//    double[] prod_count;
//    List<int[]> machine_state;
//    List<int[]> buffer_fill;
//    List<double[]> oee;
//    double[] run_oee;

//    private void OEE_Ribbon_Load(object sender, RibbonUIEventArgs e)
//    {
//        this.btn_Simulate.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.btn_click_Simulate);
//    }

//    private void btn_click_Simulate(object sender, RibbonControlEventArgs e)
//    {
//        //Get the active worksheet from Globals.
//        ws = Globals.ThisAddIn.Application.ActiveSheet;

//        //Set simulation run parameters
//        num_runs = (int)ws.Cells[8, 22].Value;
//        Sim_Time_Minutes = (int)ws.Cells[7, 22].Value;
//        Sim_Time_Seconds = Sim_Time_Minutes * 60;
//        Line_Rate = (int)ws.Cells[9, 12].Value;
//        OEE100_Count = Line_Rate * Sim_Time_Minutes;
//        prod_count = new double[num_runs];

//        //Initialize an empty manufacturing process and empty data
//        process = new List<Process>();
//        machine_state = new List<int[]>();
//        buffer_fill = new List<int[]>();
//        oee = new List<double[]>();
//        run_oee = new double[num_runs];

//        //The first row where the unit operations list begins
//        //The index column is checked for a value before a unit op is added
//        //The first cell in the index column without a value will break the loop
//        int row = 15;
//        int column = 1;
//        int? index = (int?)ws.Cells[row, column].Value;

//        for (int i = 0; i < num_runs; i++)
//        {
//            process.Add(new Process());
//            while (index.HasValue)
//            {
//                //If there is a value > 0 in the buffer column then create buffer in process
//                if (((int?)ws.Cells[row, 17].Value) > 0)
//                {
//                    Buffer newBuffer = new Buffer();
//                    PopulateBuffer(newBuffer, row);
//                    process[i].Activities.Add(newBuffer);
//                    buffer_fill.Add(new int[Sim_Time_Minutes]);
//                }
//                else
//                {
//                    Unit_Op newOp = new Unit_Op();
//                    PopulateUnitOp(newOp, row);
//                    process[i].Activities.Add(newOp);
//                    machine_state.Add(new int[Sim_Time_Minutes]);
//                    oee.Add(new double[num_runs]);
//                }

//                //Iterates current row and redefines index
//                row++;
//                index = (int?)ws.Cells[row, column].Value;
//            }
//            row = 15;
//            index = (int?)ws.Cells[row, column].Value;
//        }

//        Simulation();
//    }

//    private void Simulation()
//    {
//        StartActivities();
//        //Initialize empty data arrays
//        Parallel.For(0, num_runs, run =>
//        {
//            prod_count[run] = 0;
//            for (int Time_Step = 0; Time_Step < Sim_Time_Seconds; Time_Step++)
//            {
//                process[run].Step(Time_Step);

//                //Evaluate product count to find total number of products produced during run
//                if (((Unit_Op)process[run].Activities[process[run].Activities.Count - 1]).Running)
//                {
//                    prod_count[run] = prod_count[run] + ((Unit_Op)process[run].Activities[process[run].Activities.Count - 1]).ActualSpeed / (double)60.0;
//                }

//                //Data collection function
//                //Live data is data that is iterative over the simulation process and must be pulled as it happens
//                //Live data is extracted at every minute time step
//                if (Time_Step % 60 == 0)
//                {
//                    LiveDataCollection(Time_Step / 60, run);
//                }
//            }
//            //Final data is data that is inherently captured as part of the process and does not need iterative calculation
//            FinalDataCollection(run);
//        });
//        OverallDataAnalysis();
//        //GenerateReport();
//    }

//    private void LiveDataCollection(int step, int run)
//    {
//        //Collects data for each unit op and buffer individually
//        int i = 0;  //Used to track Unit_Ops
//        int j = 0;  //Used to track Buffers
//        foreach (IOperations iop in process[run].Activities)
//        {
//            if (iop.IsUnitOp())
//            {
//                //Collects running state
//                Unit_Op op = (Unit_Op)iop;
//                //machine_state[i][step] = Convert.ToInt32(op.Running);
//                i++;
//            }
//            else if (iop.IsBuffer())
//            {
//                //Collects buffer fill count
//                Buffer buff = (Buffer)iop;
//                buffer_fill[j][step] = (int)buff.Buffer_Count;
//                j++;
//            }
//        }
//    }

//    private void FinalDataCollection(int run)
//    {
//        int i = 0;  //Used to track Unit_Ops
//        int j = 0;  //Used to track Buffers
//        foreach (IOperations iop in process[run].Activities)
//        {
//            if (iop.IsUnitOp())
//            {
//                Unit_Op op = (Unit_Op)iop;
//                oee[i][run] = (double)op.TotalUpTime / (double)(op.TotalUpTime + op.TotalDownTime);
//                i++;
//            }
//            else if (iop.IsBuffer())
//            {
//                j++;
//            }
//        }
//        run_oee[run] = prod_count[run] / OEE100_Count;
//    }

//    private void OverallDataAnalysis()
//    {
//        double max = run_oee.Max();
//        double min = run_oee.Min();
//        double test = run_oee.Average();
//    }

//    //private void GenerateReport()
//    //{
//    //    int[] xvals = new int[buffer_fill[0].Length];
//    //    for (int i = 0; i < buffer_fill[0].Length; i++)
//    //    {
//    //        xvals[i] = i + 1;
//    //    }            

//    //    Chart buff_chart = new Chart();
//    //    buff_chart.Size = new System.Drawing.Size(1600, 450);
//    //    ChartArea area = new ChartArea();
//    //    area.AxisX.MajorGrid.LineColor = System.Drawing.Color.LightGray;
//    //    area.AxisY.MajorGrid.LineColor = System.Drawing.Color.LightGray;
//    //    area.AxisX.LabelStyle.Font = new Font("Consolas", 8);
//    //    area.AxisY.LabelStyle.Font = new Font("Consolas", 8);
//    //    buff_chart.ChartAreas.Add(area);

//    //    Series series = new Series();
//    //    series.Name = "Buffer Fill";
//    //    series.ChartType = SeriesChartType.Line;
//    //    series.XValueType = ChartValueType.Int32;
//    //    series.YValueType = ChartValueType.Auto;
//    //    buff_chart.Series.Add(series);

//    //    double[] yvalues = buffer_fill[0].Select(x => (double)x / ((Buffer)process[run].Activities[3]).Buffer_Capacity).ToArray();

//    //    buff_chart.Series["Buffer Fill"].Points.DataBindXY(xvals, yvalues);

//    //    buff_chart.Invalidate();
//    //    buff_chart.SaveImage(@"C:\Users\acduniga\Desktop\test.png", ChartImageFormat.Png);

//    //    buff_chart.Series.Remove(series);
//    //    series.Name = "Running";
//    //    series.ChartType = SeriesChartType.Line;
//    //    series.XValueType = ChartValueType.Auto;
//    //    series.YValueType = ChartValueType.Auto;
//    //    buff_chart.Series.Add(series);

//    //    int[] yval2 = machine_state[0];

//    //    buff_chart.Series["Running"].Points.DataBindXY(xvals, yval2);

//    //    buff_chart.Invalidate();
//    //    buff_chart.SaveImage(@"C:\Users\acduniga\Desktop\test2.png", ChartImageFormat.Png);
//    //}

//    private void StartActivities()
//    {
//        for (int r = 0; r < num_runs; r++)
//        {
//            for (int i = 0; i < process[r].Activities.Count; i++)
//            {
//                if (process[r].Activities[i].IsUnitOp())
//                {
//                    Unit_Op op = (Unit_Op)process[r].Activities[i];
//                    op.Running = true;
//                    op.Sim_UpTime();
//                    op.Sim_DownTime();
//                    op.SetpointSpeed = line_speed;
//                }
//                else if (process[r].Activities[i].IsBuffer())
//                {
//                    Buffer buffer = (Buffer)process[r].Activities[i];
//                    buffer.Buffer_Count = 0;
//                    buffer.SetpointSpeed = line_speed;
//                }
//            }
//        }
//    }

//    private void PopulateUnitOp(Unit_Op op, int row)
//    {
//        op.Name = ws.Cells[row, 2].Value.ToString();
//        op.DesignSpeed = (int)ws.Cells[row, 10].Value;
//        op.SpeedLoss = (int)ws.Cells[row, 5].Value;
//        op.MTTR = (int)ws.Cells[row, 13].Value * 60;
//        op.MTBF = (int)ws.Cells[row, 14].Value * 60;
//        op.QualityLoss = (double?)ws.Cells[row, 16].Value;

//        if (op.DesignSpeed < line_speed)
//        {
//            line_speed = op.DesignSpeed;
//        }
//    }

//    private void PopulateBuffer(Buffer buffer, int row)
//    {
//        buffer.Name = ws.Cells[row, 2].Value.ToString();
//        buffer.DesignSpeed = (int)ws.Cells[row, 10].Value;
//        buffer.Buffer_Capacity = (int)ws.Cells[row, 17].Value;

//        if (buffer.DesignSpeed < line_speed)
//        {
//            line_speed = buffer.DesignSpeed;
//        }
//    }
//}
#endregion


