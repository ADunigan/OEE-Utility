namespace OEE_ExcelAddIn_2010
{
    partial class OEE_Ribbon : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public OEE_Ribbon()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OEE_Ribbon));
            this.tab_Simulator = this.Factory.CreateRibbonTab();
            this.group_Simulate = this.Factory.CreateRibbonGroup();
            this.btn_Simulate = this.Factory.CreateRibbonButton();
            this.tab_Simulator.SuspendLayout();
            this.group_Simulate.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab_Simulator
            // 
            this.tab_Simulator.Groups.Add(this.group_Simulate);
            this.tab_Simulator.Label = "Line Simulator";
            this.tab_Simulator.Name = "tab_Simulator";
            // 
            // group_Simulate
            // 
            this.group_Simulate.Items.Add(this.btn_Simulate);
            this.group_Simulate.Label = "Simulate";
            this.group_Simulate.Name = "group_Simulate";
            // 
            // btn_Simulate
            // 
            this.btn_Simulate.ControlSize = Microsoft.Office.Core.RibbonControlSize.RibbonControlSizeLarge;
            this.btn_Simulate.Image = ((System.Drawing.Image)(resources.GetObject("btn_Simulate.Image")));
            this.btn_Simulate.Label = "Start Simulation";
            this.btn_Simulate.Name = "btn_Simulate";
            this.btn_Simulate.ShowImage = true;
            // 
            // OEE_Ribbon
            // 
            this.Name = "OEE_Ribbon";
            this.RibbonType = "Microsoft.Excel.Workbook";
            this.Tabs.Add(this.tab_Simulator);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.OEE_Ribbon_Load);
            this.tab_Simulator.ResumeLayout(false);
            this.tab_Simulator.PerformLayout();
            this.group_Simulate.ResumeLayout(false);
            this.group_Simulate.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab_Simulator;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group_Simulate;
        internal Microsoft.Office.Tools.Ribbon.RibbonButton btn_Simulate;
    }

    partial class ThisRibbonCollection
    {
        internal OEE_Ribbon OEE_Ribbon
        {
            get { return this.GetRibbon<OEE_Ribbon>(); }
        }
    }
}
