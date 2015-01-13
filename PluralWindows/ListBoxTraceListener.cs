using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PluralWindows
{
    public class ListBoxTraceListener : TraceListener
    {
        private ListBox output;

        public ListBoxTraceListener(ListBox output)
        {
            this.Name = "Trace";
            this.output = output;
        }


        public override void Write(string message)
        {
            Action append = delegate()
            {
                output.Items.Add(string.Format("[{0}] - {1}",DateTime.Now.ToShortDateString(), message));
            };
            if (output.InvokeRequired)
            {
                output.BeginInvoke(append);
            }
            else
            {
                append();
            }

        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
    }
}
