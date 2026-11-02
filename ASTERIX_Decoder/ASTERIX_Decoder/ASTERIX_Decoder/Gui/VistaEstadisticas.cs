#nullable disable
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Panel de estadisticas (dashboard). Distingue claramente numero de mensajes,
    /// numero de trayectorias y numero de aeronaves unicas. No inventa metricas.
    /// </summary>
    internal sealed class VistaEstadisticas : UserControl
    {
        private readonly Dictionary<string, Label> _valores = new Dictionary<string, Label>();

        public VistaEstadisticas()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;
            Padding = new Padding(22, 18, 22, 18);
            AutoScroll = true;

            var raiz = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Tema.Fondo
            };

            raiz.Controls.Add(new Label
            {
                Text = "ESTADISTICAS",
                Font = Tema.FuenteTitulo,
                ForeColor = Tema.Texto,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            });

            var tarjetas = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Tema.Fondo,
                MaximumSize = new Size(1100, 0)
            };

            tarjetas.Controls.Add(Tarjeta("msg048", "Mensajes CAT048", Tema.Radar));
            tarjetas.Controls.Add(Tarjeta("msg021", "Mensajes CAT021", Tema.Adsb));
            tarjetas.Controls.Add(Tarjeta("filt048", "CAT048 filtrados", Tema.Radar));
            tarjetas.Controls.Add(Tarjeta("filt021", "CAT021 filtrados", Tema.Adsb));
            tarjetas.Controls.Add(Tarjeta("trayRadar", "Trayectorias Radar", Tema.Radar));
            tarjetas.Controls.Add(Tarjeta("trayAdsb", "Trayectorias ADS-B", Tema.Adsb));
            tarjetas.Controls.Add(Tarjeta("unicas", "Aeronaves unicas", Tema.AcentoCian));
            tarjetas.Controls.Add(Tarjeta("visibles", "Visibles ahora", Tema.Seleccion));
            tarjetas.Controls.Add(Tarjeta("intervalo", "Intervalo temporal", Tema.AcentoAzul, ancho: 300));
            tarjetas.Controls.Add(Tarjeta("tdecod", "Tiempo de decodificacion", Tema.AcentoAzul));

            raiz.Controls.Add(tarjetas);
            Controls.Add(raiz);
        }

        public void Actualizar(DatosAsterix d, long msDecodificacion, double tMin, double tMax)
        {
            if (d == null) return;

            int unicas = d.Mensajes048Filtrados.Select(m => m.AircraftAddressHex)
                .Concat(d.Mensajes021Filtrados.Select(m => m.AircraftAddressHex))
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .Count();

            _valores["msg048"].Text = d.Mensajes048.Length.ToString("N0");
            _valores["msg021"].Text = d.Mensajes021.Length.ToString("N0");
            _valores["filt048"].Text = d.Mensajes048Filtrados.Count.ToString("N0");
            _valores["filt021"].Text = d.Mensajes021Filtrados.Count.ToString("N0");
            _valores["trayRadar"].Text = d.TrayectoriasRadar.Count.ToString("N0");
            _valores["trayAdsb"].Text = d.TrayectoriasAdsb.Count.ToString("N0");
            _valores["unicas"].Text = unicas.ToString("N0");
            _valores["tdecod"].Text = msDecodificacion > 0 ? $"{msDecodificacion:N0} ms" : Formato.ND;
            _valores["intervalo"].Text = (tMax > tMin)
                ? $"{Formato.Hora(tMin)}  a  {Formato.Hora(tMax)}"
                : Formato.ND;
        }

        public void ActualizarVisibles(int n)
        {
            if (_valores.TryGetValue("visibles", out var l))
                l.Text = n.ToString("N0");
        }

        private Panel Tarjeta(string clave, string titulo, Color acento, int ancho = 210)
        {
            var panel = new Panel
            {
                Size = new Size(ancho, 112),
                BackColor = Tema.Panel,
                Margin = new Padding(0, 0, 14, 14),
                Padding = new Padding(16, 12, 12, 12)
            };

            // Franja de color a la izquierda.
            var franja = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = acento };

            var valor = new Label
            {
                Text = Formato.ND,
                Font = Tema.FuenteNumero,
                ForeColor = Tema.Texto,
                AutoSize = true,
                Location = new Point(16, 18)
            };
            var tit = new Label
            {
                Text = titulo,
                Font = Tema.FuentePequena,
                ForeColor = Tema.TextoTenue,
                AutoSize = true,
                Location = new Point(16, 74)
            };

            _valores[clave] = valor;
            panel.Controls.Add(valor);
            panel.Controls.Add(tit);
            panel.Controls.Add(franja);
            return panel;
        }
    }
}
