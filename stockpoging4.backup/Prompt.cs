using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace stockpoging4
{
    public class Prompt : IDisposable
    {
        private Form prompt { get; set; }
        public string Result { get; }

        public Prompt(string text, string caption)
        {
            Result = ShowDialog(text, caption);
        }
        //use a using statement
        private string ShowDialog(string text, string caption)
        {
            prompt = new Form()
            {
                Width = 200,
                Height = 120,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                MinimizeBox = false,
                MaximizeBox = false,
            };
            Label textLabel = new Label() { Left = 50, Top = 20, Text = text, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter };
            TextBox textBox = new TextBox() { Left = 30, Top = 30, Width = 100 };
            Button confirmation = new Button() { Text = "Ok", Left = 30, Width = 100, Top = 50, DialogResult = DialogResult.OK, ImageAlign = ContentAlignment.BottomRight };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        public void Dispose()
        {
            //See Marcus comment
            if (prompt != null)
            {
                prompt.Dispose();
            }
        }
    }
}
