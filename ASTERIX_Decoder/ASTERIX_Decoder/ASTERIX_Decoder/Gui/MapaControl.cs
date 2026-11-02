#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using ASTERIX_Decoder;   // TipoFuente, PuntoTrayectoria

namespace ASTERIX_Decoder_p1.Gui
{
    internal enum ModoTrayectoria
    {
        Ninguna,
        Todas,
        Seleccionada,
        RecorridoActual
    }

    /// <summary>
    /// Mapa interactivo (GMap.NET + OpenStreetMap) con los aviones. Reutiliza los
    /// marcadores entre fotogramas (no los reconstruye) y dibuja las trayectorias
    /// como lineas geograficas con el codigo de color Radar/ADS-B.
    /// </summary>
    internal sealed class MapaControl : UserControl
    {
        // Centro inicial: area de Barcelona (lat 40.9-41.7 N, lon 1.5-2.6 E).
        private static readonly PointLatLng CentroBarcelona = new PointLatLng(41.30, 2.08);
        private const int ZoomInicial = 9;

        private readonly GMapControl _mapa;
        private readonly GMapOverlay _capaRutas = new GMapOverlay("rutas");
        private readonly GMapOverlay _capaAviones = new GMapOverlay("aviones");

        private readonly Dictionary<AeronaveSimulada, MarcadorAvion> _marcadores =
            new Dictionary<AeronaveSimulada, MarcadorAvion>();

        private List<AeronaveSimulada> _aeronaves = new List<AeronaveSimulada>();
        private double _ultimoTiempo;
        private bool _rutasSucias = true;

        public bool MostrarRadar { get; set; } = true;
        public bool MostrarAdsb { get; set; } = true;
        public bool MostrarEtiquetas { get; set; } = true;
        public bool SeguirSeleccionada { get; set; }
        public ModoTrayectoria ModoTrayectorias { get; set; } = ModoTrayectoria.Seleccionada;

        public AeronaveSimulada Seleccionada { get; private set; }
        public int VisiblesActuales { get; private set; }

        /// <summary>Se dispara cuando el usuario pulsa un avion en el mapa.</summary>
        public event Action<AeronaveSimulada> AeronaveClic;

        public MapaControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Tema.Fondo;

            // Los servidores de OpenStreetMap bloquean (403 "Access blocked") las
            // peticiones de aplicaciones de escritorio como GMap.NET, incluso con
            // un User-Agent propio. Se usa un proveedor alternativo (CARTO, basado
            // en OSM) que permite este uso y combina con el tema oscuro.
            GMapProvider.UserAgent = "ASTERIX-Decoder-PGTA/1.0 (proyecto academico)";

            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            _mapa = new GMapControl
            {
                Dock = DockStyle.Fill,
                MapProvider = ProveedorMapaOscuro.Instance,
                Position = CentroBarcelona,
                MinZoom = 2,
                MaxZoom = 18,
                Zoom = ZoomInicial,
                DragButton = MouseButtons.Left,
                CanDragMap = true,
                MouseWheelZoomEnabled = true,
                MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter,
                ShowCenter = false,
                IgnoreMarkerOnMouseWheel = true,
                BackColor = Tema.Fondo
            };

            _mapa.Overlays.Add(_capaRutas);
            _mapa.Overlays.Add(_capaAviones);
            _mapa.OnMarkerClick += MapaMarkerClick;

            Controls.Add(_mapa);
        }

        // ----- Carga de datos -----

        public void CargarAeronaves(IEnumerable<AeronaveSimulada> aeronaves)
        {
            _capaAviones.Markers.Clear();
            _capaRutas.Routes.Clear();
            _marcadores.Clear();
            _aeronaves = aeronaves?.ToList() ?? new List<AeronaveSimulada>();
            Seleccionada = null;
            _rutasSucias = true;
        }

        public void Limpiar()
        {
            CargarAeronaves(null);
            _mapa.Invalidate();
        }

        // ----- Controles del mapa -----

        public void AcercarZoom() => _mapa.Zoom = Math.Min(_mapa.MaxZoom, (int)_mapa.Zoom + 1);
        public void AlejarZoom() => _mapa.Zoom = Math.Max(_mapa.MinZoom, (int)_mapa.Zoom - 1);

        public void CentrarBarcelona()
        {
            _mapa.Position = CentroBarcelona;
            _mapa.Zoom = ZoomInicial;
        }

        public void CentrarEnSeleccionada()
        {
            if (Seleccionada == null) return;
            if (_marcadores.TryGetValue(Seleccionada, out var m))
            {
                _mapa.Position = m.Position;
                if (_mapa.Zoom < 11) _mapa.Zoom = 11;
            }
            else if (Seleccionada.NumeroPuntos > 0)
            {
                var p = Seleccionada.Trayectoria.Puntos[0];
                _mapa.Position = new PointLatLng(p.Latitud, p.Longitud);
            }
        }

        // ----- Seleccion -----

        public void Seleccionar(AeronaveSimulada a, bool centrar = false)
        {
            Seleccionada = a;
            _rutasSucias = true;
            if (centrar) CentrarEnSeleccionada();
            Actualizar(_ultimoTiempo);
        }

        // ----- Bucle de actualizacion -----

