namespace SW_Membership_Assignment2
{
    partial class MainServer
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
            this.ServerStart = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ServerStart
            // 
            this.ServerStart.Location = new System.Drawing.Point(38, 30);
            this.ServerStart.Name = "ServerStart";
            this.ServerStart.Size = new System.Drawing.Size(97, 34);
            this.ServerStart.TabIndex = 0;
            this.ServerStart.Text = "ServerStart";
            this.ServerStart.UseVisualStyleBackColor = true;
            this.ServerStart.Click += new System.EventHandler(this.ServerStart_Click);
            // 
            // MainServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(165, 99);
            this.Controls.Add(this.ServerStart);
            this.Name = "MainServer";
            this.Text = "MainServer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainServer_FormClosed);
            this.Load += new System.EventHandler(this.MainServer_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ServerStart;

    }
}