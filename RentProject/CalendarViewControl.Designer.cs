namespace RentProject
{
    partial class CalendarViewControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            DevExpress.XtraScheduler.TimeRuler timeRuler4 = new DevExpress.XtraScheduler.TimeRuler();
            DevExpress.XtraScheduler.TimeRuler timeRuler5 = new DevExpress.XtraScheduler.TimeRuler();
            DevExpress.XtraScheduler.TimeRuler timeRuler6 = new DevExpress.XtraScheduler.TimeRuler();
            lblTitle = new DevExpress.XtraEditors.LabelControl();
            splitContainerControl1 = new DevExpress.XtraEditors.SplitContainerControl();
            calendarPanel = new DevExpress.XtraEditors.PanelControl();
            schedulerControl1 = new DevExpress.XtraScheduler.SchedulerControl();
            schedulerDataStorage1 = new DevExpress.XtraScheduler.SchedulerDataStorage(components);
            panelControl1 = new DevExpress.XtraEditors.PanelControl();
            btnNextMonth = new DevExpress.XtraEditors.SimpleButton();
            btnPrevMonth = new DevExpress.XtraEditors.SimpleButton();
            grpDetail = new DevExpress.XtraEditors.GroupControl();
            memoDetail = new DevExpress.XtraEditors.MemoEdit();
            cboRentTime = new DevExpress.XtraEditors.ComboBoxEdit();
            ((System.ComponentModel.ISupportInitialize)splitContainerControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainerControl1.Panel1).BeginInit();
            splitContainerControl1.Panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerControl1.Panel2).BeginInit();
            splitContainerControl1.Panel2.SuspendLayout();
            splitContainerControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)calendarPanel).BeginInit();
            calendarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)schedulerControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)schedulerDataStorage1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)panelControl1).BeginInit();
            panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)grpDetail).BeginInit();
            grpDetail.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)memoDetail.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)cboRentTime.Properties).BeginInit();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.Appearance.Font = new System.Drawing.Font("Tahoma", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblTitle.Appearance.Options.UseFont = true;
            lblTitle.Appearance.Options.UseTextOptions = true;
            lblTitle.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            lblTitle.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            lblTitle.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            lblTitle.Location = new System.Drawing.Point(0, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(1800, 1007);
            lblTitle.TabIndex = 0;
            // 
            // splitContainerControl1
            // 
            splitContainerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainerControl1.FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel2;
            splitContainerControl1.Location = new System.Drawing.Point(0, 0);
            splitContainerControl1.Name = "splitContainerControl1";
            // 
            // splitContainerControl1.Panel1
            // 
            splitContainerControl1.Panel1.Controls.Add(calendarPanel);
            splitContainerControl1.Panel1.Text = "Panel1";
            // 
            // splitContainerControl1.Panel2
            // 
            splitContainerControl1.Panel2.Controls.Add(grpDetail);
            splitContainerControl1.Panel2.MinSize = 300;
            splitContainerControl1.Panel2.Text = "Panel2";
            splitContainerControl1.Size = new System.Drawing.Size(1800, 1007);
            splitContainerControl1.SplitterPosition = 400;
            splitContainerControl1.TabIndex = 1;
            // 
            // calendarPanel
            // 
            calendarPanel.Controls.Add(schedulerControl1);
            calendarPanel.Controls.Add(panelControl1);
            calendarPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            calendarPanel.Location = new System.Drawing.Point(0, 0);
            calendarPanel.Name = "calendarPanel";
            calendarPanel.Size = new System.Drawing.Size(1385, 1007);
            calendarPanel.TabIndex = 0;
            // 
            // schedulerControl1
            // 
            schedulerControl1.ActiveViewType = DevExpress.XtraScheduler.SchedulerViewType.Month;
            schedulerControl1.DataStorage = schedulerDataStorage1;
            schedulerControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            schedulerControl1.Location = new System.Drawing.Point(2, 65);
            schedulerControl1.Name = "schedulerControl1";
            schedulerControl1.OptionsDragDrop.AutoScrollEnabled = false;
            schedulerControl1.Size = new System.Drawing.Size(1381, 940);
            schedulerControl1.Start = new System.DateTime(2025, 12, 28, 0, 0, 0, 0);
            schedulerControl1.TabIndex = 0;
            schedulerControl1.Text = "schedulerControl1";
            schedulerControl1.Views.DayView.TimeRulers.Add(timeRuler4);
            schedulerControl1.Views.FullWeekView.Enabled = true;
            schedulerControl1.Views.FullWeekView.TimeRulers.Add(timeRuler5);
            schedulerControl1.Views.WeekView.Enabled = false;
            schedulerControl1.Views.WorkWeekView.TimeRulers.Add(timeRuler6);
            schedulerControl1.Views.YearView.UseOptimizedScrolling = false;
            schedulerControl1.MouseDown += schedulerControl1_MouseDown;
            // 
            // schedulerDataStorage1
            // 
            // 
            // 
            // 
            schedulerDataStorage1.AppointmentDependencies.AutoReload = false;
            // 
            // 
            // 
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(0, "None", "&None", System.Drawing.SystemColors.Window);
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(1, "Important", "&Important", System.Drawing.Color.FromArgb(255, 194, 190));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(2, "Business", "&Business", System.Drawing.Color.FromArgb(168, 213, 255));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(3, "Personal", "&Personal", System.Drawing.Color.FromArgb(193, 244, 156));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(4, "Vacation", "&Vacation", System.Drawing.Color.FromArgb(243, 228, 199));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(5, "Must Attend", "Must &Attend", System.Drawing.Color.FromArgb(244, 206, 147));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(6, "Travel Required", "&Travel Required", System.Drawing.Color.FromArgb(199, 244, 255));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(7, "Needs Preparation", "&Needs Preparation", System.Drawing.Color.FromArgb(207, 219, 152));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(8, "Birthday", "&Birthday", System.Drawing.Color.FromArgb(224, 207, 233));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(9, "Anniversary", "&Anniversary", System.Drawing.Color.FromArgb(141, 233, 223));
            schedulerDataStorage1.Appointments.Labels.CreateNewLabel(10, "Phone Call", "Phone &Call", System.Drawing.Color.FromArgb(255, 247, 165));
            // 
            // panelControl1
            // 
            panelControl1.Controls.Add(btnNextMonth);
            panelControl1.Controls.Add(btnPrevMonth);
            panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            panelControl1.Location = new System.Drawing.Point(2, 2);
            panelControl1.Name = "panelControl1";
            panelControl1.Size = new System.Drawing.Size(1381, 63);
            panelControl1.TabIndex = 2;
            // 
            // btnNextMonth
            // 
            btnNextMonth.Dock = System.Windows.Forms.DockStyle.Right;
            btnNextMonth.Location = new System.Drawing.Point(1282, 2);
            btnNextMonth.Name = "btnNextMonth";
            btnNextMonth.Size = new System.Drawing.Size(97, 59);
            btnNextMonth.TabIndex = 1;
            btnNextMonth.Text = "Next";
            btnNextMonth.Click += btnNextMonth_Click;
            // 
            // btnPrevMonth
            // 
            btnPrevMonth.Dock = System.Windows.Forms.DockStyle.Left;
            btnPrevMonth.Location = new System.Drawing.Point(2, 2);
            btnPrevMonth.Name = "btnPrevMonth";
            btnPrevMonth.Size = new System.Drawing.Size(97, 59);
            btnPrevMonth.TabIndex = 0;
            btnPrevMonth.Text = "Preview";
            btnPrevMonth.Click += btnPrevMonth_Click;
            // 
            // grpDetail
            // 
            grpDetail.Controls.Add(memoDetail);
            grpDetail.Controls.Add(cboRentTime);
            grpDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            grpDetail.Location = new System.Drawing.Point(0, 0);
            grpDetail.Name = "grpDetail";
            grpDetail.Size = new System.Drawing.Size(400, 1007);
            grpDetail.TabIndex = 0;
            grpDetail.Text = "詳細資訊";
            // 
            // memoDetail
            // 
            memoDetail.Dock = System.Windows.Forms.DockStyle.Fill;
            memoDetail.Location = new System.Drawing.Point(2, 62);
            memoDetail.Name = "memoDetail";
            memoDetail.Size = new System.Drawing.Size(396, 943);
            memoDetail.TabIndex = 1;
            // 
            // cboRentTime
            // 
            cboRentTime.Dock = System.Windows.Forms.DockStyle.Top;
            cboRentTime.Location = new System.Drawing.Point(2, 34);
            cboRentTime.Name = "cboRentTime";
            cboRentTime.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cboRentTime.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            cboRentTime.Size = new System.Drawing.Size(396, 28);
            cboRentTime.TabIndex = 0;
            // 
            // CalendarViewControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 22F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(splitContainerControl1);
            Controls.Add(lblTitle);
            Name = "CalendarViewControl";
            Size = new System.Drawing.Size(1800, 1007);
            Load += CalendarViewControl_Load;
            ((System.ComponentModel.ISupportInitialize)splitContainerControl1.Panel1).EndInit();
            splitContainerControl1.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerControl1.Panel2).EndInit();
            splitContainerControl1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerControl1).EndInit();
            splitContainerControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)calendarPanel).EndInit();
            calendarPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)schedulerControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)schedulerDataStorage1).EndInit();
            ((System.ComponentModel.ISupportInitialize)panelControl1).EndInit();
            panelControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)grpDetail).EndInit();
            grpDetail.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)memoDetail.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)cboRentTime.Properties).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraEditors.LabelControl lblTitle;
        private DevExpress.XtraEditors.SplitContainerControl splitContainerControl1;
        private DevExpress.XtraEditors.PanelControl calendarPanel;
        private DevExpress.XtraEditors.GroupControl grpDetail;
        private DevExpress.XtraScheduler.SchedulerControl schedulerControl1;
        private DevExpress.XtraScheduler.SchedulerDataStorage schedulerDataStorage1;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.SimpleButton btnNextMonth;
        private DevExpress.XtraEditors.SimpleButton btnPrevMonth;
        private DevExpress.XtraEditors.MemoEdit memoDetail;
        private DevExpress.XtraEditors.ComboBoxEdit cboRentTime;
    }
}
