#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Tabla de mensajes decodificados (CAT048 o CAT021) sobre un DataGridView en
    /// modo virtual. Permite buscar (Aircraft Address / callsign / Mode 3/A),
    /// ordenar por columnas y exportar a CSV reutilizando los metodos existentes
    /// de los decodificadores. Los datos proceden directamente de MensajeCAT048 /
    /// MensajeCAT021; no se recalcula ningun campo.
    /// </summary>
    internal sealed class VistaTabla : UserControl
    {
        private readonly bool _esCat048;
        private readonly DataGridView _grid = new DataGridView();
        private readonly TextBox _busqueda = new TextBox();
        private readonly Label _contador = new Label();

        private readonly string[] _columnas;

        private List<MensajeCAT048> _l048 = new List<MensajeCAT048>();
        private List<MensajeCAT048> _f048 = new List<MensajeCAT048>();
        private FiltroCAT048 _filtro048 = new FiltroCAT048();

        private List<MensajeCAT021> _l021 = new List<MensajeCAT021>();
        private List<MensajeCAT021> _f021 = new List<MensajeCAT021>();
        private FiltroCAT021 _filtro021 = new FiltroCAT021();

        private int _colOrden = -1;
        private bool _ascendente = true;

        public VistaTabla(bool esCat048)
        {
            _esCat048 = esCat048;
            _columnas = esCat048
                ? new[] { "Tiempo", "Aircraft Address", "Callsign", "Mode 3/A", "FL",
                          "Alt. QNH (ft)", "Latitud", "Longitud", "GS (kt)", "Heading", "Track" }
                : new[] { "T. recepcion", "Aircraft Address", "Callsign", "Mode 3/A", "FL",
                          "Alt. QNH (ft)", "Latitud", "Longitud", "Ground Bit" };

            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;
            Padding = new Padding(16, 12, 16, 12);

            Controls.Add(ConstruirGrid());
            Controls.Add(ConstruirBarra());
        }

        // ----- Carga -----

        public void CargarCat048(List<MensajeCAT048> datos, FiltroCAT048 filtro)
        {
            _l048 = datos ?? new List<MensajeCAT048>();
            _filtro048 = filtro ?? new FiltroCAT048();
            AplicarBusqueda();
        }

        public void CargarCat021(List<MensajeCAT021> datos, FiltroCAT021 filtro)
        {
            _l021 = datos ?? new List<MensajeCAT021>();
            _filtro021 = filtro ?? new FiltroCAT021();
            AplicarBusqueda();
        }

        // ----- Construccion UI -----

        private Control ConstruirBarra()
        {
            var barra = new Panel { Dock = DockStyle.Top, Height = 44, BackColor = Tema.Fondo };

            var etq = new Label
            {
                Text = _esCat048 ? "DATOS CAT048 / Radar" : "DATOS CAT021 / ADS-B",
                Font = Tema.FuenteTitulo,
                ForeColor = Tema.Texto,
                AutoSize = true,
                Location = new Point(2, 8)
            };

            _busqueda.SetBounds(360, 7, 220, 26);
            _busqueda.BorderStyle = BorderStyle.FixedSingle;
            _busqueda.BackColor = Tema.PanelClaro;
            _busqueda.ForeColor = Tema.Texto;
            _busqueda.Font = Tema.Fuente;
            _busqueda.PlaceholderText = "Buscar direccion / callsign / Mode 3/A...";
            _busqueda.Width = 260;
            _busqueda.TextChanged += (s, e) => AplicarBusqueda();

            _contador.AutoSize = true;
            _contador.Font = Tema.FuentePequena;
            _contador.ForeColor = Tema.TextoTenue;
            _contador.Location = new Point(640, 12);

            var btnExport = new Button { Text = "Exportar CSV", Size = new Size(120, 28) };
            Tema.EstiloBoton(btnExport);
            btnExport.Location = new Point(Width - 136, 8);
            btnExport.Click += (s, e) => Exportar();

            barra.Controls.Add(etq);
            barra.Controls.Add(_busqueda);
            barra.Controls.Add(_contador);
            barra.Controls.Add(btnExport);

            // Reposiciona busqueda/contador/boton al ancho disponible.
            barra.Resize += (s, e) =>
            {
                btnExport.Left = barra.Width - btnExport.Width - 2;
                _contador.Left = btnExport.Left - _contador.Width - 16;
                _busqueda.Left = _contador.Left - _busqueda.Width - 16;
            };

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
            _grid.AlternatingRowsDefaultCellStyle.ForeColor = Tema.Texto;

            foreach (var c in _columnas)
            {
                _grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    HeaderText = c,
                    SortMode = DataGridViewColumnSortMode.Programmatic,
                    Name = c
                });
            }

            _grid.CellValueNeeded += GridCellValueNeeded;
            _grid.ColumnHeaderMouseClick += GridColumnHeaderMouseClick;

            return _grid;
        }

        // ----- Datos para el grid virtual -----

        private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (_esCat048)
            {
                if (e.RowIndex < 0 || e.RowIndex >= _f048.Count) return;
                e.Value = Valor048(_f048[e.RowIndex], e.ColumnIndex);
            }
            else
            {
                if (e.RowIndex < 0 || e.RowIndex >= _f021.Count) return;
                e.Value = Valor021(_f021[e.RowIndex], e.ColumnIndex);
            }
        }

        private void AplicarBusqueda()
        {
            string q = (_busqueda.Text ?? "").Trim();

            if (_esCat048)
            {
                _f048 = string.IsNullOrEmpty(q)
                    ? new List<MensajeCAT048>(_l048)
                    : _l048.Where(m => Coincide(m.AircraftAddressHex, m.AircraftIdentification, m.Mode3ACodigo.ToString(), q)).ToList();
                OrdenarActual();
                _grid.RowCount = _f048.Count;
                _contador.Text = $"{_f048.Count:N0} de {_l048.Count:N0} mensajes";
            }
            else
            {
                _f021 = string.IsNullOrEmpty(q)
                    ? new List<MensajeCAT021>(_l021)
                    : _l021.Where(m => Coincide(m.AircraftAddressHex, m.AircraftIdentification, m.Mode3ACodigo?.ToString(), q)).ToList();
                OrdenarActual();
                _grid.RowCount = _f021.Count;
                _contador.Text = $"{_f021.Count:N0} de {_l021.Count:N0} mensajes";
            }

            _grid.Invalidate();
        }

        private static bool Coincide(string dir, string cs, string modo, string q)
        {
            q = q.ToUpperInvariant();
            return (dir ?? "").ToUpperInvariant().Contains(q)
                || (cs ?? "").ToUpperInvariant().Contains(q)
                || (modo ?? "").Contains(q);
        }

        // ----- Ordenacion -----

        private void GridColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            if (_colOrden == e.ColumnIndex) _ascendente = !_ascendente;
            else { _colOrden = e.ColumnIndex; _ascendente = true; }

            OrdenarActual();
            _grid.Invalidate();
        }

        private void OrdenarActual()
        {
            if (_colOrden < 0) return;
            int signo = _ascendente ? 1 : -1;

            if (_esCat048)
                _f048.Sort((a, b) => signo * Comparar(Clave048(a, _colOrden), Clave048(b, _colOrden)));
            else
                _f021.Sort((a, b) => signo * Comparar(Clave021(a, _colOrden), Clave021(b, _colOrden)));
        }

        private static int Comparar(IComparable x, IComparable y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return x.CompareTo(y);
        }

        // ----- Exportacion CSV (reutiliza los decodificadores) -----

        private void Exportar()
        {
            using var dlg = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv",
                FileName = _esCat048 ? "CAT048_tabla.csv" : "CAT021_tabla.csv"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                if (_esCat048)
                    DecoderCAT048.ExportarCSV(_f048, dlg.FileName, _filtro048);
                else
                    DecoderCAT021.ExportarCSV(_f021, dlg.FileName, _filtro021);

                MessageBox.Show(this, "CSV exportado:\n" + dlg.FileName, "Exportacion",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "No se pudo exportar:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ----- Valores de celda -----

        private static string Valor048(MensajeCAT048 m, int col)
        {
            switch (col)
            {
                case 0: return Formato.Hora(m.TiempoDiaSeg);
                case 1: return string.IsNullOrEmpty(m.AircraftAddressHex) ? Formato.ND : m.AircraftAddressHex;
                case 2: return string.IsNullOrWhiteSpace(m.AircraftIdentification) ? Formato.ND : m.AircraftIdentification.Trim();
                case 3: return m.Mode3ACodigo.ToString("D4");
                case 4: return m.FlightLevel.HasValue ? m.FlightLevel.Value.ToString("0.##") : Formato.ND;
                case 5:
                    double? a = m.AltitudCorregidaQNH ?? m.AltitudEstandarFt;
                    return a.HasValue ? a.Value.ToString("0") : Formato.ND;
                case 6: return m.Latitud.ToString("0.00000");
                case 7: return m.Longitud.ToString("0.00000");
                case 8: return m.GroundSpeedKt.ToString("0");
                case 9: return m.Heading.ToString("0");
                case 10: return m.TrackNumber.ToString();
                default: return "";
            }
        }

        private static string Valor021(MensajeCAT021 m, int col)
        {
            switch (col)
            {
                case 0: return m.TiempoRecepcionPosicion.HasValue ? Formato.Hora(m.TiempoRecepcionPosicion.Value) : Formato.ND;
                case 1: return string.IsNullOrEmpty(m.AircraftAddressHex) ? Formato.ND : m.AircraftAddressHex;
                case 2: return string.IsNullOrWhiteSpace(m.AircraftIdentification) ? Formato.ND : m.AircraftIdentification.Trim();
                case 3: return m.Mode3ACodigo.HasValue ? m.Mode3ACodigo.Value.ToString("D4") : Formato.ND;
                case 4: return m.FlightLevel.HasValue ? m.FlightLevel.Value.ToString("0.##") : Formato.ND;
                case 5:
                    double? a = m.AltitudCorregidaQNH ?? m.AltitudEstandarFt;
                    return a.HasValue ? a.Value.ToString("0") : Formato.ND;
                case 6: return m.Latitud.HasValue ? m.Latitud.Value.ToString("0.00000") : Formato.ND;
                case 7: return m.Longitud.HasValue ? m.Longitud.Value.ToString("0.00000") : Formato.ND;
                case 8: return m.GroundBitStatus.HasValue ? (m.GroundBitStatus.Value ? "En tierra" : "En vuelo") : Formato.ND;
                default: return "";
            }
        }

        // ----- Claves de ordenacion -----

        private static IComparable Clave048(MensajeCAT048 m, int col)
        {
            switch (col)
            {
                case 0: return m.TiempoDiaSeg;
                case 1: return m.AircraftAddressHex ?? "";
                case 2: return m.AircraftIdentification ?? "";
                case 3: return m.Mode3ACodigo;
                case 4: return m.FlightLevel ?? double.MinValue;
                case 5: return m.AltitudCorregidaQNH ?? m.AltitudEstandarFt ?? double.MinValue;
                case 6: return m.Latitud;
                case 7: return m.Longitud;
                case 8: return m.GroundSpeedKt;
                case 9: return m.Heading;
                case 10: return m.TrackNumber;
                default: return "";
            }
        }

        private static IComparable Clave021(MensajeCAT021 m, int col)
        {
            switch (col)
            {
                case 0: return m.TiempoRecepcionPosicion ?? double.MinValue;
                case 1: return m.AircraftAddressHex ?? "";
                case 2: return m.AircraftIdentification ?? "";
                case 3: return m.Mode3ACodigo ?? int.MinValue;
                case 4: return m.FlightLevel ?? double.MinValue;
                case 5: return m.AltitudCorregidaQNH ?? m.AltitudEstandarFt ?? double.MinValue;
                case 6: return m.Latitud ?? double.MinValue;
                case 7: return m.Longitud ?? double.MinValue;
                case 8: return m.GroundBitStatus.HasValue ? (m.GroundBitStatus.Value ? 1 : 0) : -1;
                default: return "";
            }
        }
    }
}
