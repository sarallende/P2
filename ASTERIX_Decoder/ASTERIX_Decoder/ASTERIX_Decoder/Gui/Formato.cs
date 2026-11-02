#nullable disable
using System;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>Formatos compartidos (tiempo simulado, valores no disponibles).</summary>
    internal static class Formato
    {
        public const string ND = "N/D";

        /// <summary>Convierte segundos desde medianoche en HH:mm:ss.</summary>
        public static string Hora(double segundosDesdeMedianoche)
        {
            if (double.IsNaN(segundosDesdeMedianoche) || segundosDesdeMedianoche < 0)
                return "--:--:--";

            var ts = TimeSpan.FromSeconds(segundosDesdeMedianoche);
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
        }

        /// <summary>Convierte segundos desde medianoche en HH:mm.</summary>
        public static string HoraMin(double segundos)
        {
            if (double.IsNaN(segundos) || segundos < 0)
                return "--:--";

            var ts = TimeSpan.FromSeconds(segundos);
            return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}";
        }

        /// <summary>Tamano de fichero legible.</summary>
        public static string Tamano(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] u = { "B", "KB", "MB", "GB" };
            double v = bytes;
            int i = 0;
            while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
            return $"{v:0.#} {u[i]}";
        }
    }
}
