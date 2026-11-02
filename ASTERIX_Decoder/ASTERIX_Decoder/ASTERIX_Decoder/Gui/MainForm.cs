#nullable disable
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ASTERIX_Decoder;   // Trayectoria

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Ventana principal. Integra la barra superior, el menu lateral, el mapa con
    /// la simulacion, las tablas, los filtros y las estadisticas. Orquesta la
    /// carga de ficheros reutilizando AsterixProcessingService (que a su vez usa
    /// las clases del nucleo) y conecta todos los eventos.
    /// </summary>
    internal sealed class MainForm : Form
    {
        private readonly AsterixProcessingService _servicio = new AsterixProcessingService();
        private readonly SimuladorTrafico _sim = new SimuladorTrafico();

        private readonly VistaMapa _vistaMapa = new VistaMapa();
        private readonly VistaTrayectorias _vistaTrayectorias = new VistaTrayectorias();
        private readonly VistaFiltros _vistaFiltros = new VistaFiltros();
        private readonly VistaTabla _vistaTabla048 = new VistaTabla(true);
        private readonly VistaTabla _vistaTabla021 = new VistaTabla(false);
        private readonly VistaEstadisticas _vistaStats = new VistaEstadisticas();
        private readonly BarraReproduccion _barra;

        private readonly Panel _host = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Fondo };
        private readonly System.Collections.Generic.Dictionary<Control, Button> _menu =
            new System.Collections.Generic.Dictionary<Control, Button>();

        private readonly Label _nombreArchivo = new Label();
        private readonly Label _estado = new Label();
        private Button _btnAbrir;
        private Button _btnExportar;
        private ContextMenuStrip _menuExportar;

        private DatosAsterix _datos;
        private long _msDecodificacion;

        public MainForm()
        {
            Text = "ASTERIX Decoder - PGTA";
            BackColor = Tema.Fondo;
            Font = Tema.Fuente;
            MinimumSize = new Size(1150, 720);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;   // se abre maximizada

            _barra = new BarraReproduccion(_sim);

            // Vistas en el area central (solo una visible a la vez).
            foreach (var v in new Control[] { _vistaMapa, _vistaTrayectorias, _vistaFiltros,
                                              _vistaTabla048, _vistaTabla021, _vistaStats })
            {
                v.Dock = DockStyle.Fill;
                v.Visible = false;
                _host.Controls.Add(v);
            }

            // Orden de anadido (z-order) para que el dock reparta bien el espacio.
            Controls.Add(_host);
            Controls.Add(_barra);
            Controls.Add(ConstruirMenuLateral());
            Controls.Add(ConstruirBarraSuperior());

            CablearEventos();

            MostrarVista(_vistaMapa);
            _vistaMapa.MostrarMensajeInicial(true);
        }

        // ============================================================
        // BARRA SUPERIOR
        // ============================================================
        private Control ConstruirBarraSuperior()
        {
            var topbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Tema.Panel };

            // --- Logo (izquierda) ---
            var izquierda = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                WrapContents = false,
                BackColor = Tema.Panel,
                Padding = new Padding(16, 0, 0, 0)
            };
            var logo = new Label
            {
                Text = "ASTERIX DECODER",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Tema.AcentoCian,
                AutoSize = true,
                Margin = new Padding(0, 15, 10, 0)
            };
            var tag = new Label
            {
                Text = "  PGTA  ",
                Font = Tema.FuentePequena,
                ForeColor = Color.White,
                BackColor = Tema.AcentoAzul,
                AutoSize = true,
                Margin = new Padding(0, 20, 0, 0),
                Padding = new Padding(2, 3, 2, 3)
            };
            izquierda.Controls.Add(logo);
            izquierda.Controls.Add(tag);

            // --- Acciones (derecha) ---
            var derecha = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Tema.Panel,
                Padding = new Padding(0, 0, 16, 0)
            };
            _btnAbrir = new Button { Text = "Abrir archivo", Size = new Size(130, 34), Margin = new Padding(6, 13, 0, 0) };
            Tema.EstiloBoton(_btnAbrir, acento: true);
            _btnAbrir.Click += AbrirArchivoAsync;

            _btnExportar = new Button { Text = "Exportar CSV", Size = new Size(130, 34), Margin = new Padding(8, 13, 0, 0), Enabled = false };
            Tema.EstiloBoton(_btnExportar);
            _menuExportar = ConstruirMenuExportar();
            _btnExportar.Click += (s, e) => _menuExportar.Show(_btnExportar, new Point(0, _btnExportar.Height));

            var btnConfig = new Button { Text = "Configuracion", Size = new Size(130, 34), Margin = new Padding(8, 13, 0, 0) };
            Tema.EstiloBoton(btnConfig);
            btnConfig.Click += (s, e) => MostrarConfiguracion();

            derecha.Controls.Add(_btnAbrir);
            derecha.Controls.Add(_btnExportar);
            derecha.Controls.Add(btnConfig);

            // --- Centro (archivo + estado) ---
            var centro = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Panel };
            _nombreArchivo.Text = "Sin archivo cargado";
            _nombreArchivo.Font = Tema.FuenteFuerte;
            _nombreArchivo.ForeColor = Tema.Texto;
            _nombreArchivo.AutoSize = true;
            _estado.Text = "Abre un fichero ASTERIX para empezar";
            _estado.Font = Tema.FuentePequena;
            _estado.ForeColor = Tema.TextoTenue;
            _estado.AutoSize = true;
            centro.Controls.Add(_nombreArchivo);
            centro.Controls.Add(_estado);
            centro.Resize += (s, e) =>
            {
                _nombreArchivo.Location = new Point(24, 12);
                _estado.Location = new Point(24, 33);
            };

            topbar.Controls.Add(centro);
            topbar.Controls.Add(derecha);
            topbar.Controls.Add(izquierda);
            return topbar;
        }

        private ContextMenuStrip ConstruirMenuExportar()
        {
            var m = new ContextMenuStrip();
            m.Items.Add("CAT048 - todos los mensajes", null, (s, e) =>
                { if (Hay()) ExportacionCsv.GuardarCat048(this, _datos.Mensajes048, _datos.Filtro048, "salida_CAT048.csv"); });
            m.Items.Add("CAT048 - solo filtrados", null, (s, e) =>
                { if (Hay()) ExportacionCsv.GuardarCat048(this, _datos.Mensajes048Filtrados, _datos.Filtro048, "salida_CAT048_filtro.csv"); });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("CAT021 - todos los mensajes", null, (s, e) =>
                { if (Hay()) ExportacionCsv.GuardarCat021(this, _datos.Mensajes021, _datos.Filtro021, "salida_CAT021.csv"); });
            m.Items.Add("CAT021 - solo filtrados", null, (s, e) =>
                { if (Hay()) ExportacionCsv.GuardarCat021(this, _datos.Mensajes021Filtrados, _datos.Filtro021, "salida_CAT021_filtro.csv"); });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Ambas categorias - todos", null, (s, e) => { if (Hay()) ExportacionCsv.GuardarAmbas(this, _datos, false); });
            m.Items.Add("Ambas categorias - solo filtrados", null, (s, e) => { if (Hay()) ExportacionCsv.GuardarAmbas(this, _datos, true); });
            return m;
        }

        private bool Hay() => _datos != null;

        // ============================================================
        // MENU LATERAL
        // ============================================================
        private Control ConstruirMenuLateral()
        {
            var panel = new Panel { Dock = DockStyle.Left, Width = 210, BackColor = Tema.Panel };

            var cabecera = new Label
            {
                Text = "NAVEGACION",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                Font = Tema.FuentePequena,
                ForeColor = Tema.TextoTenue
            };

            var items = new (string Texto, Control Vista)[]
            {
                ("  Mapa / Simulacion", _vistaMapa),
                ("  Datos CAT048", _vistaTabla048),
                ("  Datos CAT021", _vistaTabla021),
                ("  Trayectorias", _vistaTrayectorias),
                ("  Filtros", _vistaFiltros),
                ("  Estadisticas", _vistaStats)
            };

            // Se anaden en orden inverso porque Dock=Top apila el ultimo arriba.
            for (int i = items.Length - 1; i >= 0; i--)
            {
                var it = items[i];
                var b = CrearBotonMenu(it.Texto, it.Vista);
                _menu[it.Vista] = b;
                panel.Controls.Add(b);
            }

            panel.Controls.Add(cabecera);
            return panel;
        }

        private Button CrearBotonMenu(string texto, Control vista)
        {
            var b = new Button
            {
                Text = texto,
                Dock = DockStyle.Top,
                Height = 46,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = Tema.FuenteFuerte,
                ForeColor = Tema.Texto,
                BackColor = Tema.Panel,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Tema.Elevado;
            b.Click += (s, e) => MostrarVista(vista);
            return b;
        }

        private void MostrarVista(Control vista)
        {
            foreach (Control c in _host.Controls)
                c.Visible = ReferenceEquals(c, vista);
            vista.BringToFront();

            foreach (var kv in _menu)
            {
                bool activo = ReferenceEquals(kv.Key, vista);
                kv.Value.BackColor = activo ? Tema.Elevado : Tema.Panel;
                kv.Value.ForeColor = activo ? Tema.AcentoCian : Tema.Texto;
            }
        }

        // ============================================================
        // EVENTOS
        // ============================================================
        private void CablearEventos()
        {
            // El reloj de simulacion mueve el mapa y actualiza el contador visible.
            _sim.TiempoCambiado += t =>
            {
                _vistaMapa.Refrescar(t);
                _vistaStats.ActualizarVisibles(_vistaMapa.Mapa.VisiblesActuales);
            };

            // Reaplicar filtros sin volver a decodificar.
            _vistaFiltros.FiltrosAplicados += (f048, f021) =>
            {
                if (!_servicio.HayDatos) return;
                try
                {
                    var datos = _servicio.ReaplicarFiltros(f048, f021);
                    AplicarDatos(datos);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "Error al aplicar filtros:\n" + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Elegir una trayectoria la muestra en el mapa.
            _vistaTrayectorias.TrayectoriaElegida += a =>
            {
                MostrarVista(_vistaMapa);
                _vistaMapa.SeleccionarExterna(a);
            };
        }

        private async void AbrirArchivoAsync(object sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Abrir archivo(s) ASTERIX",
                Filter = "ASTERIX (*.ast)|*.ast|Todos los archivos (*.*)|*.*",
                Multiselect = true
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _btnAbrir.Enabled = false;
            _estado.ForeColor = Tema.TextoTenue;

            var f048 = _vistaFiltros.ConstruirFiltro048();
            var f021 = _vistaFiltros.ConstruirFiltro021();
            var progreso = new Progress<ProgresoCarga>(p => _estado.Text = p.Etapa);

            try
            {
                var datos = await _servicio.CargarAsync(dlg.FileNames, f048, f021, progreso);
                _msDecodificacion = datos.Resumen.MilisegundosProcesado;

                _nombreArchivo.Text = $"{datos.Resumen.NombreArchivo}   ({Formato.Tamano(datos.Resumen.TamanoBytes)})";
                _estado.Text = $"CAT048: {datos.Resumen.MensajesCat048:N0}   |   CAT021: {datos.Resumen.MensajesCat021:N0}   |   {datos.Resumen.MilisegundosProcesado:N0} ms";

                AplicarDatos(datos);
                _vistaMapa.MostrarMensajeInicial(false);
                _btnExportar.Enabled = true;
            }
            catch (Exception ex)
            {
                _estado.Text = "Error al procesar el archivo";
                MessageBox.Show(this, "No se pudo procesar el archivo:\n\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnAbrir.Enabled = true;
            }
        }

        /// <summary>Aplica un conjunto de datos (tras cargar o tras reaplicar filtros).</summary>
        private void AplicarDatos(DatosAsterix datos)
        {
            _datos = datos;

            var aeronaves = datos.TrayectoriasRadar
                .Concat(datos.TrayectoriasAdsb)
                .Select(t => new AeronaveSimulada(t))
                .ToList();

            // El mapa debe conocer las aeronaves antes de que el reloj emita tiempos.
            _vistaMapa.Mapa.CargarAeronaves(aeronaves);
            _sim.Cargar(aeronaves);          // reinicia el reloj al instante inicial
            _vistaMapa.Mapa.CentrarBarcelona();
            _vistaMapa.Refrescar(_sim.TiempoActual);

            _vistaTrayectorias.Cargar(aeronaves);
            _vistaTabla048.CargarCat048(datos.Mensajes048Filtrados, datos.Filtro048);
            _vistaTabla021.CargarCat021(datos.Mensajes021Filtrados, datos.Filtro021);

            _vistaFiltros.ActualizarContadores(
                datos.Mensajes048.Length, datos.Mensajes048Filtrados.Count,
                datos.Mensajes021.Length, datos.Mensajes021Filtrados.Count);

            _vistaStats.Actualizar(datos, _msDecodificacion, _sim.TiempoMin, _sim.TiempoMax);
            _vistaStats.ActualizarVisibles(_vistaMapa.Mapa.VisiblesActuales);
        }

        private void MostrarConfiguracion()
        {
            MessageBox.Show(this,
                "ASTERIX Decoder - PGTA (P2)\n\n" +
                "Mapa: OpenStreetMap (GMap.NET).\n" +
                "Area de estudio: lat 40.9-41.7 N, lon 1.5-2.6 E (Barcelona).\n" +
                "Fuentes: CAT048 (Radar) y CAT021 (ADS-B).\n\n" +
                "Velocidades de simulacion: 0.5x, 1x, 2x, 5x, 10x, 20x.\n" +
                "La simulacion usa las trayectorias generadas por el nucleo del proyecto.",
                "Configuracion / Acerca de",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
