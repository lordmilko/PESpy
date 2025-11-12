namespace PESpy.Controls
{
    partial class ListViewPanel
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
            treeListView = new TreeListView();
            topPanel = new System.Windows.Forms.Panel();
            btnImage = new System.Windows.Forms.Button();
            lblOrigin = new System.Windows.Forms.LinkLabel();
            topPanel.SuspendLayout();
            SuspendLayout();
            // 
            // treeListView
            // 
            treeListView.Dock = System.Windows.Forms.DockStyle.Fill;
            treeListView.FullRowSelect = true;
            treeListView.GridLines = true;
            treeListView.Location = new System.Drawing.Point(0, 21);
            treeListView.Name = "treeListView";
            treeListView.OwnerDraw = true;
            treeListView.Size = new System.Drawing.Size(809, 328);
            treeListView.TabIndex = 3;
            treeListView.UseCompatibleStateImageBehavior = false;
            treeListView.View = System.Windows.Forms.View.Details;
            treeListView.BeforeExpand += treeListView_BeforeExpand;
            // 
            // topPanel
            // 
            topPanel.Controls.Add(btnImage);
            topPanel.Controls.Add(lblOrigin);
            topPanel.Dock = System.Windows.Forms.DockStyle.Top;
            topPanel.Location = new System.Drawing.Point(0, 0);
            topPanel.Name = "topPanel";
            topPanel.Size = new System.Drawing.Size(809, 21);
            topPanel.TabIndex = 4;
            // 
            // btnImage
            // 
            btnImage.FlatAppearance.BorderSize = 0;
            btnImage.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.Control;
            btnImage.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.Control;
            btnImage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            btnImage.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            btnImage.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btnImage.ImageKey = "StructurePublic.16.16.png";
            btnImage.Location = new System.Drawing.Point(-2, -6);
            btnImage.Name = "btnImage";
            btnImage.Size = new System.Drawing.Size(180, 27);
            btnImage.TabIndex = 3;
            btnImage.Text = "IMAGE_DOS_HEADER";
            btnImage.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            btnImage.UseVisualStyleBackColor = true;
            // 
            // lblOrigin
            // 
            lblOrigin.AutoSize = true;
            lblOrigin.Dock = System.Windows.Forms.DockStyle.Right;
            lblOrigin.Font = new System.Drawing.Font("Segoe UI", 7F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            lblOrigin.LinkArea = new System.Windows.Forms.LinkArea(8, 15);
            lblOrigin.Location = new System.Drawing.Point(719, 0);
            lblOrigin.Name = "lblOrigin";
            lblOrigin.Padding = new System.Windows.Forms.Padding(0, 0, 10, 0);
            lblOrigin.Size = new System.Drawing.Size(90, 20);
            lblOrigin.TabIndex = 2;
            lblOrigin.TabStop = true;
            lblOrigin.Text = "origin: winnt.h";
            lblOrigin.UseCompatibleTextRendering = true;
            // 
            // ListViewPanel
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(treeListView);
            Controls.Add(topPanel);
            Name = "ListViewPanel";
            Size = new System.Drawing.Size(809, 349);
            topPanel.ResumeLayout(false);
            topPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TreeListView treeListView;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Button btnImage;
        private System.Windows.Forms.LinkLabel lblOrigin;
    }
}
