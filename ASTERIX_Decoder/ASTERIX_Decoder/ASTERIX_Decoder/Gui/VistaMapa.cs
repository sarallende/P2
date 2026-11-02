#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Pantalla principal de Mapa / Simulacion: barra de herramientas del mapa,
    /// mapa interactivo con los aviones y panel lateral de detalles.
    /// </summary>
    internal sealed class VistaMapa : UserControl
    {
        public MapaControl Mapa { get; } = new MapaControl();
        public PanelDetalles Detalles { get; } = new PanelDetalles();

        private readonly Label _mensaje = new Label();
        private double _ultimoTiempo;

        /// <summary>Reenvia la seleccion de una aeronave desde el mapa.</summary>
        public event Action<AeronaveSimulada> AeronaveClic;

        public VistaMapa()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;

            var centro = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo };

            _mensaje.Text = "Abre un fichero ASTERIX para comenzar  (boton \"Abrir archivo\")";
            _mensaje.AutoSize = true;
            _mensaje.Font = Tema.FuenteFuerte;
            _mensaje.ForeColor = Tema.TextoTenue;
            _mensaje.BackColor = Tema.Panel;
            _mensaje.Padding = new Padding(16, 10, 16, 10);

            centro.Controls.Add(Mapa);
            centro.Controls.Add(_mensaje);
            _mensaje.BringToFront();
            centro.Resize += (s, e) =>
            {
                _mensaje.Left = (centro.Width - _mensaje.Width) / 2;
                _mensaje.Top = 24;
            };

            // Orden de anadido: primero el relleno (centro), luego el panel derecho,
            // y por ultimo la barra de herramientas (arriba).
            Controls.Add(centro);
            Controls.Add(Detalles);
            Controls.Add(ConstruirBarraHerramientas());

            Mapa.AeronaveClic += a =>
            {
                ActualizarDetalles(a);
                AeronaveClic?.Invoke(a);
            };
        }

        private Control ConstruirBarraHerramientas()
        {
            var barra = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Tema.Panel, Padding = new Padding(10, 7, 10, 7) };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Tema.Panel
            };

            Button Boton(string t, int w, EventHandler h)
            {
                var b = new Button { Text = t, Size = new Size(w, 32), Margin = new Padding(0, 0, 8, 0) };
                Tema.EstiloBoton(b);
                b.Click += h;
                return b;
            }

            flow.Controls.Add(Boton("Zoom +", 70, (s, e) => Mapa.AcercarZoom()));
            flow.Controls.Add(Boton("Zoom -", 70, (s, e) => Mapa.AlejarZoom()));
            flow.Controls.Add(Boton("Centrar BCN", 110, (s, e) => Mapa.CentrarBarcelona()));
            flow.Controls.Add(Boton("Centrar avion", 120, (s, e) => Mapa.CentrarEnSeleccionada()));

            flow.Controls.Add(Separador());

            flow.Controls.Add(ChkFuente("Radar", Tema.Radar, true, v => { Mapa.MostrarRadar = v; Mapa.MarcarRutasSucias(); }));
            flow.Controls.Add(ChkFuente("ADS-B", Tema.Adsb, true, v => { Mapa.MostrarAdsb = v; Mapa.MarcarRutasSucias(); }));
            flow.Controls.Add(ChkFuente("Etiquetas", Tema.Texto, true, v => { Mapa.MostrarEtiquetas = v; Mapa.MarcarRutasSucias(); }));

            flow.Controls.Add(Separador());

            var etq = new Label { Text = "Trayectorias:", AutoSize = true, ForeColor = Tema.TextoTenue, Font = Tema.Fuente, Margin = new Padding(0, 8, 6, 0) };
            flow.Controls.Add(etq);

            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Tema.PanelClaro,
                ForeColor = Tema.Texto,
                Width = 150,
                Margin = new Padding(0, 4, 8, 0)
            };
            combo.Items.AddRange(new object[] { "Ninguna", "Todas", "Solo seleccionada", "Recorrido actual" });
            combo.SelectedIndex = 2;   // Solo seleccionada (coincide con el valor por defecto)
            combo.SelectedIndexChanged += (s, e) =>
            {
                Mapa.ModoTrayectorias = (ModoTrayectoria)combo.SelectedIndex;
                Mapa.MarcarRutasSucias();
            };
            flow.Controls.Add(combo);

            flow.Controls.Add(ChkFuente("Seguir", Tema.AcentoCian, false, v => Mapa.SeguirSeleccionada = v));

            barra.Controls.Add(flow);
            return barra;
        }

        private static Control Separador() => new Panel { Width = 1, Height = 28, BackColor = Tema.Borde, Margin = new Padding(6, 2, 10, 0) };

        private static CheckBox ChkFuente(string texto, Color color, bool marcado, Action<bool> alCambiar)
        {
            var c = new CheckBox
            {
                Text = texto,
                AutoSize = true,
                Checked = marcado,
                ForeColor = color,
                Font = Tema.FuenteFuerte,
                Margin = new Padding(0, 7, 10, 0)
            };
            c.CheckedChanged += (s, e) => alCambiar(c.Checked);
            return c;
        }

        // ----- API para la ventana principal -----

        public void MostrarMensajeInicial(bool visible) => _mensaje.Visible = visible;

        public void Refrescar(double t)
        {
            _ultimoTiempo = t;
            Mapa.Actualizar(t);

            var sel = Mapa.Seleccionada;
            if (sel != null && sel.TryEstado(t, out var e))
                Detalles.Mostrar(e, sel.NumeroPuntos);
        }

        public void SeleccionarExterna(AeronaveSimulada a)
        {
            Mapa.Seleccionar(a, centrar: true);
            ActualizarDetalles(a);
        }

        private void ActualizarDetalles(AeronaveSimulada a)
        {
            if (a != null && a.TryEstado(_ultimoTiempo, out var e))
                Detalles.Mostrar(e, a.NumeroPuntos);
            else
                Detalles.Limpiar();
        }
    }
}
