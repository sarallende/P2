#nullable disable
using System.Drawing;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Paleta de colores, tipografias y ayudas de estilo del tema oscuro.
    /// Centraliza el aspecto "software aeronautico profesional" descrito en el
    /// encargo para que todos los controles sean coherentes.
    /// </summary>
    internal static class Tema
    {
        // --- Fondos ---
        public static readonly Color Fondo = Color.FromArgb(0x0B, 0x12, 0x20);       // azul marino muy oscuro
        public static readonly Color Panel = Color.FromArgb(0x11, 0x1C, 0x2E);       // paneles
        public static readonly Color PanelClaro = Color.FromArgb(0x16, 0x24, 0x3B);  // paneles destacados
        public static readonly Color Elevado = Color.FromArgb(0x1C, 0x2C, 0x45);     // hover / seleccion suave
        public static readonly Color Borde = Color.FromArgb(0x24, 0x35, 0x50);

        // --- Acentos ---
        public static readonly Color AcentoAzul = Color.FromArgb(0x3B, 0x82, 0xF6);  // azul electrico
        public static readonly Color AcentoCian = Color.FromArgb(0x22, 0xD3, 0xEE);  // cian

        // --- Texto ---
        public static readonly Color Texto = Color.FromArgb(0xE6, 0xED, 0xF8);
        public static readonly Color TextoTenue = Color.FromArgb(0x8B, 0x9C, 0xB5);

        // --- Codigo de color de fuentes (mapa / tablas) ---
        public static readonly Color Radar = Color.FromArgb(0x4D, 0xA6, 0xFF);       // CAT048 / Radar: azul-cian
        public static readonly Color Adsb = Color.FromArgb(0x35, 0xE0, 0xA8);        // CAT021 / ADS-B: verde-turquesa
        public static readonly Color Seleccion = Color.FromArgb(0xFF, 0xB0, 0x3A);   // aeronave seleccionada: naranja

        // --- Tipografia ---
        public static Font Fuente => new Font("Segoe UI", 9.5f);
        public static Font FuentePequena => new Font("Segoe UI", 8.25f);
        public static Font FuenteFuerte => new Font("Segoe UI Semibold", 9.5f);
        public static Font FuenteTitulo => new Font("Segoe UI Semibold", 13f);
        public static Font FuenteNumero => new Font("Segoe UI", 22f, FontStyle.Bold);

        /// <summary>Aplica el aspecto de boton moderno plano con estado hover.</summary>
        public static void EstiloBoton(Button b, bool acento = false)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = acento ? 0 : 1;
            b.FlatAppearance.BorderColor = Borde;
            b.BackColor = acento ? AcentoAzul : PanelClaro;
            b.ForeColor = acento ? Color.White : Texto;
            b.Font = FuenteFuerte;
            b.Cursor = Cursors.Hand;
            b.UseVisualStyleBackColor = false;
            b.AutoSize = false;
            b.Padding = new Padding(10, 0, 10, 0);

            Color normal = b.BackColor;
            Color hover = acento ? AcentoCian : Elevado;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = hover;
        }

        /// <summary>Boton compacto para iconos / barras de herramientas.</summary>
        public static void EstiloBotonIcono(Button b)
        {
            EstiloBoton(b);
            b.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
            b.FlatAppearance.BorderSize = 1;
        }
    }
}
