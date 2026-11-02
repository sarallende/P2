#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Barra inferior de control de la simulacion: Play/Pausa, Stop, Reinicio,
    /// avanzar/retroceder 1 s, barra de progreso temporal, selector de velocidad,
    /// reloj simulado (HH:mm:ss) e intervalo total de los datos.
    /// </summary>
    internal sealed class BarraReproduccion : UserControl
    {
        private static readonly double[] Velocidades = { 0.5, 1, 2, 5, 10, 20 };

        private readonly SimuladorTrafico _sim;

        private readonly Button _btnReinicio = new Button { Text = "|< Inicio" };
        private readonly Button _btnMenos = new Button { Text = "-1 s" };
        private readonly Button _btnPlay = new Button { Text = "Play" };
        private readonly Button _btnStop = new Button { Text = "Stop" };
        private readonly Button _btnMas = new Button { Text = "+1 s" };

        private readonly TrackBar _barra = new TrackBar();
        private readonly ComboBox _velocidad = new ComboBox();
        private readonly Label _reloj = new Label();
        private readonly Label _intervalo = new Label();

        private bool _actualizando;

        public BarraReproduccion(SimuladorTrafico sim)
        {
            _sim = sim;

            Dock = DockStyle.Bottom;
            Height = 78;
            BackColor = Tema.Panel;
            Padding = new Padding(14, 8, 14, 8);

            var raiz = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.Panel,
                ColumnCount = 4,
                RowCount = 1
            };
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // --- Botones de transporte ---
            var botones = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 6, 8, 0),
                BackColor = Tema.Panel
            };
            foreach (var b in new[] { _btnReinicio, _btnMenos, _btnPlay, _btnStop, _btnMas })
            {
                Tema.EstiloBoton(b, acento: b == _btnPlay);
                b.Size = new Size(b == _btnPlay ? 90 : 72, 36);
                b.Margin = new Padding(3, 0, 3, 0);
                botones.Controls.Add(b);
            }

            _btnReinicio.Click += (s, e) => _sim.Reiniciar();
            _btnMenos.Click += (s, e) => _sim.Avanzar(-1);
            _btnMas.Click += (s, e) => _sim.Avanzar(1);
            _btnStop.Click += (s, e) => _sim.Parar();
            _btnPlay.Click += (s, e) =>
            {
                if (_sim.Reproduciendo) _sim.Pausa();
                else _sim.Play();
            };

            // --- Barra de progreso temporal ---
            _barra.Dock = DockStyle.Fill;
            _barra.Minimum = 0;
            _barra.Maximum = 1000;
            _barra.TickStyle = TickStyle.None;
            _barra.BackColor = Tema.Panel;
            _barra.Margin = new Padding(6, 10, 6, 0);
            _barra.Scroll += (s, e) =>
            {
                if (_actualizando) return;
                _sim.Seek(TiempoDeBarra(_barra.Value));
            };

            // --- Selector de velocidad ---
            var panelVel = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Margin = new Padding(8, 4, 8, 0),
                BackColor = Tema.Panel
            };
            var etqVel = new Label
            {
                Text = "Velocidad",
                Font = Tema.FuentePequena,
                ForeColor = Tema.TextoTenue,
                AutoSize = true,
                Margin = new Padding(2, 0, 0, 2)
            };
            _velocidad.DropDownStyle = ComboBoxStyle.DropDownList;
            _velocidad.FlatStyle = FlatStyle.Flat;
            _velocidad.BackColor = Tema.PanelClaro;
            _velocidad.ForeColor = Tema.Texto;
            _velocidad.Width = 90;
            _velocidad.Font = Tema.FuenteFuerte;
            foreach (var v in Velocidades)
                _velocidad.Items.Add(v == 1 ? "1x" : (v < 1 ? v.ToString("0.0") + "x" : v.ToString("0") + "x"));
            _velocidad.SelectedIndex = 1;   // 1x
            _velocidad.SelectedIndexChanged += (s, e) =>
            {
                if (_velocidad.SelectedIndex >= 0)
                    _sim.Velocidad = Velocidades[_velocidad.SelectedIndex];
            };
            panelVel.Controls.Add(etqVel);
            panelVel.Controls.Add(_velocidad);

            // --- Reloj simulado + intervalo ---
            var panelReloj = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                Margin = new Padding(10, 2, 2, 0),
                BackColor = Tema.Panel
            };
            _reloj.Text = "--:--:--";
            _reloj.Font = new Font("Segoe UI", 20f, FontStyle.Bold);
            _reloj.ForeColor = Tema.AcentoCian;
            _reloj.AutoSize = true;
            _reloj.Margin = new Padding(0);
            _intervalo.Text = "--:--:--   a   --:--:--";
            _intervalo.Font = Tema.FuentePequena;
            _intervalo.ForeColor = Tema.TextoTenue;
            _intervalo.AutoSize = true;
            _intervalo.Margin = new Padding(0);
            panelReloj.Controls.Add(_reloj);
            panelReloj.Controls.Add(_intervalo);

            raiz.Controls.Add(botones, 0, 0);
            raiz.Controls.Add(_barra, 1, 0);
            raiz.Controls.Add(panelVel, 2, 0);
            raiz.Controls.Add(panelReloj, 3, 0);

            Controls.Add(raiz);

            _sim.TiempoCambiado += OnTiempo;
            _sim.ReproduccionCambiada += OnReproduccion;

            RefrescarEstado();
        }

        private void OnTiempo(double t)
        {
            _actualizando = true;
            _barra.Value = ValorBarra(t);
            _reloj.Text = Formato.Hora(t);
            _actualizando = false;
        }

        private void OnReproduccion() => RefrescarEstado();

        private void RefrescarEstado()
        {
            bool hay = _sim.HayDatos;

            _btnReinicio.Enabled = hay;
            _btnMenos.Enabled = hay;
            _btnPlay.Enabled = hay;
            _btnStop.Enabled = hay;
            _btnMas.Enabled = hay;
            _barra.Enabled = hay;
            _velocidad.Enabled = hay;

            _btnPlay.Text = _sim.Reproduciendo ? "Pausa" : "Play";

            _intervalo.Text = hay
                ? $"{Formato.Hora(_sim.TiempoMin)}   a   {Formato.Hora(_sim.TiempoMax)}"
                : "--:--:--   a   --:--:--";

            if (!hay)
                _reloj.Text = "--:--:--";
        }

        private int ValorBarra(double t)
        {
            if (_sim.TiempoMax <= _sim.TiempoMin) return 0;
            double f = (t - _sim.TiempoMin) / (_sim.TiempoMax - _sim.TiempoMin);
            int v = (int)Math.Round(f * 1000.0);
            return v < 0 ? 0 : (v > 1000 ? 1000 : v);
        }

        private double TiempoDeBarra(int v)
        {
            if (_sim.TiempoMax <= _sim.TiempoMin) return _sim.TiempoMin;
            return _sim.TiempoMin + (v / 1000.0) * (_sim.TiempoMax - _sim.TiempoMin);
        }
    }
}
