#nullable disable
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Panel de estadisticas (dashboard). Tarjetas agrupadas por secciones, con
    /// maquetacion por Dock (no posiciones absolutas) para que el texto no se
    /// recorte con escalado DPI. Distingue mensajes, trayectorias y aeronaves.
    /// </summary>
    internal sealed class VistaEstadisticas : UserControl
    {
        private readonly Dictionary<string, Label> _valores = new Dictionary<string, Label>();

        public VistaEstadisticas()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;
            Padding = new Padding(26, 20, 26, 20);
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
                Margin = new Padding(0, 0, 0, 16)
            });

            raiz.Controls.Add(Seccion("Mensajes decodificados"));
            raiz.Controls.Add(FilaTarjetas(
                Tarjeta("msg048", "Mensajes CAT048", Tema.Radar),
                Tarjeta("msg021", "Mensajes CAT021", Tema.Adsb),
                Tarjeta("filt048", "CAT048 tras filtros", Tema.Radar),
                Tarjeta("filt021", "CAT021 tras filtros", Tema.Adsb)));

            raiz.Controls.Add(Seccion("Trayectorias y aeronaves"));
            raiz.Controls.Add(FilaTarjetas(
                Tarjeta("trayRadar", "Trayectorias Radar", Tema.Radar),
                Tarjeta("trayAdsb", "Trayectorias ADS-B", Tema.Adsb),
                Tarjeta("unicas", "Aeronaves unicas", Tema.AcentoCian),
                Tarjeta("visibles", "Visibles ahora", Tema.Seleccion)));

            raiz.Controls.Add(Seccion("Tiempo de los datos"));
            raiz.Controls.Add(FilaTarjetas(
                Tarjeta("intervalo", "Intervalo temporal", Tema.AcentoAzul, ancho: 360, tamValor: 15f),
                Tarjeta("tdecod", "Tiempo de decodificacion", Tema.AcentoAzul)));

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

        // ----- Construccion de UI -----

        private static Label Seccion(string texto) => new Label
        {
            Text = texto.ToUpperInvariant(),
            Font = Tema.FuenteFuerte,
            ForeColor = Tema.AcentoCian,
            AutoSize = true,
            Margin = new Padding(2, 8, 0, 8)
        };

        private static FlowLayoutPanel FilaTarjetas(params Control[] tarjetas)
        {
            var fila = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Tema.Fondo,
                MaximumSize = new Size(1220, 0),
                Margin = new Padding(0, 0, 0, 10)
            };
            fila.Controls.AddRange(tarjetas);
            return fila;
        }

        private Panel Tarjeta(string clave, string titulo, Color acento, int ancho = 236, float tamValor = 22f)
        {
            var panel = new Panel
            {
                Size = new Size(ancho, 100),
                BackColor = Tema.Panel,
                Margin = new Padding(0, 0, 14, 14)
            };

            var franja = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = acento };

            var inner = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.Panel,
                Padding = new Padding(18, 14, 14, 14)
            };

            // Valor (ocupa el resto, debajo del titulo). AutoEllipsis evita cortes feos.
            var valor = new Label
            {
                Text = Formato.ND,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", tamValor, FontStyle.Bold),
                ForeColor = Tema.Texto,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                AutoEllipsis = true
            };

            var tit = new Label
            {
                Text = titulo.ToUpperInvariant(),
                Dock = DockStyle.Top,
                Height = 20,
                Font = Tema.FuentePequena,
                ForeColor = Tema.TextoTenue,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                AutoEllipsis = true
            };

            _valores[clave] = valor;

            inner.Controls.Add(valor);   // primero el que ocupa el resto
            inner.Controls.Add(tit);     // despues el de arriba
            panel.Controls.Add(inner);
            panel.Controls.Add(franja);
            return panel;
        }
    }
}
