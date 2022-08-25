namespace BrotecsLateSMSReporting
{
    partial class SMSReporting
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SMSReporting));
            this.label_Main = new System.Windows.Forms.Label();
            this.toolStripMenuItem_Option = new System.Windows.Forms.ToolStripMenuItem();
            this.configureSMSPortToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sMSLogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listView1 = new System.Windows.Forms.ListView();
            this.id = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.phoneNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.message = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.sentTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.button_Refresh = new System.Windows.Forms.Button();
            this.textBox_log = new System.Windows.Forms.TextBox();
            this.balance = new System.Windows.Forms.Button();
            this.pictureBoxDataReceive = new System.Windows.Forms.PictureBox();
            this.label_Operator = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDataReceive)).BeginInit();
            this.SuspendLayout();
            // 
            // label_Main
            // 
            this.label_Main.AutoSize = true;
            this.label_Main.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label_Main.Location = new System.Drawing.Point(406, 358);
            this.label_Main.Name = "label_Main";
            this.label_Main.Size = new System.Drawing.Size(210, 16);
            this.label_Main.TabIndex = 0;
            this.label_Main.Text = "Brotecs Late SMS Reporting Running...";
            // 
            // toolStripMenuItem_Option
            // 
            this.toolStripMenuItem_Option.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.configureSMSPortToolStripMenuItem,
            this.sMSLogToolStripMenuItem});
            this.toolStripMenuItem_Option.Name = "toolStripMenuItem_Option";
            this.toolStripMenuItem_Option.Size = new System.Drawing.Size(56, 20);
            this.toolStripMenuItem_Option.Text = "Option";
            // 
            // configureSMSPortToolStripMenuItem
            // 
            this.configureSMSPortToolStripMenuItem.Name = "configureSMSPortToolStripMenuItem";
            this.configureSMSPortToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.configureSMSPortToolStripMenuItem.Text = "Configure SMS Port";
            this.configureSMSPortToolStripMenuItem.Click += new System.EventHandler(this.configureSMSPortToolStripMenuItem_Click);
            // 
            // sMSLogToolStripMenuItem
            // 
            this.sMSLogToolStripMenuItem.Name = "sMSLogToolStripMenuItem";
            this.sMSLogToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.sMSLogToolStripMenuItem.Text = "SMS Log";
            this.sMSLogToolStripMenuItem.Click += new System.EventHandler(this.sMSLogToolStripMenuItem_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Visible;
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(10, 10);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem_Option,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(634, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // listView1
            // 
            this.listView1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView1.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.listView1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.id,
            this.name,
            this.phoneNumber,
            this.message,
            this.sentTime});
            this.listView1.GridLines = true;
            this.listView1.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(18, 169);
            this.listView1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(598, 185);
            this.listView1.TabIndex = 5;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            // 
            // id
            // 
            this.id.Text = "ID";
            this.id.Width = 40;
            // 
            // name
            // 
            this.name.Text = "Name";
            this.name.Width = 170;
            // 
            // phoneNumber
            // 
            this.phoneNumber.Text = "Phone Number";
            this.phoneNumber.Width = 0;
            // 
            // message
            // 
            this.message.Text = "Message";
            this.message.Width = 114;
            // 
            // sentTime
            // 
            this.sentTime.Text = "Sent time";
            this.sentTime.Width = 275;
            // 
            // button_Refresh
            // 
            this.button_Refresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button_Refresh.Location = new System.Drawing.Point(111, 133);
            this.button_Refresh.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button_Refresh.Name = "button_Refresh";
            this.button_Refresh.Size = new System.Drawing.Size(87, 28);
            this.button_Refresh.TabIndex = 6;
            this.button_Refresh.Text = "Refresh";
            this.button_Refresh.UseVisualStyleBackColor = true;
            this.button_Refresh.Click += new System.EventHandler(this.button_Refresh_Click);
            // 
            // textBox_log
            // 
            this.textBox_log.BackColor = System.Drawing.SystemColors.ControlDark;
            this.textBox_log.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_log.Cursor = System.Windows.Forms.Cursors.Default;
            this.textBox_log.Font = new System.Drawing.Font("Arial", 6F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBox_log.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this.textBox_log.Location = new System.Drawing.Point(204, 28);
            this.textBox_log.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBox_log.Multiline = true;
            this.textBox_log.Name = "textBox_log";
            this.textBox_log.ReadOnly = true;
            this.textBox_log.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_log.ShortcutsEnabled = false;
            this.textBox_log.Size = new System.Drawing.Size(412, 133);
            this.textBox_log.TabIndex = 0;
            this.textBox_log.TabStop = false;
            // 
            // balance
            // 
            this.balance.Cursor = System.Windows.Forms.Cursors.WaitCursor;
            this.balance.Location = new System.Drawing.Point(18, 133);
            this.balance.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.balance.Name = "balance";
            this.balance.Size = new System.Drawing.Size(87, 28);
            this.balance.TabIndex = 7;
            this.balance.Text = "Balance";
            this.balance.UseVisualStyleBackColor = true;
            this.balance.UseWaitCursor = true;
            this.balance.Click += new System.EventHandler(this.balance_Click);
            // 
            // pictureBoxDataReceive
            // 
            this.pictureBoxDataReceive.BackColor = System.Drawing.Color.Transparent;
            this.pictureBoxDataReceive.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.pictureBoxDataReceive.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxDataReceive.Location = new System.Drawing.Point(23, 53);
            this.pictureBoxDataReceive.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBoxDataReceive.Name = "pictureBoxDataReceive";
            this.pictureBoxDataReceive.Size = new System.Drawing.Size(10, 10);
            this.pictureBoxDataReceive.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBoxDataReceive.TabIndex = 10;
            this.pictureBoxDataReceive.TabStop = false;
            this.pictureBoxDataReceive.WaitOnLoad = true;
            // 
            // label_Operator
            // 
            this.label_Operator.AutoSize = true;
            this.label_Operator.Location = new System.Drawing.Point(39, 53);
            this.label_Operator.Name = "label_Operator";
            this.label_Operator.Size = new System.Drawing.Size(17, 16);
            this.label_Operator.TabIndex = 9;
            this.label_Operator.Text = "...";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft YaHei", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(17, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 19);
            this.label1.TabIndex = 8;
            this.label1.Text = "Operator:";
            // 
            // SMSReporting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(634, 380);
            this.Controls.Add(this.pictureBoxDataReceive);
            this.Controls.Add(this.label_Operator);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.balance);
            this.Controls.Add(this.textBox_log);
            this.Controls.Add(this.button_Refresh);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.label_Main);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Microsoft YaHei", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.Desktop;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.Name = "SMSReporting";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Brotecs SMS Reporting";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.main_FormClosed);
            this.Load += new System.EventHandler(this.main_Load);
            this.TextChanged += new System.EventHandler(this.SMSReporting_TextChanged);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxDataReceive)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label_Main;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Option;
        private System.Windows.Forms.ToolStripMenuItem configureSMSPortToolStripMenuItem;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ColumnHeader id;
        private System.Windows.Forms.ColumnHeader name;
        private System.Windows.Forms.ColumnHeader phoneNumber;
        private System.Windows.Forms.ColumnHeader message;
        private System.Windows.Forms.ColumnHeader sentTime;
        private System.Windows.Forms.ToolStripMenuItem sMSLogToolStripMenuItem;
        private System.Windows.Forms.Button button_Refresh;
        private System.Windows.Forms.TextBox textBox_log;
        private System.Windows.Forms.Button balance;
        private System.Windows.Forms.PictureBox pictureBoxDataReceive;
        private System.Windows.Forms.Label label_Operator;
        private System.Windows.Forms.Label label1;
    }
}

