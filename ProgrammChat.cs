using MiniMessenger.forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MiniMessenger.forms;
namespace MiniMessenger
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var serverForm = new ServerForm();
            serverForm.Show();
            Application.Run(new ClientForm());
        }
    }
}
