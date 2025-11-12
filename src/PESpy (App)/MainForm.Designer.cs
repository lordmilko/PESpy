using System;
using PESpy.Controls;

namespace PESpy
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.toolStripDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitLeftAndRight = new PESpy.MySplitContainer();
            this.treeView = new PESpy.Controls.TreeViewPanel();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.splitTopAndBottom = new PESpy.MySplitContainer();
            this.textViewHost = new PESpy.Controls.TextViewHost();
            this.viewMapHost = new PESpy.Controls.ViewMapHost();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();
            this.overviewPanel = new PESpy.Controls.OverviewPanel();
            this.listViewPanel = new PESpy.Controls.ListViewPanel();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitLeftAndRight)).BeginInit();
            this.splitLeftAndRight.Panel1.SuspendLayout();
            this.splitLeftAndRight.Panel2.SuspendLayout();
            this.splitLeftAndRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitTopAndBottom)).BeginInit();
            this.splitTopAndBottom.Panel1.SuspendLayout();
            this.splitTopAndBottom.Panel2.SuspendLayout();
            this.splitTopAndBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.BackColor = System.Drawing.SystemColors.Control;
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDropDownButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.toolStrip.Size = new System.Drawing.Size(1315, 31);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip";
            // 
            // toolStripDropDownButton
            // 
            this.toolStripDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem});
            this.toolStripDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton.Name = "toolStripDropDownButton";
            this.toolStripDropDownButton.ShowDropDownArrow = false;
            this.toolStripDropDownButton.Size = new System.Drawing.Size(36, 24);
            this.toolStripDropDownButton.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            this.openToolStripMenuItem.Text = "&Open...";
            // 
            // splitLeftAndRight
            // 
            this.splitLeftAndRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitLeftAndRight.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitLeftAndRight.Location = new System.Drawing.Point(0, 31);
            this.splitLeftAndRight.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitLeftAndRight.Name = "splitLeftAndRight";
            // 
            // splitLeftAndRight.Panel1
            // 
            this.splitLeftAndRight.Panel1.Controls.Add(this.treeView);
            // 
            // splitLeftAndRight.Panel2
            // 
            this.splitLeftAndRight.Panel2.Controls.Add(this.splitTopAndBottom);
            this.splitLeftAndRight.Size = new System.Drawing.Size(1315, 616);
            this.splitLeftAndRight.SplitterDistance = 301;
            this.splitLeftAndRight.TabIndex = 1;
            this.splitLeftAndRight.TabStop = false;
            // 
            // treeView
            // 
            this.treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView.HideSelection = false;
            this.treeView.ImageIndex = 0;
            this.treeView.ImageList = this.imageList;
            this.treeView.Location = new System.Drawing.Point(0, 0);
            this.treeView.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.treeView.Name = "treeView";
            this.treeView.SelectedImageIndex = 0;
            this.treeView.ShowRootLines = false;
            this.treeView.Size = new System.Drawing.Size(301, 616);
            this.treeView.TabIndex = 0;
            this.treeView.AfterSelect += mainTreeView_AfterSelect;
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "box.png");
            this.imageList.Images.SetKeyName(1, "database.png");
            this.imageList.Images.SetKeyName(2, "block.png");
            this.imageList.Images.SetKeyName(3, "documents-stack.png");
            this.imageList.Images.SetKeyName(4, "StructurePublic.16.16.png");
            this.imageList.Images.SetKeyName(5, "folder-struct.png");
            this.imageList.Images.SetKeyName(6, "document-binary.png");
            this.imageList.Images.SetKeyName(7, "edit-alignment.png");
            this.imageList.Images.SetKeyName(8, "money-coin.png");
            this.imageList.Images.SetKeyName(9, "folders.png");
            this.imageList.Images.SetKeyName(10, "struct-stack.png");
            this.imageList.Images.SetKeyName(11, "layers-stack.png");
            this.imageList.Images.SetKeyName(12, "struct-named.png");
            this.imageList.Images.SetKeyName(13, "MethodPublic.16.16.png");
            this.imageList.Images.SetKeyName(14, "FieldPublic.16.16.png");
            // 
            // splitTopAndBottom
            // 
            this.splitTopAndBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitTopAndBottom.Location = new System.Drawing.Point(0, 0);
            this.splitTopAndBottom.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.splitTopAndBottom.Name = "splitTopAndBottom";
            this.splitTopAndBottom.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitTopAndBottom.Panel1
            // 
            this.splitTopAndBottom.Panel1.AutoScroll = true;
            this.splitTopAndBottom.Panel1.Controls.Add(this.textViewHost);
            this.splitTopAndBottom.Panel1.Controls.Add(this.viewMapHost);
            this.splitTopAndBottom.Panel1.Controls.Add(this.progressBar);
            this.splitTopAndBottom.Panel1.Controls.Add(this.lblProgress);
            // 
            // splitTopAndBottom.Panel2
            // 
            this.splitTopAndBottom.Panel2.Controls.Add(this.overviewPanel);
            this.splitTopAndBottom.Panel2.Controls.Add(this.listViewPanel);
            this.splitTopAndBottom.Size = new System.Drawing.Size(1010, 616);
            this.splitTopAndBottom.SplitterDistance = 349;
            this.splitTopAndBottom.SplitterWidth = 3;
            this.splitTopAndBottom.TabIndex = 0;
            this.splitTopAndBottom.TabStop = false;
            // 
            // textViewHost
            // 
            this.textViewHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textViewHost.Location = new System.Drawing.Point(0, 38);
            this.textViewHost.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textViewHost.Name = "textViewHost";
            this.textViewHost.Size = new System.Drawing.Size(1010, 311);
            this.textViewHost.TabIndex = 3;
            this.textViewHost.Visible = false;
            // 
            // viewMapHost
            // 
            this.viewMapHost.Dock = System.Windows.Forms.DockStyle.Top;
            this.viewMapHost.Location = new System.Drawing.Point(0, 0);
            this.viewMapHost.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.viewMapHost.Name = "viewMapHost";
            this.viewMapHost.Size = new System.Drawing.Size(1010, 38);
            this.viewMapHost.TabIndex = 2;
            this.viewMapHost.Visible = false;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(227, 129);
            this.progressBar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(519, 15);
            this.progressBar.TabIndex = 1;
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(454, 110);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(44, 16);
            this.lblProgress.TabIndex = 0;
            this.lblProgress.Text = "label1";
            // 
            // overviewPanel
            // 
            this.overviewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.overviewPanel.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.overviewPanel.HideSelection = false;
            this.overviewPanel.Location = new System.Drawing.Point(0, 0);
            this.overviewPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.overviewPanel.Name = "overviewPanel";
            this.overviewPanel.OwnerDraw = true;
            this.overviewPanel.Size = new System.Drawing.Size(1010, 264);
            this.overviewPanel.TabIndex = 2;
            this.overviewPanel.UseCompatibleStateImageBehavior = false;
            this.overviewPanel.View = System.Windows.Forms.View.Details;
            this.overviewPanel.Visible = false;
            // 
            // listViewPanel
            // 
            this.listViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewPanel.Location = new System.Drawing.Point(0, 0);
            this.listViewPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.listViewPanel.Name = "listViewPanel";
            this.listViewPanel.Size = new System.Drawing.Size(1010, 264);
            this.listViewPanel.TabIndex = 0;
            this.listViewPanel.Visible = false;
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "openFileDialog1";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1315, 647);
            this.Controls.Add(this.splitLeftAndRight);
            this.Controls.Add(this.toolStrip);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PESpy";
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.splitLeftAndRight.Panel1.ResumeLayout(false);
            this.splitLeftAndRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitLeftAndRight)).EndInit();
            this.splitLeftAndRight.ResumeLayout(false);
            this.splitTopAndBottom.Panel1.ResumeLayout(false);
            this.splitTopAndBottom.Panel1.PerformLayout();
            this.splitTopAndBottom.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitTopAndBottom)).EndInit();
            this.splitTopAndBottom.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private PESpy.MySplitContainer splitLeftAndRight;
        private PESpy.MySplitContainer splitTopAndBottom;
        private PESpy.Controls.TreeViewPanel treeView;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblProgress;
        private OverviewPanel overviewPanel;
        private ListViewPanel listViewPanel;
        private ViewMapHost viewMapHost;
        private TextViewHost textViewHost;
    }
}
