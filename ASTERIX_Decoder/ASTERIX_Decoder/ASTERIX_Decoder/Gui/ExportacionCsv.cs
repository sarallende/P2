#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Ayudas de exportacion a CSV. SIEMPRE delega en los metodos ExportarCSV
    /// existentes de DecoderCAT048 / DecoderCAT021, que ya aplican el formato
    /// requerido (separador ';', decimales con coma, un mensaje por fila y orden
    /// original). No se implementa ningun generador de CSV alternativo.
    /// </summary>
    internal static class ExportacionCsv
    {
        public static void GuardarCat048(
            IWin32Window owner,
            IEnumerable<MensajeCAT048> mensajes,
            FiltroCAT048 filtro,
            string nombreSugerido)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = nombreSugerido
            };
            if (dlg.ShowDialog(owner) != DialogResult.OK) return;

            try
            {
                DecoderCAT048.ExportarCSV(mensajes, dlg.FileName, filtro ?? new FiltroCAT048());
                Aviso(owner, "CSV exportado:\n" + dlg.FileName);
            }
            catch (Exception ex)
            {
                Error(owner, ex);
            }
        }

        public static void GuardarCat021(
            IWin32Window owner,
            IEnumerable<MensajeCAT021> mensajes,
            FiltroCAT021 filtro,
            string nombreSugerido)
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = nombreSugerido
            };
            if (dlg.ShowDialog(owner) != DialogResult.OK) return;

            try
            {
                DecoderCAT021.ExportarCSV(mensajes, dlg.FileName, filtro ?? new FiltroCAT021());
                Aviso(owner, "CSV exportado:\n" + dlg.FileName);
            }
            catch (Exception ex)
            {
                Error(owner, ex);
            }
        }

        /// <summary>Exporta ambas categorias en una carpeta (dos ficheros CSV).</summary>
        public static void GuardarAmbas(IWin32Window owner, DatosAsterix datos, bool filtrados)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Carpeta donde guardar los CSV de ambas categorias"
            };
            if (dlg.ShowDialog(owner) != DialogResult.OK) return;

            string sufijo = filtrados ? "_filtrado" : "";
            string ruta048 = Path.Combine(dlg.SelectedPath, "salida_CAT048" + sufijo + ".csv");
            string ruta021 = Path.Combine(dlg.SelectedPath, "salida_CAT021" + sufijo + ".csv");

            try
            {
                IEnumerable<MensajeCAT048> m048 = filtrados ? datos.Mensajes048Filtrados : datos.Mensajes048;
                IEnumerable<MensajeCAT021> m021 = filtrados ? datos.Mensajes021Filtrados : datos.Mensajes021;

                DecoderCAT048.ExportarCSV(m048, ruta048, datos.Filtro048 ?? new FiltroCAT048());
                DecoderCAT021.ExportarCSV(m021, ruta021, datos.Filtro021 ?? new FiltroCAT021());

                Aviso(owner, "CSV exportados en:\n" + dlg.SelectedPath);
            }
            catch (Exception ex)
            {
                Error(owner, ex);
            }
        }

        private static void Aviso(IWin32Window owner, string texto) =>
            MessageBox.Show(owner, texto, "Exportacion", MessageBoxButtons.OK, MessageBoxIcon.Information);

        private static void Error(IWin32Window owner, Exception ex) =>
            MessageBox.Show(owner, "No se pudo exportar:\n" + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
