namespace RentProject
{
    partial class AdvancedFilterForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DevExpress.XtraLayout.ColumnDefinition columnDefinition1 = new DevExpress.XtraLayout.ColumnDefinition();
            DevExpress.XtraLayout.ColumnDefinition columnDefinition2 = new DevExpress.XtraLayout.ColumnDefinition();
            DevExpress.XtraLayout.ColumnDefinition columnDefinition3 = new DevExpress.XtraLayout.ColumnDefinition();
            DevExpress.XtraLayout.RowDefinition rowDefinition1 = new DevExpress.XtraLayout.RowDefinition();
            DevExpress.XtraLayout.RowDefinition rowDefinition2 = new DevExpress.XtraLayout.RowDefinition();
            DevExpress.XtraLayout.RowDefinition rowDefinition3 = new DevExpress.XtraLayout.RowDefinition();
            DevExpress.XtraLayout.RowDefinition rowDefinition4 = new DevExpress.XtraLayout.RowDefinition();
            DevExpress.XtraLayout.RowDefinition rowDefinition5 = new DevExpress.XtraLayout.RowDefinition();
            layoutControl1 = new DevExpress.XtraLayout.LayoutControl();
            EndDate = new DevExpress.XtraEditors.DateEdit();
            StartDate = new DevExpress.XtraEditors.DateEdit();
            cmbStatus = new DevExpress.XtraEditors.ComboBoxEdit();
            cmbPE = new DevExpress.XtraEditors.ComboBoxEdit();
            cmbCompany = new DevExpress.XtraEditors.ComboBoxEdit();
            cmbLocation = new DevExpress.XtraEditors.ComboBoxEdit();
            cmbProjectName = new DevExpress.XtraEditors.ComboBoxEdit();
            cmbArea = new DevExpress.XtraEditors.ComboBoxEdit();
            cmbProjectNo = new DevExpress.XtraEditors.ComboBoxEdit();
            cmbBookingNo = new DevExpress.XtraEditors.ComboBoxEdit();
            panelControl1 = new DevExpress.XtraEditors.PanelControl();
            btnCancel = new DevExpress.XtraEditors.SimpleButton();
            btnFiltered = new DevExpress.XtraEditors.SimpleButton();
            Root = new DevExpress.XtraLayout.LayoutControlGroup();
            layoutControlGroup1 = new DevExpress.XtraLayout.LayoutControlGroup();
            lblBookingNo = new DevExpress.XtraLayout.LayoutControlItem();
            lblProjectNo = new DevExpress.XtraLayout.LayoutControlItem();
            lblArea = new DevExpress.XtraLayout.LayoutControlItem();
            lblProjectName = new DevExpress.XtraLayout.LayoutControlItem();
            lblLocation = new DevExpress.XtraLayout.LayoutControlItem();
            lblCompany = new DevExpress.XtraLayout.LayoutControlItem();
            lblPE = new DevExpress.XtraLayout.LayoutControlItem();
            lblStatus = new DevExpress.XtraLayout.LayoutControlItem();
            lblStartDate = new DevExpress.XtraLayout.LayoutControlItem();
            lblEndDate = new DevExpress.XtraLayout.LayoutControlItem();
            layoutControlItem1 = new DevExpress.XtraLayout.LayoutControlItem();
            ((System.ComponentModel.ISupportInitialize)layoutControl1).BeginInit();
            layoutControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)EndDate.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)EndDate.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)StartDate.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)StartDate.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbStatus.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbPE.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbCompany.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbLocation.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbProjectName.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbArea.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbProjectNo.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cmbBookingNo.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)panelControl1).BeginInit();
            panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)Root).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlGroup1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblBookingNo).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblProjectNo).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblArea).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblProjectName).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblLocation).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblCompany).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblPE).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblStatus).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblStartDate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)lblEndDate).BeginInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItem1).BeginInit();
            SuspendLayout();
            // 
            // layoutControl1
            // 
            layoutControl1.Controls.Add(EndDate);
            layoutControl1.Controls.Add(StartDate);
            layoutControl1.Controls.Add(cmbStatus);
            layoutControl1.Controls.Add(cmbPE);
            layoutControl1.Controls.Add(cmbCompany);
            layoutControl1.Controls.Add(cmbLocation);
            layoutControl1.Controls.Add(cmbProjectName);
            layoutControl1.Controls.Add(cmbArea);
            layoutControl1.Controls.Add(cmbProjectNo);
            layoutControl1.Controls.Add(cmbBookingNo);
            layoutControl1.Controls.Add(panelControl1);
            layoutControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            layoutControl1.Location = new System.Drawing.Point(0, 0);
            layoutControl1.Name = "layoutControl1";
            layoutControl1.OptionsCustomizationForm.DesignTimeCustomizationFormPositionAndSize = new System.Drawing.Rectangle(3985, 308, 975, 600);
            layoutControl1.Root = Root;
            layoutControl1.Size = new System.Drawing.Size(801, 353);
            layoutControl1.TabIndex = 0;
            layoutControl1.Text = "layoutControl1";
            // 
            // EndDate
            // 
            EndDate.EditValue = null;
            EndDate.Location = new System.Drawing.Point(572, 213);
            EndDate.Name = "EndDate";
            EndDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            EndDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            EndDate.Size = new System.Drawing.Size(194, 28);
            EndDate.StyleController = layoutControl1;
            EndDate.TabIndex = 14;
            // 
            // StartDate
            // 
            StartDate.EditValue = null;
            StartDate.Location = new System.Drawing.Point(185, 213);
            StartDate.Name = "StartDate";
            StartDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            StartDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            StartDate.Size = new System.Drawing.Size(194, 28);
            StartDate.StyleController = layoutControl1;
            StartDate.TabIndex = 13;
            // 
            // cmbStatus
            // 
            cmbStatus.Location = new System.Drawing.Point(572, 168);
            cmbStatus.Name = "cmbStatus";
            cmbStatus.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbStatus.Size = new System.Drawing.Size(194, 28);
            cmbStatus.StyleController = layoutControl1;
            cmbStatus.TabIndex = 12;
            // 
            // cmbPE
            // 
            cmbPE.Location = new System.Drawing.Point(185, 168);
            cmbPE.Name = "cmbPE";
            cmbPE.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbPE.Size = new System.Drawing.Size(194, 28);
            cmbPE.StyleController = layoutControl1;
            cmbPE.TabIndex = 11;
            // 
            // cmbCompany
            // 
            cmbCompany.Location = new System.Drawing.Point(572, 123);
            cmbCompany.Name = "cmbCompany";
            cmbCompany.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbCompany.Size = new System.Drawing.Size(194, 28);
            cmbCompany.StyleController = layoutControl1;
            cmbCompany.TabIndex = 10;
            // 
            // cmbLocation
            // 
            cmbLocation.Location = new System.Drawing.Point(185, 123);
            cmbLocation.Name = "cmbLocation";
            cmbLocation.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbLocation.Size = new System.Drawing.Size(194, 28);
            cmbLocation.StyleController = layoutControl1;
            cmbLocation.TabIndex = 9;
            // 
            // cmbProjectName
            // 
            cmbProjectName.Location = new System.Drawing.Point(572, 78);
            cmbProjectName.Name = "cmbProjectName";
            cmbProjectName.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbProjectName.Size = new System.Drawing.Size(194, 28);
            cmbProjectName.StyleController = layoutControl1;
            cmbProjectName.TabIndex = 8;
            // 
            // cmbArea
            // 
            cmbArea.Location = new System.Drawing.Point(185, 78);
            cmbArea.Name = "cmbArea";
            cmbArea.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbArea.Size = new System.Drawing.Size(194, 28);
            cmbArea.StyleController = layoutControl1;
            cmbArea.TabIndex = 7;
            // 
            // cmbProjectNo
            // 
            cmbProjectNo.Location = new System.Drawing.Point(572, 35);
            cmbProjectNo.Name = "cmbProjectNo";
            cmbProjectNo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbProjectNo.Size = new System.Drawing.Size(194, 28);
            cmbProjectNo.StyleController = layoutControl1;
            cmbProjectNo.TabIndex = 6;
            // 
            // cmbBookingNo
            // 
            cmbBookingNo.Location = new System.Drawing.Point(185, 35);
            cmbBookingNo.Name = "cmbBookingNo";
            cmbBookingNo.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbBookingNo.Size = new System.Drawing.Size(194, 28);
            cmbBookingNo.StyleController = layoutControl1;
            cmbBookingNo.TabIndex = 5;
            // 
            // panelControl1
            // 
            panelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            panelControl1.Controls.Add(btnCancel);
            panelControl1.Controls.Add(btnFiltered);
            panelControl1.Location = new System.Drawing.Point(18, 275);
            panelControl1.Name = "panelControl1";
            panelControl1.Size = new System.Drawing.Size(765, 60);
            panelControl1.TabIndex = 4;
            // 
            // btnCancel
            // 
            btnCancel.Location = new System.Drawing.Point(408, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(168, 51);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "取消";
            btnCancel.Click += btnCancel_Click;
            // 
            // btnFiltered
            // 
            btnFiltered.Location = new System.Drawing.Point(176, 3);
            btnFiltered.Name = "btnFiltered";
            btnFiltered.Size = new System.Drawing.Size(168, 51);
            btnFiltered.TabIndex = 0;
            btnFiltered.Text = "篩選";
            btnFiltered.Click += btnFiltered_Click;
            // 
            // Root
            // 
            Root.EnableIndentsWithoutBorders = DevExpress.Utils.DefaultBoolean.True;
            Root.GroupBordersVisible = false;
            Root.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] { layoutControlGroup1, layoutControlItem1 });
            Root.Name = "Root";
            Root.Size = new System.Drawing.Size(801, 353);
            Root.TextVisible = false;
            // 
            // layoutControlGroup1
            // 
            layoutControlGroup1.Items.AddRange(new DevExpress.XtraLayout.BaseLayoutItem[] { lblBookingNo, lblProjectNo, lblArea, lblProjectName, lblLocation, lblCompany, lblPE, lblStatus, lblStartDate, lblEndDate });
            layoutControlGroup1.LayoutMode = DevExpress.XtraLayout.Utils.LayoutMode.Table;
            layoutControlGroup1.Location = new System.Drawing.Point(0, 0);
            layoutControlGroup1.Name = "layoutControlGroup1";
            columnDefinition1.SizeType = System.Windows.Forms.SizeType.Percent;
            columnDefinition1.Width = 47.5D;
            columnDefinition2.SizeType = System.Windows.Forms.SizeType.Percent;
            columnDefinition2.Width = 5D;
            columnDefinition3.SizeType = System.Windows.Forms.SizeType.Percent;
            columnDefinition3.Width = 47.5D;
            layoutControlGroup1.OptionsTableLayoutGroup.ColumnDefinitions.AddRange(new DevExpress.XtraLayout.ColumnDefinition[] { columnDefinition1, columnDefinition2, columnDefinition3 });
            rowDefinition1.Height = 20D;
            rowDefinition1.SizeType = System.Windows.Forms.SizeType.Percent;
            rowDefinition2.Height = 20D;
            rowDefinition2.SizeType = System.Windows.Forms.SizeType.Percent;
            rowDefinition3.Height = 20D;
            rowDefinition3.SizeType = System.Windows.Forms.SizeType.Percent;
            rowDefinition4.Height = 20D;
            rowDefinition4.SizeType = System.Windows.Forms.SizeType.Percent;
            rowDefinition5.Height = 20D;
            rowDefinition5.SizeType = System.Windows.Forms.SizeType.Percent;
            layoutControlGroup1.OptionsTableLayoutGroup.RowDefinitions.AddRange(new DevExpress.XtraLayout.RowDefinition[] { rowDefinition1, rowDefinition2, rowDefinition3, rowDefinition4, rowDefinition5 });
            layoutControlGroup1.Size = new System.Drawing.Size(771, 257);
            layoutControlGroup1.TextVisible = false;
            // 
            // lblBookingNo
            // 
            lblBookingNo.Control = cmbBookingNo;
            lblBookingNo.Location = new System.Drawing.Point(0, 0);
            lblBookingNo.Name = "lblBookingNo";
            lblBookingNo.Size = new System.Drawing.Size(350, 43);
            lblBookingNo.Text = "BookingNo.";
            lblBookingNo.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblProjectNo
            // 
            lblProjectNo.Control = cmbProjectNo;
            lblProjectNo.Location = new System.Drawing.Point(387, 0);
            lblProjectNo.Name = "lblProjectNo";
            lblProjectNo.OptionsTableLayoutItem.ColumnIndex = 2;
            lblProjectNo.Size = new System.Drawing.Size(350, 43);
            lblProjectNo.Text = "ProjectNo";
            lblProjectNo.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblArea
            // 
            lblArea.Control = cmbArea;
            lblArea.Location = new System.Drawing.Point(0, 43);
            lblArea.Name = "lblArea";
            lblArea.OptionsTableLayoutItem.RowIndex = 1;
            lblArea.Size = new System.Drawing.Size(350, 45);
            lblArea.Text = "區域";
            lblArea.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblProjectName
            // 
            lblProjectName.Control = cmbProjectName;
            lblProjectName.Location = new System.Drawing.Point(387, 43);
            lblProjectName.Name = "lblProjectName";
            lblProjectName.OptionsTableLayoutItem.ColumnIndex = 2;
            lblProjectName.OptionsTableLayoutItem.RowIndex = 1;
            lblProjectName.Size = new System.Drawing.Size(350, 45);
            lblProjectName.Text = "Project Name";
            lblProjectName.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblLocation
            // 
            lblLocation.Control = cmbLocation;
            lblLocation.Location = new System.Drawing.Point(0, 88);
            lblLocation.Name = "lblLocation";
            lblLocation.OptionsTableLayoutItem.RowIndex = 2;
            lblLocation.Size = new System.Drawing.Size(350, 45);
            lblLocation.Text = "場地";
            lblLocation.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblCompany
            // 
            lblCompany.Control = cmbCompany;
            lblCompany.Location = new System.Drawing.Point(387, 88);
            lblCompany.Name = "lblCompany";
            lblCompany.OptionsTableLayoutItem.ColumnIndex = 2;
            lblCompany.OptionsTableLayoutItem.RowIndex = 2;
            lblCompany.Size = new System.Drawing.Size(350, 45);
            lblCompany.Text = "客戶名稱";
            lblCompany.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblPE
            // 
            lblPE.Control = cmbPE;
            lblPE.Location = new System.Drawing.Point(0, 133);
            lblPE.Name = "lblPE";
            lblPE.OptionsTableLayoutItem.RowIndex = 3;
            lblPE.Size = new System.Drawing.Size(350, 45);
            lblPE.Text = "PE";
            lblPE.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblStatus
            // 
            lblStatus.Control = cmbStatus;
            lblStatus.Location = new System.Drawing.Point(387, 133);
            lblStatus.Name = "lblStatus";
            lblStatus.OptionsTableLayoutItem.ColumnIndex = 2;
            lblStatus.OptionsTableLayoutItem.RowIndex = 3;
            lblStatus.Size = new System.Drawing.Size(350, 45);
            lblStatus.Text = "租時單狀態";
            lblStatus.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblStartDate
            // 
            lblStartDate.Control = StartDate;
            lblStartDate.CustomizationFormText = "開始日期 (From)";
            lblStartDate.Location = new System.Drawing.Point(0, 178);
            lblStartDate.Name = "lblStartDate";
            lblStartDate.OptionsTableLayoutItem.RowIndex = 4;
            lblStartDate.Size = new System.Drawing.Size(350, 45);
            lblStartDate.Text = "開始日期 (From)";
            lblStartDate.TextSize = new System.Drawing.Size(132, 22);
            // 
            // lblEndDate
            // 
            lblEndDate.Control = EndDate;
            lblEndDate.Location = new System.Drawing.Point(387, 178);
            lblEndDate.Name = "lblEndDate";
            lblEndDate.OptionsTableLayoutItem.ColumnIndex = 2;
            lblEndDate.OptionsTableLayoutItem.RowIndex = 4;
            lblEndDate.Size = new System.Drawing.Size(350, 45);
            lblEndDate.Text = "結束日期 (To)";
            lblEndDate.TextSize = new System.Drawing.Size(132, 22);
            // 
            // layoutControlItem1
            // 
            layoutControlItem1.Control = panelControl1;
            layoutControlItem1.Location = new System.Drawing.Point(0, 257);
            layoutControlItem1.Name = "layoutControlItem1";
            layoutControlItem1.Size = new System.Drawing.Size(771, 66);
            layoutControlItem1.TextVisible = false;
            // 
            // AdvancedFilterForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 22F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(801, 353);
            Controls.Add(layoutControl1);
            IconOptions.ShowIcon = false;
            Name = "AdvancedFilterForm";
            Text = "租時單管理 - 進階篩選";
            TopMost = true;
            WindowState = System.Windows.Forms.FormWindowState.Minimized;
            ((System.ComponentModel.ISupportInitialize)layoutControl1).EndInit();
            layoutControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)EndDate.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)EndDate.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)StartDate.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)StartDate.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbStatus.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbPE.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbCompany.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbLocation.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbProjectName.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbArea.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbProjectNo.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cmbBookingNo.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)panelControl1).EndInit();
            panelControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)Root).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlGroup1).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblBookingNo).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblProjectNo).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblArea).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblProjectName).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblLocation).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblCompany).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblPE).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblStatus).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblStartDate).EndInit();
            ((System.ComponentModel.ISupportInitialize)lblEndDate).EndInit();
            ((System.ComponentModel.ISupportInitialize)layoutControlItem1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraLayout.LayoutControl layoutControl1;
        private DevExpress.XtraLayout.LayoutControlGroup Root;
        private DevExpress.XtraLayout.LayoutControlGroup layoutControlGroup1;

        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraLayout.LayoutControlItem layoutControlItem1;

        private DevExpress.XtraEditors.ComboBoxEdit cmbBookingNo;
        private DevExpress.XtraLayout.LayoutControlItem lblBookingNo;

        private DevExpress.XtraEditors.ComboBoxEdit cmbProjectNo;
        private DevExpress.XtraLayout.LayoutControlItem lblProjectNo;

        private DevExpress.XtraEditors.ComboBoxEdit cmbArea;
        private DevExpress.XtraLayout.LayoutControlItem lblArea;

        private DevExpress.XtraEditors.ComboBoxEdit cmbProjectName;
        private DevExpress.XtraLayout.LayoutControlItem lblProjectName;

        private DevExpress.XtraEditors.ComboBoxEdit cmbLocation;
        private DevExpress.XtraLayout.LayoutControlItem lblLocation;

        private DevExpress.XtraEditors.ComboBoxEdit cmbCompany;
        private DevExpress.XtraLayout.LayoutControlItem lblCompany;

        private DevExpress.XtraEditors.ComboBoxEdit cmbPE;
        private DevExpress.XtraLayout.LayoutControlItem lblPE;

        private DevExpress.XtraEditors.ComboBoxEdit cmbStatus;
        private DevExpress.XtraLayout.LayoutControlItem lblStatus;

        private DevExpress.XtraEditors.DateEdit StartDate;
        private DevExpress.XtraLayout.LayoutControlItem lblStartDate;

        private DevExpress.XtraEditors.DateEdit EndDate;
        private DevExpress.XtraLayout.LayoutControlItem lblEndDate;

        private DevExpress.XtraEditors.SimpleButton btnFiltered;
        private DevExpress.XtraEditors.SimpleButton btnCancel;
    }
}
