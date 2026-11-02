#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ASTERIX_Decoder;   // TipoFuente

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Lista de trayectorias generadas por GestorTrayectorias. Al elegir una, se
    /// selecciona en el mapa. No reconstruye ni modifica la asociacion de vuelos.
    /// </summary>
    internal sealed class VistaTrayectorias : UserControl
    {
        private static readonly string[] Columnas =
            { "Callsign", "Aircraft Address", "Fuente", "Puntos", "Inicio", "Fin" };

        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _busqueda = new TextBox();
        private readonly Label _contador = new Label();

        private List<AeronaveSimulada> _todas = new List<AeronaveSimulada>();
        private List<AeronaveSimulada> _filtradas = new List<AeronaveSimulada>();

        public event Action<AeronaveSimulada> TrayectoriaElegida;

        public VistaTrayectorias()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;
            Padding = new Padding(16, 12, 16, 12);

            Controls.Add(ConstruirGrid());
            Controls.Add(ConstruirBarra());
        }

        public void Cargar(IEnumerable<AeronaveSimulada> aeronaves)
        {
            _todas = aeronaves?.OrderByDescending(a => a.NumeroPuntos).ToList() ?? new List<AeronaveSimulada>();
            Filtrar();
        }

        private Control ConstruirBarra()
        {
            var barra = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Tema.Fondo };

            var etq = new Label
            {
                Text = "TRAYECTORIAS",
                Font = Tema.FuenteTitulo,
                ForeColor = Tema.Texto,
                AutoSize = true,
                Location = new Point(2, 8)
            };

            _busqueda.SetBounds(230, 7, 240, 26);
            _busqueda.BorderStyle = BorderStyle.FixedSingle;
            _busqueda.BackColor = Tema.PanelClaro;
            _busqueda.ForeColor = Tema.Texto;
            _busqueda.Font = Tema.Fuente;
            _busqueda.PlaceholderText = "Buscar callsign / direccion...";
            _busqueda.TextChanged += (s, e) => Filtrar();

            _contador.AutoSize = true;
            _contador.Font = Tema.FuentePequena;
            _contador.ForeColor = Tema.TextoTenue;
            _contador.Location = new Point(490, 12);

            var btnVer = new Button { Text = "Ver en mapa", Size = new Size(120, 28) };
            Tema.EstiloBoton(btnVer, acento: true);
            btnVer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnVer.Click += (s, e) => ElegirSeleccionada();

            barra.Controls.Add(etq);
            barra.Controls.Add(_busqueda);
            barra.Controls.Add(_contador);
            barra.Controls.Add(btnVer);
            barra.Resize += (s, e) => btnVer.Left = barra.Width - btnVer.Width - 2;

            return barra;
        }

        private Control ConstruirGrid()
        {
            _grid.Dock = DockStyle.Fill;
            _grid.VirtualMode = true;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            _grid.ColumnHeadersHeight = 32;
            _grid.BorderStyle = BorderStyle.None;
            _grid.BackgroundColor = Tema.Fondo;
            _grid.GridColor = Tema.Borde;
            _grid.EnableHeadersVisualStyles = false;
            _grid.RowTemplate.Height = 24;

            _grid.ColumnHeadersDefaultCellStyle.BackColor = Tema.PanelClaro;
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Tema.Texto;
            _grid.ColumnHeadersDefaultCellStyle.Font = Tema.FuenteFuerte;
            _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Tema.PanelClaro;
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);

            _grid.DefaultCellStyle.BackColor = Tema.Panel;
            _grid.DefaultCellStyle.ForeColor = Tema.Texto;
            _grid.DefaultCellStyle.SelectionBackColor = Tema.AcentoAzul;
            _grid.DefaultCellStyle.SelectionForeColor = Color.White;
            _grid.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Tema.PanelClaro;

            foreach (var c in Columnas)
                _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = c, Name = c, SortMode = DataGridViewColumnSortMode.NotSortable });

            _grid.CellValueNeeded += (s, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _filtradas.Count) return;
                e.Value = Valor(_filtradas[e.RowIndex], e.ColumnIndex);
            };
            _grid.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) ElegirSeleccionada(); };

            return _grid;
        }

        private void Filtrar()
        {
            string q = (_busqueda.Text ?? "").Trim().ToUpperInvariant();
            _filtradas = string.IsNullOrEmpty(q)
                ? new List<AeronaveSimulada>(_todas)
                : _todas.Where(a =>
                      (a.Callsign ?? "").ToUpperInvariant().Contains(q) ||
                      (a.Direccion ?? "").ToUpperInvariant().Contains(q)).ToList();

            _grid.RowCount = _filtradas.Count;
            _contador.Text = $"{_filtradas.Count:N0} trayectorias";
            _grid.Invalidate();
        }

        private void ElegirSeleccionada()
        {
            if (_grid.CurrentRow == null) return;
            int i = _grid.CurrentRow.Index;
            if (i < 0 || i >= _filtradas.Count) return;
            TrayectoriaElegida?.Invoke(_filtradas[i]);
        }

        private static string Valor(AeronaveSimulada a, int col)
        {
            switch (col)
            {
                case 0: return string.IsNullOrWhiteSpace(a.Callsign) ? Formato.ND : a.Callsign.Trim();
                case 1: return string.IsNullOrEmpty(a.Direccion) ? Formato.ND : a.Direccion;
                case 2: return a.Fuente == TipoFuente.Radar ? "Radar" : "ADS-B";
                case 3: return a.NumeroPuntos.ToString();
                case 4: return Formato.Hora(a.TiempoInicio);
                case 5: return Formato.Hora(a.TiempoFin);
                default: return "";
            }
        }
    }
}
