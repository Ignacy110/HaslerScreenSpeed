namespace HaslerScreenSpeed
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            textBoxComNumber = new TextBox();
            textBoxComSpeed = new TextBox();
            label2 = new Label();
            buttonComStart = new Button();
            buttonComStop = new Button();
            labelCurrentSpeed = new Label();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(32, 28);
            label1.Name = "label1";
            label1.Size = new Size(35, 15);
            label1.TabIndex = 0;
            label1.Text = "COM";
            // 
            // textBoxComNumber
            // 
            textBoxComNumber.Location = new Point(73, 25);
            textBoxComNumber.Name = "textBoxComNumber";
            textBoxComNumber.Size = new Size(50, 23);
            textBoxComNumber.TabIndex = 1;
            textBoxComNumber.TextAlign = HorizontalAlignment.Right;
            // 
            // textBoxComSpeed
            // 
            textBoxComSpeed.Location = new Point(73, 54);
            textBoxComSpeed.Name = "textBoxComSpeed";
            textBoxComSpeed.Size = new Size(50, 23);
            textBoxComSpeed.TabIndex = 3;
            textBoxComSpeed.Text = "115200";
            textBoxComSpeed.TextAlign = HorizontalAlignment.Right;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(28, 57);
            label2.Name = "label2";
            label2.Size = new Size(39, 15);
            label2.TabIndex = 2;
            label2.Text = "Speed";
            // 
            // buttonComStart
            // 
            buttonComStart.Location = new Point(32, 93);
            buttonComStart.Name = "buttonComStart";
            buttonComStart.Size = new Size(91, 23);
            buttonComStart.TabIndex = 4;
            buttonComStart.Text = "START";
            buttonComStart.UseVisualStyleBackColor = true;
            buttonComStart.Click += buttonComStart_Click;
            // 
            // buttonComStop
            // 
            buttonComStop.Location = new Point(129, 93);
            buttonComStop.Name = "buttonComStop";
            buttonComStop.Size = new Size(91, 23);
            buttonComStop.TabIndex = 5;
            buttonComStop.Text = "STOP";
            buttonComStop.UseVisualStyleBackColor = true;
            buttonComStop.Click += buttonComStop_Click;
            // 
            // labelCurrentSpeed
            // 
            labelCurrentSpeed.AutoSize = true;
            labelCurrentSpeed.Location = new Point(154, 57);
            labelCurrentSpeed.Name = "labelCurrentSpeed";
            labelCurrentSpeed.Size = new Size(13, 15);
            labelCurrentSpeed.TabIndex = 6;
            labelCurrentSpeed.Text = "0";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(250, 139);
            Controls.Add(labelCurrentSpeed);
            Controls.Add(buttonComStop);
            Controls.Add(buttonComStart);
            Controls.Add(textBoxComSpeed);
            Controls.Add(label2);
            Controls.Add(textBoxComNumber);
            Controls.Add(label1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBoxComNumber;
        private TextBox textBoxComSpeed;
        private Label label2;
        private Button buttonComStart;
        private Button buttonComStop;
        private Label labelCurrentSpeed;
    }
}
