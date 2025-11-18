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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            toolStrip = new System.Windows.Forms.ToolStrip();
            toolStripDropDownButton = new System.Windows.Forms.ToolStripDropDownButton();
            openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            goToToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            splitLeftAndRight = new MySplitContainer();
            treeView = new TreeViewPanel();
            imageList = new System.Windows.Forms.ImageList(components);
            splitTopAndBottom = new MySplitContainer();
            textViewHost = new TextViewHost();
            viewMapHost = new ViewMapHost();
            progressBar = new System.Windows.Forms.ProgressBar();
            lblProgress = new System.Windows.Forms.Label();
            overviewPanel = new OverviewPanel();
            listViewPanel = new ListViewPanel();
            openFileDialog = new System.Windows.Forms.OpenFileDialog();
            toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitLeftAndRight).BeginInit();
            splitLeftAndRight.Panel1.SuspendLayout();
            splitLeftAndRight.Panel2.SuspendLayout();
            splitLeftAndRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize) splitTopAndBottom).BeginInit();
            splitTopAndBottom.Panel1.SuspendLayout();
            splitTopAndBottom.Panel2.SuspendLayout();
            splitTopAndBottom.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.BackColor = System.Drawing.SystemColors.Control;
            toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            toolStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripDropDownButton, toolStripDropDownButton1, toolStripSeparator1, toolStripButton1 });
            toolStrip.Location = new System.Drawing.Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
            toolStrip.Size = new System.Drawing.Size(1315, 31);
            toolStrip.TabIndex = 0;
            toolStrip.Text = "toolStrip";
            // 
            // toolStripDropDownButton
            // 
            toolStripDropDownButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            toolStripDropDownButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { openToolStripMenuItem });
            toolStripDropDownButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton.Name = "toolStripDropDownButton";
            toolStripDropDownButton.ShowDropDownArrow = false;
            toolStripDropDownButton.Size = new System.Drawing.Size(36, 24);
            toolStripDropDownButton.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys =  System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O;
            openToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            openToolStripMenuItem.Text = "&Open...";
            // 
            // toolStripDropDownButton1
            // 
            toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { goToToolStripMenuItem });
            toolStripDropDownButton1.Image = (System.Drawing.Image) resources.GetObject("toolStripDropDownButton1.Image");
            toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            toolStripDropDownButton1.ShowDropDownArrow = false;
            toolStripDropDownButton1.Size = new System.Drawing.Size(57, 24);
            toolStripDropDownButton1.Text = "&Search";
            // 
            // goToToolStripMenuItem
            // 
            goToToolStripMenuItem.Image = Resource.rocket_fly;
            goToToolStripMenuItem.Name = "goToToolStripMenuItem";
            goToToolStripMenuItem.ShortcutKeys =  System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G;
            goToToolStripMenuItem.Size = new System.Drawing.Size(190, 26);
            goToToolStripMenuItem.Text = "Go to...";
            goToToolStripMenuItem.Click += GoTo_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // toolStripButton1
            // 
            toolStripButton1.AutoSize = false;
            toolStripButton1.Image = Resource.rocket_fly;
            toolStripButton1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            toolStripButton1.Name = "toolStripButton1";
            toolStripButton1.Size = new System.Drawing.Size(92, 24);
            toolStripButton1.Text = " Go To";
            toolStripButton1.ToolTipText = "Go To (Ctrl+G)";
            toolStripButton1.Click += GoTo_Click;
            // 
            // splitLeftAndRight
            // 
            splitLeftAndRight.Dock = System.Windows.Forms.DockStyle.Fill;
            splitLeftAndRight.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            splitLeftAndRight.Location = new System.Drawing.Point(0, 31);
            splitLeftAndRight.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            splitLeftAndRight.Name = "splitLeftAndRight";
            // 
            // splitLeftAndRight.Panel1
            // 
            splitLeftAndRight.Panel1.Controls.Add(treeView);
            // 
            // splitLeftAndRight.Panel2
            // 
            splitLeftAndRight.Panel2.Controls.Add(splitTopAndBottom);
            splitLeftAndRight.Size = new System.Drawing.Size(1315, 778);
            splitLeftAndRight.SplitterDistance = 301;
            splitLeftAndRight.TabIndex = 1;
            splitLeftAndRight.TabStop = false;
            // 
            // treeView
            // 
            treeView.Dock = System.Windows.Forms.DockStyle.Fill;
            treeView.HideSelection = false;
            treeView.ImageIndex = 0;
            treeView.ImageList = imageList;
            treeView.Location = new System.Drawing.Point(0, 0);
            treeView.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            treeView.Name = "treeView";
            treeView.SelectedImageIndex = 0;
            treeView.ShowRootLines = false;
            treeView.Size = new System.Drawing.Size(301, 778);
            treeView.TabIndex = 0;
            treeView.AfterSelect += mainTreeView_AfterSelect;
            // 
            // imageList
            // 
            imageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            imageList.ImageStream = (System.Windows.Forms.ImageListStreamer) resources.GetObject("imageList.ImageStream");
            imageList.TransparentColor = System.Drawing.Color.Transparent;
            imageList.Images.SetKeyName(0, "box.png");
            imageList.Images.SetKeyName(1, "database.png");
            imageList.Images.SetKeyName(2, "block.png");
            imageList.Images.SetKeyName(3, "documents-stack.png");
            imageList.Images.SetKeyName(4, "StructurePublic.16.16.png");
            imageList.Images.SetKeyName(5, "folder-struct.png");
            imageList.Images.SetKeyName(6, "document-binary.png");
            imageList.Images.SetKeyName(7, "edit-alignment.png");
            imageList.Images.SetKeyName(8, "money-coin.png");
            imageList.Images.SetKeyName(9, "folders.png");
            imageList.Images.SetKeyName(10, "struct-stack.png");
            imageList.Images.SetKeyName(11, "layers-stack.png");
            imageList.Images.SetKeyName(12, "struct-named.png");
            imageList.Images.SetKeyName(13, "MethodPublic.16.16.png");
            imageList.Images.SetKeyName(14, "FieldPublic.16.16.png");
            imageList.Images.SetKeyName(15, "rocket-fly.png");
            imageList.Images.SetKeyName(16, "magnifier.png");
            imageList.Images.SetKeyName(17, "edit-style.png");
            imageList.Images.SetKeyName(18, "script-text.png");
            // 
            // splitTopAndBottom
            // 
            splitTopAndBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            splitTopAndBottom.Location = new System.Drawing.Point(0, 0);
            splitTopAndBottom.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            splitTopAndBottom.Name = "splitTopAndBottom";
            splitTopAndBottom.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitTopAndBottom.Panel1
            // 
            splitTopAndBottom.Panel1.AutoScroll = true;
            splitTopAndBottom.Panel1.Controls.Add(textViewHost);
            splitTopAndBottom.Panel1.Controls.Add(viewMapHost);
            splitTopAndBottom.Panel1.Controls.Add(progressBar);
            splitTopAndBottom.Panel1.Controls.Add(lblProgress);
            // 
            // splitTopAndBottom.Panel2
            // 
            splitTopAndBottom.Panel2.Controls.Add(overviewPanel);
            splitTopAndBottom.Panel2.Controls.Add(listViewPanel);
            splitTopAndBottom.Size = new System.Drawing.Size(1010, 778);
            splitTopAndBottom.SplitterDistance = 440;
            splitTopAndBottom.TabIndex = 0;
            splitTopAndBottom.TabStop = false;
            // 
            // textViewHost
            // 
            textViewHost.Dock = System.Windows.Forms.DockStyle.Fill;
            textViewHost.Location = new System.Drawing.Point(0, 48);
            textViewHost.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            textViewHost.Name = "textViewHost";
            textViewHost.Size = new System.Drawing.Size(1010, 392);
            textViewHost.TabIndex = 3;
            textViewHost.Visible = false;
            // 
            // viewMapHost
            // 
            viewMapHost.Dock = System.Windows.Forms.DockStyle.Top;
            viewMapHost.Location = new System.Drawing.Point(0, 0);
            viewMapHost.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            viewMapHost.Name = "viewMapHost";
            viewMapHost.Size = new System.Drawing.Size(1010, 48);
            viewMapHost.TabIndex = 2;
            viewMapHost.Visible = false;
            // 
            // progressBar
            // 
            progressBar.Location = new System.Drawing.Point(227, 161);
            progressBar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(519, 19);
            progressBar.TabIndex = 1;
            // 
            // lblProgress
            // 
            lblProgress.AutoSize = true;
            lblProgress.Location = new System.Drawing.Point(454, 138);
            lblProgress.Name = "lblProgress";
            lblProgress.Size = new System.Drawing.Size(50, 20);
            lblProgress.TabIndex = 0;
            lblProgress.Text = "label1";
            // 
            // overviewPanel
            // 
            overviewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            overviewPanel.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            overviewPanel.Location = new System.Drawing.Point(0, 0);
            overviewPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            overviewPanel.Name = "overviewPanel";
            overviewPanel.OwnerDraw = true;
            overviewPanel.Size = new System.Drawing.Size(1010, 334);
            overviewPanel.TabIndex = 2;
            overviewPanel.UseCompatibleStateImageBehavior = false;
            overviewPanel.View = System.Windows.Forms.View.Details;
            overviewPanel.Visible = false;
            // 
            // listViewPanel
            // 
            listViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            listViewPanel.Location = new System.Drawing.Point(0, 0);
            listViewPanel.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            listViewPanel.Name = "listViewPanel";
            listViewPanel.Size = new System.Drawing.Size(1010, 334);
            listViewPanel.TabIndex = 0;
            listViewPanel.Visible = false;
            // 
            // openFileDialog
            // 
            openFileDialog.FileName = "openFileDialog1";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1315, 809);
            Controls.Add(splitLeftAndRight);
            Controls.Add(toolStrip);
            Icon = (System.Drawing.Icon) resources.GetObject("$this.Icon");
            Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "PESpy";
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            splitLeftAndRight.Panel1.ResumeLayout(false);
            splitLeftAndRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitLeftAndRight).EndInit();
            splitLeftAndRight.ResumeLayout(false);
            splitTopAndBottom.Panel1.ResumeLayout(false);
            splitTopAndBottom.Panel1.PerformLayout();
            splitTopAndBottom.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize) splitTopAndBottom).EndInit();
            splitTopAndBottom.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

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
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem goToToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
    }
}
