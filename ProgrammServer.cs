

using MiniMessenger.forms;

namespace MiniMessenger
{
    internal class ProgrammServer
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ServerForm());
        }
    }
}
