namespace TTSAgent.UI.Screens
{
    partial class ExtractionResultScreen
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblHeader = new System.Windows.Forms.Label();
            this.lblCaption = new System.Windows.Forms.Label();
            this.lblFilter = new System.Windows.Forms.Label();
            this.cmbFilter = new System.Windows.Forms.ComboBox();
            this.dgvTerms = new System.Windows.Forms.DataGridView();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.btnSend = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTerms)).BeginInit();
            this.SuspendLayout();
            // lblHeader
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Location = new System.Drawing.Point(16, 12);
            this.lblHeader.Text = "Screen 3 — Kanji Review";
            this.lblHeader.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.FromArgb(30, 39, 97);
            this.lblHeader.AutoSize = true;
            // lblCaption
            this.lblCaption.Name = "lblCaption";
            this.lblCaption.Location = new System.Drawing.Point(18, 44);
            this.lblCaption.Text = "Edit Correct Hiragana where needed. Checked rows are saved to the Fixed List so they skip ChatGPT next time.";
            this.lblCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblCaption.ForeColor = System.Drawing.Color.DimGray;
            this.lblCaption.AutoSize = true;
            // lblFilter
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Location = new System.Drawing.Point(20, 84);
            this.lblFilter.Text = "Filter:";
            this.lblFilter.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Regular);
            this.lblFilter.ForeColor = System.Drawing.Color.FromArgb(35, 38, 43);
            this.lblFilter.AutoSize = true;
            // cmbFilter
            this.cmbFilter.Name = "cmbFilter";
            this.cmbFilter.Location = new System.Drawing.Point(70, 81);
            this.cmbFilter.Size = new System.Drawing.Size(130, 25);
            this.cmbFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbFilter.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            // dgvTerms
            this.dgvTerms.Name = "dgvTerms";
            this.dgvTerms.Location = new System.Drawing.Point(20, 116);
            this.dgvTerms.Size = new System.Drawing.Size(840, 360);
            this.dgvTerms.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom
                                 | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.dgvTerms.AllowUserToAddRows = false;
            this.dgvTerms.AllowUserToDeleteRows = false;
            this.dgvTerms.RowHeadersVisible = false;
            this.dgvTerms.BackgroundColor = System.Drawing.Color.White;
            this.dgvTerms.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvTerms.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvTerms.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dgvTerms.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgvTerms.Font = new System.Drawing.Font("Segoe UI", 9F);
            // btnSave
            this.btnSave.Name = "btnSave";
            this.btnSave.Location = new System.Drawing.Point(20, 488);
            this.btnSave.Size = new System.Drawing.Size(220, 34);
            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnSave.Text = "Save Checked to Fixed List";
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.BackColor = System.Drawing.Color.FromArgb(238, 243, 252);
            this.btnSave.ForeColor = System.Drawing.Color.FromArgb(30, 39, 97);
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(201, 212, 232);
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // btnBack
            this.btnBack.Name = "btnBack";
            this.btnBack.Location = new System.Drawing.Point(250, 488);
            this.btnBack.Size = new System.Drawing.Size(110, 34);
            this.btnBack.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnBack.Text = "◀ Back";
            this.btnBack.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.BackColor = System.Drawing.Color.FromArgb(238, 243, 252);
            this.btnBack.ForeColor = System.Drawing.Color.FromArgb(30, 39, 97);
            this.btnBack.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(201, 212, 232);
            this.btnBack.UseVisualStyleBackColor = false;
            this.btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // btnNext
            this.btnNext.Name = "btnNext";
            this.btnNext.Location = new System.Drawing.Point(370, 488);
            this.btnNext.Size = new System.Drawing.Size(130, 34);
            this.btnNext.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnNext.Text = "Next ▶";
            this.btnNext.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.BackColor = System.Drawing.Color.FromArgb(199, 62, 58);
            this.btnNext.ForeColor = System.Drawing.Color.White;
            this.btnNext.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(199, 62, 58);
            this.btnNext.UseVisualStyleBackColor = false;
            this.btnNext.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // btnSend
            this.btnSend.Name = "btnSend";
            this.btnSend.Location = new System.Drawing.Point(510, 488);
            this.btnSend.Size = new System.Drawing.Size(240, 34);
            this.btnSend.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.btnSend.Text = "Send Need Review to Human Review";
            this.btnSend.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.BackColor = System.Drawing.Color.FromArgb(238, 243, 252);
            this.btnSend.ForeColor = System.Drawing.Color.FromArgb(30, 39, 97);
            this.btnSend.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(201, 212, 232);
            this.btnSend.UseVisualStyleBackColor = false;
            this.btnSend.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // lblStatus
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Location = new System.Drawing.Point(20, 530);
            this.lblStatus.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            this.lblStatus.Text = "";
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.AutoSize = true;
            // ExtractionResultScreen
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(246, 248, 252);
            this.Controls.Add(this.lblHeader);
            this.Controls.Add(this.lblCaption);
            this.Controls.Add(this.lblFilter);
            this.Controls.Add(this.cmbFilter);
            this.Controls.Add(this.dgvTerms);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.btnSend);
            this.Controls.Add(this.lblStatus);
            this.Name = "ExtractionResultScreen";
            this.Size = new System.Drawing.Size(880, 600);
            ((System.ComponentModel.ISupportInitialize)(this.dgvTerms)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblHeader;
        private System.Windows.Forms.Label lblCaption;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.ComboBox cmbFilter;
        private System.Windows.Forms.DataGridView dgvTerms;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnNext;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.Label lblStatus;
    }
}
