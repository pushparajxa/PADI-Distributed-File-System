namespace MasterComponent {
    partial class Puppeteer {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.LoadButton = new System.Windows.Forms.Button();
            this.FileNameTextBox = new System.Windows.Forms.TextBox();
            this.RunButton = new System.Windows.Forms.Button();
            this.NextButton = new System.Windows.Forms.Button();
            this.outputBox = new System.Windows.Forms.TextBox();
            this.ClientButton = new System.Windows.Forms.Button();
            this.DataButton = new System.Windows.Forms.Button();
            this.MetaButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // LoadButton
            // 
            this.LoadButton.Location = new System.Drawing.Point(196, 12);
            this.LoadButton.Name = "LoadButton";
            this.LoadButton.Size = new System.Drawing.Size(40, 23);
            this.LoadButton.TabIndex = 0;
            this.LoadButton.Text = "Load";
            this.LoadButton.UseVisualStyleBackColor = true;
            this.LoadButton.Click += new System.EventHandler(this.LoadButton_Click);
            // 
            // FileNameTextBox
            // 
            this.FileNameTextBox.Location = new System.Drawing.Point(12, 12);
            this.FileNameTextBox.Name = "FileNameTextBox";
            this.FileNameTextBox.Size = new System.Drawing.Size(178, 20);
            this.FileNameTextBox.TabIndex = 1;
            // 
            // RunButton
            // 
            this.RunButton.Location = new System.Drawing.Point(243, 12);
            this.RunButton.Name = "RunButton";
            this.RunButton.Size = new System.Drawing.Size(40, 23);
            this.RunButton.TabIndex = 2;
            this.RunButton.Text = "Run";
            this.RunButton.UseVisualStyleBackColor = true;
            this.RunButton.Click += new System.EventHandler(this.RunButton_Click);
            // 
            // NextButton
            // 
            this.NextButton.Location = new System.Drawing.Point(289, 12);
            this.NextButton.Name = "NextButton";
            this.NextButton.Size = new System.Drawing.Size(40, 23);
            this.NextButton.TabIndex = 3;
            this.NextButton.Text = "Next";
            this.NextButton.UseVisualStyleBackColor = true;
            this.NextButton.Click += new System.EventHandler(this.NextButton_Click);
            // 
            // outputBox
            // 
            this.outputBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.outputBox.Location = new System.Drawing.Point(0, 50);
            this.outputBox.Multiline = true;
            this.outputBox.Name = "outputBox";
            this.outputBox.ReadOnly = true;
            this.outputBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.outputBox.Size = new System.Drawing.Size(572, 339);
            this.outputBox.TabIndex = 4;
            // 
            // ClientButton
            // 
            this.ClientButton.Location = new System.Drawing.Point(537, 12);
            this.ClientButton.Name = "ClientButton";
            this.ClientButton.Size = new System.Drawing.Size(23, 23);
            this.ClientButton.TabIndex = 5;
            this.ClientButton.Text = "C";
            this.ClientButton.UseVisualStyleBackColor = true;
            this.ClientButton.Click += new System.EventHandler(this.ClientButton_Click);
            // 
            // DataButton
            // 
            this.DataButton.Location = new System.Drawing.Point(508, 12);
            this.DataButton.Name = "DataButton";
            this.DataButton.Size = new System.Drawing.Size(23, 23);
            this.DataButton.TabIndex = 6;
            this.DataButton.Text = "D";
            this.DataButton.UseVisualStyleBackColor = true;
            this.DataButton.Click += new System.EventHandler(this.DataButton_Click);
            // 
            // MetaButton
            // 
            this.MetaButton.Location = new System.Drawing.Point(479, 12);
            this.MetaButton.Name = "MetaButton";
            this.MetaButton.Size = new System.Drawing.Size(23, 23);
            this.MetaButton.TabIndex = 7;
            this.MetaButton.Text = "M";
            this.MetaButton.UseVisualStyleBackColor = true;
            this.MetaButton.Click += new System.EventHandler(this.MetaButton_Click);
            // 
            // Puppeteer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(572, 389);
            this.Controls.Add(this.MetaButton);
            this.Controls.Add(this.DataButton);
            this.Controls.Add(this.ClientButton);
            this.Controls.Add(this.outputBox);
            this.Controls.Add(this.NextButton);
            this.Controls.Add(this.RunButton);
            this.Controls.Add(this.FileNameTextBox);
            this.Controls.Add(this.LoadButton);
            this.Name = "Puppeteer";
            this.Text = "Puppet Master";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button LoadButton;
        private System.Windows.Forms.TextBox FileNameTextBox;
        private System.Windows.Forms.Button RunButton;
        private System.Windows.Forms.Button NextButton;
        private System.Windows.Forms.TextBox outputBox;
        private System.Windows.Forms.Button ClientButton;
        private System.Windows.Forms.Button DataButton;
        private System.Windows.Forms.Button MetaButton;
    }
}

