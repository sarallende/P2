#nullable disable
using System;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Punto de entrada de la aplicacion de ventanas.
    ///
    /// Se usa como StartupObject en el .csproj para arrancar directamente la
    /// interfaz grafica. De este modo el archivo original Program.cs no se
    /// modifica: su Main sigue existiendo pero no se ejecuta.
    /// </summary>
    internal static class AppLauncher
    {
        [STAThread]
        private static void Main()
        {
            // Configuracion recomendada de WinForms en .NET 6 (DPI, estilos).
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new MainForm());
        }
    }
}
