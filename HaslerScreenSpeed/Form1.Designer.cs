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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            label1 = new Label();
            textBoxComSpeed = new TextBox();
            label2 = new Label();
            buttonComStart = new Button();
            buttonComStop = new Button();
            labelCurrentSpeed = new Label();
            textBoxSpeedX = new TextBox();
            textBoxSpeedLength = new TextBox();
            textBoxSpeedHeight = new TextBox();
            textBoxSpeedY = new TextBox();
            comboBoxComNumber = new ComboBox();
            groupBox1 = new GroupBox();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label7 = new Label();
            label8 = new Label();
            textBoxWaitingTime = new TextBox();
            label9 = new Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(33, 20);
            label1.Name = "label1";
            label1.Size = new Size(65, 15);
            label1.TabIndex = 0;
            label1.Text = "Port name:";
            // 
            // textBoxComSpeed
            // 
            textBoxComSpeed.Location = new Point(104, 46);
            textBoxComSpeed.Name = "textBoxComSpeed";
            textBoxComSpeed.Size = new Size(50, 23);
            textBoxComSpeed.TabIndex = 3;
            textBoxComSpeed.TextAlign = HorizontalAlignment.Right;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(33, 49);
            label2.Name = "label2";
            label2.Size = new Size(60, 15);
            label2.TabIndex = 2;
            label2.Text = "Baud rate:";
            // 
            // buttonComStart
            // 
            buttonComStart.Location = new Point(33, 270);
            buttonComStart.Name = "buttonComStart";
            buttonComStart.Size = new Size(91, 23);
            buttonComStart.TabIndex = 4;
            buttonComStart.Text = "START";
            buttonComStart.UseVisualStyleBackColor = true;
            buttonComStart.Click += buttonComStart_Click;
            // 
            // buttonComStop
            // 
            buttonComStop.Location = new Point(134, 270);
            buttonComStop.Name = "buttonComStop";
            buttonComStop.Size = new Size(91, 23);
            buttonComStop.TabIndex = 5;
            buttonComStop.Text = "STOP";
            buttonComStop.UseVisualStyleBackColor = true;
            buttonComStop.Click += buttonComStop_Click;
            // 
            // labelCurrentSpeed
            // 
            labelCurrentSpeed.Location = new Point(104, 306);
            labelCurrentSpeed.Name = "labelCurrentSpeed";
            labelCurrentSpeed.Size = new Size(42, 15);
            labelCurrentSpeed.TabIndex = 6;
            labelCurrentSpeed.Text = "-";
            labelCurrentSpeed.TextAlign = ContentAlignment.TopRight;
            // 
            // textBoxSpeedX
            // 
            textBoxSpeedX.Location = new Point(101, 22);
            textBoxSpeedX.Name = "textBoxSpeedX";
            textBoxSpeedX.Size = new Size(73, 23);
            textBoxSpeedX.TabIndex = 7;
            textBoxSpeedX.TextAlign = HorizontalAlignment.Right;
            // 
            // textBoxSpeedLength
            // 
            textBoxSpeedLength.Location = new Point(101, 80);
            textBoxSpeedLength.Name = "textBoxSpeedLength";
            textBoxSpeedLength.Size = new Size(73, 23);
            textBoxSpeedLength.TabIndex = 8;
            textBoxSpeedLength.TextAlign = HorizontalAlignment.Right;
            // 
            // textBoxSpeedHeight
            // 
            textBoxSpeedHeight.Location = new Point(101, 109);
            textBoxSpeedHeight.Name = "textBoxSpeedHeight";
            textBoxSpeedHeight.Size = new Size(73, 23);
            textBoxSpeedHeight.TabIndex = 9;
            textBoxSpeedHeight.TextAlign = HorizontalAlignment.Right;
            // 
            // textBoxSpeedY
            // 
            textBoxSpeedY.Location = new Point(101, 51);
            textBoxSpeedY.Name = "textBoxSpeedY";
            textBoxSpeedY.Size = new Size(73, 23);
            textBoxSpeedY.TabIndex = 10;
            textBoxSpeedY.TextAlign = HorizontalAlignment.Right;
            // 
            // comboBoxComNumber
            // 
            comboBoxComNumber.FormattingEnabled = true;
            comboBoxComNumber.Location = new Point(104, 17);
            comboBoxComNumber.Name = "comboBoxComNumber";
            comboBoxComNumber.Size = new Size(121, 23);
            comboBoxComNumber.TabIndex = 11;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label6);
            groupBox1.Controls.Add(label5);
            groupBox1.Controls.Add(label4);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(textBoxSpeedHeight);
            groupBox1.Controls.Add(textBoxSpeedY);
            groupBox1.Controls.Add(textBoxSpeedLength);
            groupBox1.Controls.Add(textBoxSpeedX);
            groupBox1.Location = new Point(33, 80);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(192, 143);
            groupBox1.TabIndex = 12;
            groupBox1.TabStop = false;
            groupBox1.Text = "Scanning Area:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(51, 112);
            label6.Name = "label6";
            label6.Size = new Size(44, 15);
            label6.TabIndex = 17;
            label6.Text = "height:";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(51, 83);
            label5.Name = "label5";
            label5.Size = new Size(44, 15);
            label5.TabIndex = 16;
            label5.Text = "length:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(79, 54);
            label4.Name = "label4";
            label4.Size = new Size(16, 15);
            label4.TabIndex = 15;
            label4.Text = "y:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(79, 25);
            label3.Name = "label3";
            label3.Size = new Size(16, 15);
            label3.TabIndex = 13;
            label3.Text = "x:";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(33, 306);
            label7.Name = "label7";
            label7.Size = new Size(70, 15);
            label7.TabIndex = 13;
            label7.Text = "Read speed:";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(144, 306);
            label8.Name = "label8";
            label8.Size = new Size(36, 15);
            label8.TabIndex = 14;
            label8.Text = "km/h";
            // 
            // textBoxWaitingTime
            // 
            textBoxWaitingTime.Location = new Point(117, 234);
            textBoxWaitingTime.Name = "textBoxWaitingTime";
            textBoxWaitingTime.Size = new Size(50, 23);
            textBoxWaitingTime.TabIndex = 16;
            textBoxWaitingTime.TextAlign = HorizontalAlignment.Right;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(33, 237);
            label9.Name = "label9";
            label9.Size = new Size(78, 15);
            label9.TabIndex = 15;
            label9.Text = "Waiting time:";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(262, 339);
            Controls.Add(textBoxWaitingTime);
            Controls.Add(label9);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(groupBox1);
            Controls.Add(comboBoxComNumber);
            Controls.Add(labelCurrentSpeed);
            Controls.Add(buttonComStop);
            Controls.Add(buttonComStart);
            Controls.Add(textBoxComSpeed);
            Controls.Add(label2);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Form1";
            Text = "Hasler Screen Speed";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox textBoxComSpeed;
        private Label label2;
        private Button buttonComStart;
        private Button buttonComStop;
        private Label labelCurrentSpeed;
        private TextBox textBoxSpeedX;
        private TextBox textBoxSpeedLength;
        private TextBox textBoxSpeedHeight;
        private TextBox textBoxSpeedY;
        private ComboBox comboBoxComNumber;
        private GroupBox groupBox1;
        private Label label4;
        private Label label3;
        private Label label6;
        private Label label5;
        private Label label7;
        private Label label8;
        private TextBox textBoxWaitingTime;
        private Label label9;
    }
}