        /// <summary>Actualiza marcadores y trayectorias para el instante t.</summary>
        public void Actualizar(double t)
        {
            _ultimoTiempo = t;

            var activas = new HashSet<AeronaveSimulada>();

            foreach (var a in _aeronaves)
            {
                if (a.Fuente == TipoFuente.Radar && !MostrarRadar) continue;
                if (a.Fuente == TipoFuente.Adsb && !MostrarAdsb) continue;
                if (!a.TryEstado(t, out var e)) continue;

                activas.Add(a);

                if (!_marcadores.TryGetValue(a, out var m))
                {
                    m = new MarcadorAvion(new PointLatLng(e.Latitud, e.Longitud)) { Aeronave = a };
                    _marcadores[a] = m;
                    _capaAviones.Markers.Add(m);
                }
                else
                {
                    m.Position = new PointLatLng(e.Latitud, e.Longitud);
                }

                bool sel = ReferenceEquals(a, Seleccionada);
                m.Rumbo = e.RumboIcono;
                m.Etiqueta = a.Etiqueta;
                m.MostrarEtiqueta = MostrarEtiquetas;
                m.Seleccionado = sel;
                m.Color = sel
                    ? Tema.Seleccion
                    : (a.Fuente == TipoFuente.Radar ? Tema.Radar : Tema.Adsb);
            }

            // Retirar marcadores de aeronaves que ya no estan activas.
            if (_marcadores.Count > activas.Count)
            {
                var retirar = _marcadores.Keys.Where(k => !activas.Contains(k)).ToList();
                foreach (var k in retirar)
                {
                    _capaAviones.Markers.Remove(_marcadores[k]);
                    _marcadores.Remove(k);
                }
            }

            VisiblesActuales = activas.Count;

            ActualizarRutas(t);

            if (SeguirSeleccionada && Seleccionada != null &&
                _marcadores.TryGetValue(Seleccionada, out var sm))
            {
                _mapa.Position = sm.Position;
            }

            _mapa.Invalidate();
        }

        private void ActualizarRutas(double t)
        {
            // Las rutas estaticas solo se reconstruyen cuando cambian las opciones.
            // El "recorrido hasta el instante actual" se recalcula en cada fotograma.
            if (ModoTrayectorias != ModoTrayectoria.RecorridoActual && !_rutasSucias)
                return;

            _capaRutas.Routes.Clear();
            _rutasSucias = false;

            switch (ModoTrayectorias)
            {
                case ModoTrayectoria.Ninguna:
                    break;

                case ModoTrayectoria.Todas:
                    foreach (var a in _aeronaves)
                    {
                        if (a.Fuente == TipoFuente.Radar && !MostrarRadar) continue;
                        if (a.Fuente == TipoFuente.Adsb && !MostrarAdsb) continue;
                        bool sel = ReferenceEquals(a, Seleccionada);
                        AnadirRutaCompleta(a, sel ? 180 : 60, sel ? 3f : 1.4f, sel);
                    }
                    break;

                case ModoTrayectoria.Seleccionada:
                    if (Seleccionada != null)
                        AnadirRutaCompleta(Seleccionada, 200, 3f, true);
                    break;

                case ModoTrayectoria.RecorridoActual:
                    if (Seleccionada != null)
                        AnadirRutaRecorrida(Seleccionada, t);
                    break;
            }
        }

        private Color ColorRuta(AeronaveSimulada a, bool seleccionada)
        {
            if (seleccionada) return Tema.Seleccion;
            return a.Fuente == TipoFuente.Radar ? Tema.Radar : Tema.Adsb;
        }

        private void AnadirRutaCompleta(AeronaveSimulada a, int alpha, float grosor, bool seleccionada)
        {
            var puntos = a.Trayectoria.Puntos;
            if (puntos.Count < 2) return;

            var geo = new List<PointLatLng>(puntos.Count);
            foreach (var p in puntos)
                geo.Add(new PointLatLng(p.Latitud, p.Longitud));

            AnadirRuta(geo, Color.FromArgb(alpha, ColorRuta(a, seleccionada)), grosor,
                "r_" + a.Direccion + "_" + a.TiempoInicio);
        }

        private void AnadirRutaRecorrida(AeronaveSimulada a, double t)
        {
            var puntos = a.Trayectoria.Puntos;
            var geo = new List<PointLatLng>();

            foreach (var p in puntos)
            {
                if (p.TiempoSeg <= t)
                    geo.Add(new PointLatLng(p.Latitud, p.Longitud));
                else
                    break;
            }

            // Anade la posicion interpolada actual como ultimo vertice.
            if (a.TryEstado(t, out var e))
                geo.Add(new PointLatLng(e.Latitud, e.Longitud));

            if (geo.Count < 2) return;
            AnadirRuta(geo, Color.FromArgb(220, ColorRuta(a, true)), 3f, "recorrido");
        }

        private void AnadirRuta(List<PointLatLng> puntos, Color color, float grosor, string nombre)
        {
            var ruta = new GMapRoute(puntos, nombre);
            var lapiz = new Pen(color, grosor) { LineJoin = LineJoin.Round };
            ruta.Stroke = lapiz;
            _capaRutas.Routes.Add(ruta);
        }

        // ----- Eventos -----

        private void MapaMarkerClick(GMapMarker item, MouseEventArgs e)
        {
            if (item is MarcadorAvion m && m.Aeronave != null)
            {
                Seleccionar(m.Aeronave);
                AeronaveClic?.Invoke(m.Aeronave);
            }
        }

        /// <summary>Fuerza la reconstruccion de rutas tras cambiar opciones de visualizacion.</summary>
        public void MarcarRutasSucias()
        {
            _rutasSucias = true;
            Actualizar(_ultimoTiempo);
        }
    }
}
