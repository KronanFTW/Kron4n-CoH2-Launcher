using System;
using System.Windows.Forms;

namespace Kron4n_CoH2_Launcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}