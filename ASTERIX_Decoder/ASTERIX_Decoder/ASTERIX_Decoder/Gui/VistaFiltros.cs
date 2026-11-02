#nullable disable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Seccion de filtros. Construye objetos FiltroCAT048 / FiltroCAT021 a partir
    /// de los checkboxes y los entrega a la aplicacion para que los aplique con
    /// los metodos Aplicar() existentes. No duplica ninguna logica de filtrado.
    /// Los valores iniciales coinciden con la configuracion de Program.cs.
    /// </summary>
    internal sealed class VistaFiltros : UserControl
    {
        // CAT048
        private readonly CheckBox _c48FlNoVal = Crear("Eliminar FL no validado");
        private readonly CheckBox _c48FlGarbled = Crear("Eliminar FL garbled");
        private readonly CheckBox _c48FueraArea = Crear("Eliminar fuera del area");
        private readonly CheckBox _c48BlancoPuro = Crear("Eliminar blanco puro");
        private readonly CheckBox _c48Transponder = Crear("Eliminar transponder fijo (7777)");
        private readonly CheckBox _c48Tierra = Crear("Eliminar aeronaves en tierra");

        // CAT021
        private readonly CheckBox _c21Tierra = Crear("Eliminar aeronaves en tierra");
        private readonly CheckBox _c21FueraArea = Crear("Eliminar fuera del area");
        private readonly CheckBox _c21Transponder = Crear("Eliminar transponder fijo (7777)");

        private readonly Label _indicador = new Label();
        private readonly Label _cont048 = new Label();
        private readonly Label _cont021 = new Label();

        /// <summary>Se dispara al pulsar "Aplicar filtros".</summary>
        public event Action<FiltroCAT048, FiltroCAT021> FiltrosAplicados;

        public VistaFiltros()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;
            Padding = new Padding(22, 18, 22, 18);
            AutoScroll = true;

            RestablecerValores();   // config inicial de Program.cs

            var raiz = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Tema.Fondo
            };

            raiz.Controls.Add(Titulo("FILTROS DE DATOS"));

            var grupos = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Tema.Fondo,
                Margin = new Padding(0, 8, 0, 8)
            };
            grupos.Controls.Add(Seccion("CAT048 / Radar", Tema.Radar,
                _c48FlNoVal, _c48FlGarbled, _c48FueraArea, _c48BlancoPuro, _c48Transponder, _c48Tierra));
            grupos.Controls.Add(Seccion("CAT021 / ADS-B", Tema.Adsb,
                _c21Tierra, _c21FueraArea, _c21Transponder));
            raiz.Controls.Add(grupos);

            // Botones de accion.
            var acciones = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                BackColor = Tema.Fondo,
                Margin = new Padding(0, 4, 0, 10)
            };
            var btnAplicar = new Button { Text = "Aplicar filtros", Size = new Size(140, 38) };
            var btnReset = new Button { Text = "Restablecer", Size = new Size(120, 38) };
            Tema.EstiloBoton(btnAplicar, acento: true);
            Tema.EstiloBoton(btnReset);
            btnAplicar.Margin = new Padding(0, 0, 10, 0);
            btnAplicar.Click += (s, e) => Aplicar();
            btnReset.Click += (s, e) => { RestablecerValores(); Aplicar(); };

            _indicador.AutoSize = true;
            _indicador.Font = Tema.FuenteFuerte;
            _indicador.ForeColor = Tema.AcentoCian;
            _indicador.Margin = new Padding(14, 9, 0, 0);

            acciones.Controls.Add(btnAplicar);
            acciones.Controls.Add(btnReset);
            acciones.Controls.Add(_indicador);
            raiz.Controls.Add(acciones);

            // Contadores.
            foreach (var l in new[] { _cont048, _cont021 })
            {
                l.AutoSize = true;
                l.Font = Tema.Fuente;
                l.ForeColor = Tema.Texto;
                l.Margin = new Padding(2, 2, 0, 2);
            }
            _cont048.Text = "CAT048  -  sin datos cargados";
            _cont021.Text = "CAT021  -  sin datos cargados";
            raiz.Controls.Add(_cont048);
            raiz.Controls.Add(_cont021);

            Controls.Add(raiz);
            ActualizarIndicador();
        }

        public FiltroCAT048 ConstruirFiltro048() => new FiltroCAT048
        {
            EliminarFlNoValidado = _c48FlNoVal.Checked,
            EliminarFlGarbled = _c48FlGarbled.Checked,
            EliminarFueraDelArea = _c48FueraArea.Checked,
            EliminarBlancoPuro = _c48BlancoPuro.Checked,
            EliminarTransponderFijo = _c48Transponder.Checked,
            EliminarEnTierra = _c48Tierra.Checked
        };

        public FiltroCAT021 ConstruirFiltro021() => new FiltroCAT021
        {
            EliminarEnTierra = _c21Tierra.Checked,
            EliminarFueraDelArea = _c21FueraArea.Checked,
            EliminarTransponderFijo = _c21Transponder.Checked
        };

        public void ActualizarContadores(int orig048, int filt048, int orig021, int filt021)
        {
            _cont048.Text = $"CAT048  -  originales: {orig048:N0}    filtrados: {filt048:N0}    descartados: {(orig048 - filt048):N0}";
            _cont021.Text = $"CAT021  -  originales: {orig021:N0}    filtrados: {filt021:N0}    descartados: {(orig021 - filt021):N0}";
        }

        private void Aplicar()
        {
            ActualizarIndicador();
            FiltrosAplicados?.Invoke(ConstruirFiltro048(), ConstruirFiltro021());
        }

        private void ActualizarIndicador()
        {
            bool activos = ConstruirFiltro048().HayFiltrosActivos || ConstruirFiltro021().HayFiltrosActivos;
            _indicador.Text = activos ? "Filtros activos" : "Sin filtros";
            _indicador.ForeColor = activos ? Tema.AcentoCian : Tema.TextoTenue;
        }

        private void RestablecerValores()
        {
            // Configuracion inicial identica a Program.cs.
            _c48FlNoVal.Checked = false;
            _c48FlGarbled.Checked = false;
            _c48FueraArea.Checked = true;
            _c48BlancoPuro.Checked = true;
            _c48Transponder.Checked = true;
            _c48Tierra.Checked = true;

            _c21Tierra.Checked = true;
            _c21FueraArea.Checked = true;
            _c21Transponder.Checked = true;
        }

        // ----- Ayudas de construccion de UI -----

        private static CheckBox Crear(string texto) => new CheckBox
        {
            Text = texto,
            AutoSize = true,
            ForeColor = Tema.Texto,
            BackColor = Color.Transparent,
            Font = Tema.Fuente,
            Margin = new Padding(0, 5, 0, 5)
        };

        private static Label Titulo(string texto) => new Label
        {
            Text = texto,
            Font = Tema.FuenteTitulo,
            ForeColor = Tema.Texto,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 6)
        };

        private static Panel Seccion(string titulo, Color acento, params CheckBox[] checks)
        {
            var panel = new Panel
            {
                BackColor = Tema.Panel,
                Padding = new Padding(16, 12, 20, 14),
                Margin = new Padding(0, 0, 16, 0),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var cont = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Tema.Panel
            };

            var lbl = new Label
            {
                Text = titulo,
                Font = Tema.FuenteFuerte,
                ForeColor = acento,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            cont.Controls.Add(lbl);
            foreach (var c in checks) cont.Controls.Add(c);

            panel.Controls.Add(cont);
            return panel;
        }
    }
}
