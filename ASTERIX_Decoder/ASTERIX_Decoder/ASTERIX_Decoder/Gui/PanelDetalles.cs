#nullable disable
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using ASTERIX_Decoder;   // TipoFuente

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Panel lateral derecho con la informacion de la aeronave seleccionada.
    /// Muestra "N/D" en los campos que no existan para ese mensaje.
    /// </summary>
    internal sealed class PanelDetalles : UserControl
    {
        private readonly Label _subtitulo = new Label();
        private readonly Dictionary<string, Label> _valores = new Dictionary<string, Label>();

        private static readonly string[] Campos =
        {
            "Callsign", "Aircraft Address", "Fuente", "Mode 3/A",
            "Altitud", "Velocidad", "Rumbo", "Latitud", "Longitud",
            "Ultimo mensaje", "Puntos tray."
        };

        public PanelDetalles()
        {
            Dock = DockStyle.Right;
            Width = 300;
            BackColor = Tema.Panel;

            var cont = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.Panel,
                ColumnCount = 2,
                Padding = new Padding(16, 16, 16, 16),
                AutoScroll = true
            };
            cont.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));
            cont.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));

            var titulo = new Label
            {
                Text = "INFORMACION DE AERONAVE",
                Font = Tema.FuenteTitulo,
                ForeColor = Tema.Texto,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };
            _subtitulo.Text = "Ninguna seleccionada";
            _subtitulo.Font = Tema.FuentePequena;
            _subtitulo.ForeColor = Tema.AcentoCian;
            _subtitulo.AutoSize = true;
            _subtitulo.Margin = new Padding(0, 0, 0, 14);

            cont.Controls.Add(titulo, 0, 0);
            cont.SetColumnSpan(titulo, 2);
            cont.Controls.Add(_subtitulo, 0, 1);
            cont.SetColumnSpan(_subtitulo, 2);

            int fila = 2;
            foreach (var campo in Campos)
            {
                var clave = new Label
                {
                    Text = campo,
                    Font = Tema.FuentePequena,
                    ForeColor = Tema.TextoTenue,
                    AutoSize = true,
                    Margin = new Padding(0, 6, 8, 6)
                };
                var valor = new Label
                {
                    Text = Formato.ND,
                    Font = Tema.FuenteFuerte,
                    ForeColor = Tema.Texto,
                    AutoSize = true,
                    Margin = new Padding(0, 6, 0, 6)
                };
                _valores[campo] = valor;
                cont.Controls.Add(clave, 0, fila);
                cont.Controls.Add(valor, 1, fila);
                fila++;
            }

            cont.RowCount = fila + 1;
            cont.RowStyles.Clear();
            for (int i = 0; i < fila; i++)
                cont.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            cont.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // empuja hacia arriba

            Controls.Add(cont);
            Limpiar();
        }

        public void Limpiar()
        {
            _subtitulo.Text = "Ninguna seleccionada";
            foreach (var v in _valores.Values)
                v.Text = Formato.ND;
        }

        public void Mostrar(EstadoAeronave e, int numeroPuntos)
        {
            if (e == null)
            {
                Limpiar();
                return;
            }

            string callsign = string.IsNullOrWhiteSpace(e.Callsign) ? Formato.ND : e.Callsign.Trim();
            string address = string.IsNullOrWhiteSpace(e.Direccion) ? Formato.ND : e.Direccion;

            _subtitulo.Text = (callsign != Formato.ND ? callsign : address) +
                              "   -   " + (e.Fuente == TipoFuente.Radar ? "Radar" : "ADS-B");

            _valores["Callsign"].Text = callsign;
            _valores["Aircraft Address"].Text = address;
            _valores["Fuente"].Text = e.Fuente == TipoFuente.Radar ? "Radar (CAT048)" : "ADS-B (CAT021)";
            _valores["Mode 3/A"].Text = e.Mode3A.HasValue ? e.Mode3A.Value.ToString("D4") : Formato.ND;
            _valores["Altitud"].Text = e.AltitudFt.HasValue ? $"{e.AltitudFt.Value:0} ft" : Formato.ND;
            _valores["Velocidad"].Text = e.GroundSpeedKt.HasValue ? $"{e.GroundSpeedKt.Value:0} kt" : Formato.ND;
            _valores["Rumbo"].Text = e.HeadingReal.HasValue ? $"{e.HeadingReal.Value:0} deg" : Formato.ND;
            _valores["Latitud"].Text = $"{e.Latitud:0.00000}";
            _valores["Longitud"].Text = $"{e.Longitud:0.00000}";
            _valores["Ultimo mensaje"].Text = Formato.Hora(e.TiempoUltimoMensaje);
            _valores["Puntos tray."].Text = numeroPuntos.ToString();
        }
    }
}
