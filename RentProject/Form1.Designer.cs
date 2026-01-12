namespace RentProject
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            ribbonPage1 = new DevExpress.XtraBars.Ribbon.RibbonPage();
            ribbonPageGroup1 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            btnAddRentTime = new DevExpress.XtraBars.BarButtonItem();
            btnDelete = new DevExpress.XtraBars.BarButtonItem();
            btnView = new DevExpress.XtraBars.BarButtonItem();
            ribbonPageGroup2 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            btnExport = new DevExpress.XtraBars.BarButtonItem();
            btnSubmitToAssistant = new DevExpress.XtraBars.BarButtonItem();
            ribbonPageGroup3 = new DevExpress.XtraBars.Ribbon.RibbonPageGroup();
            btnTestConnection = new DevExpress.XtraBars.BarButtonItem();
            btnLogout = new DevExpress.XtraBars.BarButtonItem();
            ribbonControl1 = new DevExpress.XtraBars.Ribbon.RibbonControl();
            barManager1 = new DevExpress.XtraBars.BarManager(components);
            barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            mainPanel = new DevExpress.XtraEditors.PanelControl();
            filterPanel = new DevExpress.XtraEditors.PanelControl();
            cmbLocationFilter = new DevExpress.XtraEditors.ComboBoxEdit();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)ribbonControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)mainPanel).BeginInit();
            mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)filterPanel).BeginInit();
            filterPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)cmbLocationFilter.Properties).BeginInit();
            SuspendLayout();
            // 
            // ribbonPage1
            // 
            ribbonPage1.Groups.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPageGroup[] { ribbonPageGroup1, ribbonPageGroup2, ribbonPageGroup3 });
            ribbonPage1.Name = "ribbonPage1";
            ribbonPage1.Text = "ribbonPage1";
            // 
            // ribbonPageGroup1
            // 
            ribbonPageGroup1.AllowTextClipping = false;
            ribbonPageGroup1.ItemLinks.Add(btnAddRentTime);
            ribbonPageGroup1.ItemLinks.Add(btnDelete);
            ribbonPageGroup1.ItemLinks.Add(btnView);
            ribbonPageGroup1.Name = "ribbonPageGroup1";
            ribbonPageGroup1.Text = "Common";
            // 
            // btnAddRentTime
            // 
            btnAddRentTime.Caption = "新增租時單";
            btnAddRentTime.Id = 1;
            btnAddRentTime.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("btnAddRentTime.ImageOptions.Image");
            btnAddRentTime.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("btnAddRentTime.ImageOptions.LargeImage");
            btnAddRentTime.Name = "btnAddRentTime";
            btnAddRentTime.ItemClick += btnAddRentTime_ItemClick;
            // 
            // btnDelete
            // 
            btnDelete.Caption = "刪除租時單";
            btnDelete.Id = 4;
            btnDelete.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("btnDelete.ImageOptions.Image");
            btnDelete.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("btnDelete.ImageOptions.LargeImage");
            btnDelete.Name = "btnDelete";
            btnDelete.ItemClick += btnDelete_ItemClick;
            // 
            // btnView
            // 
            btnView.Caption = "切換檢視模式";
            btnView.Id = 3;
            btnView.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("btnView.ImageOptions.Image");
            btnView.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("btnView.ImageOptions.LargeImage");
            btnView.Name = "btnView";
            btnView.ItemClick += btnView_ItemClick;
            // 
            // ribbonPageGroup2
            // 
            ribbonPageGroup2.ItemLinks.Add(btnExport);
            ribbonPageGroup2.ItemLinks.Add(btnSubmitToAssistant);
            ribbonPageGroup2.Name = "ribbonPageGroup2";
            ribbonPageGroup2.Text = "Print and Export";
            // 
            // btnExport
            // 
            btnExport.Caption = "匯出清單";
            btnExport.Id = 5;
            btnExport.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("btnExport.ImageOptions.Image");
            btnExport.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("btnExport.ImageOptions.LargeImage");
            btnExport.Name = "btnExport";
            // 
            // btnSubmitToAssistant
            // 
            btnSubmitToAssistant.Caption = "送出給助理";
            btnSubmitToAssistant.Id = 6;
            btnSubmitToAssistant.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("btnSubmitToAssistant.ImageOptions.Image");
            btnSubmitToAssistant.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("btnSubmitToAssistant.ImageOptions.LargeImage");
            btnSubmitToAssistant.Name = "btnSubmitToAssistant";
            btnSubmitToAssistant.ItemClick += btnSubmitToAssistant_ItemClick;
            // 
            // ribbonPageGroup3
            // 
            ribbonPageGroup3.ItemLinks.Add(btnTestConnection);
            ribbonPageGroup3.ItemLinks.Add(btnLogout);
            ribbonPageGroup3.Name = "ribbonPageGroup3";
            ribbonPageGroup3.Text = "Help";
            // 
            // btnTestConnection
            // 
            btnTestConnection.Caption = "連線測試";
            btnTestConnection.Id = 2;
            btnTestConnection.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("btnTestConnection.ImageOptions.Image");
            btnTestConnection.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("btnTestConnection.ImageOptions.LargeImage");
            btnTestConnection.Name = "btnTestConnection";
            btnTestConnection.ItemClick += btnTestConnection_ItemClick;
            // 
            // btnLogout
            // 
            btnLogout.Caption = "登出";
            btnLogout.Id = 7;
            btnLogout.ImageOptions.Image = (System.Drawing.Image)resources.GetObject("btnLogout.ImageOptions.Image");
            btnLogout.ImageOptions.LargeImage = (System.Drawing.Image)resources.GetObject("btnLogout.ImageOptions.LargeImage");
            btnLogout.Name = "btnLogout";
            // 
            // ribbonControl1
            // 
            ribbonControl1.AllowMinimizeRibbon = false;
            ribbonControl1.EmptyAreaImageOptions.ImagePadding = new System.Windows.Forms.Padding(50, 51, 50, 51);
            ribbonControl1.ExpandCollapseItem.Id = 0;
            ribbonControl1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { ribbonControl1.ExpandCollapseItem, btnAddRentTime, btnTestConnection, btnView, btnDelete, btnExport, btnSubmitToAssistant, btnLogout });
            ribbonControl1.Location = new System.Drawing.Point(0, 0);
            ribbonControl1.Margin = new System.Windows.Forms.Padding(5);
            ribbonControl1.MaxItemId = 8;
            ribbonControl1.Name = "ribbonControl1";
            ribbonControl1.OptionsMenuMinWidth = 550;
            ribbonControl1.Pages.AddRange(new DevExpress.XtraBars.Ribbon.RibbonPage[] { ribbonPage1 });
            ribbonControl1.RibbonStyle = DevExpress.XtraBars.Ribbon.RibbonControlStyle.Office2019;
            ribbonControl1.ShowApplicationButton = DevExpress.Utils.DefaultBoolean.False;
            ribbonControl1.ShowExpandCollapseButton = DevExpress.Utils.DefaultBoolean.False;
            ribbonControl1.ShowPageHeadersInFormCaption = DevExpress.Utils.DefaultBoolean.False;
            ribbonControl1.ShowPageHeadersMode = DevExpress.XtraBars.Ribbon.ShowPageHeadersMode.Hide;
            ribbonControl1.ShowToolbarCustomizeItem = false;
            ribbonControl1.Size = new System.Drawing.Size(1859, 237);
            ribbonControl1.Toolbar.ShowCustomizeItem = false;
            // 
            // barManager1
            // 
            barManager1.DockControls.Add(barDockControlTop);
            barManager1.DockControls.Add(barDockControlBottom);
            barManager1.DockControls.Add(barDockControlLeft);
            barManager1.DockControls.Add(barDockControlRight);
            barManager1.Form = this;
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = System.Windows.Forms.DockStyle.Top;
            barDockControlTop.Location = new System.Drawing.Point(0, 0);
            barDockControlTop.Manager = barManager1;
            barDockControlTop.Size = new System.Drawing.Size(1859, 0);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            barDockControlBottom.Location = new System.Drawing.Point(0, 984);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new System.Drawing.Size(1859, 0);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = System.Windows.Forms.DockStyle.Left;
            barDockControlLeft.Location = new System.Drawing.Point(0, 0);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new System.Drawing.Size(0, 984);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = System.Windows.Forms.DockStyle.Right;
            barDockControlRight.Location = new System.Drawing.Point(1859, 0);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new System.Drawing.Size(0, 984);
            // 
            // mainPanel
            // 
            mainPanel.Controls.Add(filterPanel);
            mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            mainPanel.Location = new System.Drawing.Point(0, 237);
            mainPanel.Name = "mainPanel";
            mainPanel.Size = new System.Drawing.Size(1859, 747);
            mainPanel.TabIndex = 5;
            // 
            // filterPanel
            // 
            filterPanel.Controls.Add(cmbLocationFilter);
            filterPanel.Controls.Add(labelControl1);
            filterPanel.Dock = System.Windows.Forms.DockStyle.Top;
            filterPanel.Location = new System.Drawing.Point(2, 2);
            filterPanel.Name = "filterPanel";
            filterPanel.Size = new System.Drawing.Size(1855, 79);
            filterPanel.TabIndex = 11;
            // 
            // cmbLocationFilter
            // 
            cmbLocationFilter.Location = new System.Drawing.Point(168, 30);
            cmbLocationFilter.MenuManager = ribbonControl1;
            cmbLocationFilter.Name = "cmbLocationFilter";
            cmbLocationFilter.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            cmbLocationFilter.Size = new System.Drawing.Size(225, 28);
            cmbLocationFilter.TabIndex = 1;
            cmbLocationFilter.EditValueChanged += cmbLocationFilter_EditValueChanged;
            // 
            // labelControl1
            // 
            labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            labelControl1.Appearance.Options.UseFont = true;
            labelControl1.Location = new System.Drawing.Point(76, 23);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new System.Drawing.Size(58, 34);
            labelControl1.TabIndex = 0;
            labelControl1.Text = "場地";
            // 
            // Form1
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(10F, 22F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1859, 984);
            Controls.Add(mainPanel);
            Controls.Add(ribbonControl1);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            IconOptions.LargeImage = (System.Drawing.Image)resources.GetObject("Form1.IconOptions.LargeImage");
            Margin = new System.Windows.Forms.Padding(5);
            Name = "Form1";
            Ribbon = ribbonControl1;
            Text = "RentalSystem";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)ribbonControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)mainPanel).EndInit();
            mainPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)filterPanel).EndInit();
            filterPanel.ResumeLayout(false);
            filterPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)cmbLocationFilter.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private DevExpress.XtraBars.Ribbon.RibbonPage ribbonPage1;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup1;
        private DevExpress.XtraBars.Ribbon.RibbonControl ribbonControl1;
        private DevExpress.XtraBars.BarButtonItem btnAddRentTime;
        private DevExpress.XtraBars.BarManager barManager1;
        private DevExpress.XtraBars.BarDockControl barDockControlTop;
        private DevExpress.XtraBars.BarDockControl barDockControlBottom;
        private DevExpress.XtraBars.BarDockControl barDockControlLeft;
        private DevExpress.XtraBars.BarDockControl barDockControlRight;
        private DevExpress.XtraBars.BarButtonItem btnTestConnection;
        private DevExpress.XtraEditors.PanelControl mainPanel;
        private DevExpress.XtraBars.BarButtonItem btnView;
        private DevExpress.XtraEditors.PanelControl filterPanel;
        private DevExpress.XtraEditors.ComboBoxEdit cmbLocationFilter;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraBars.BarButtonItem btnDelete;
        private DevExpress.XtraBars.BarButtonItem btnExport;
        private DevExpress.XtraBars.BarButtonItem btnSubmitToAssistant;
        private DevExpress.XtraBars.BarButtonItem btnLogout;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup2;
        private DevExpress.XtraBars.Ribbon.RibbonPageGroup ribbonPageGroup3;
    }
}

