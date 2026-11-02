#nullable disable
using System;
using ASTERIX_Decoder;   // Trayectoria, PuntoTrayectoria, TipoFuente

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>Estado de una aeronave en un instante concreto de la simulacion.</summary>
    internal sealed class EstadoAeronave
    {
        public double Latitud;
        public double Longitud;
        public double? AltitudFt;

        public double RumboIcono;        // grados; orientacion para dibujar el icono
        public double? HeadingReal;      // rumbo REALMENTE decodificado (solo CAT048)

        public double? GroundSpeedKt;
        public int? Mode3A;
        public string Callsign;
        public string Direccion;
        public TipoFuente Fuente;
        public int? TrackNumber;
        public double TiempoUltimoMensaje;   // TiempoSeg del ultimo punto real <= t
    }

    /// <summary>
    /// Envuelve una Trayectoria generada por GestorTrayectorias y calcula la
    /// posicion de la aeronave en cualquier instante mediante interpolacion
    /// lineal entre puntos reales. Nunca inventa rutas: solo interpola dentro de
    /// los segmentos existentes y respeta el intervalo de validez de la trayectoria.
    /// </summary>
    internal sealed class AeronaveSimulada
    {
        public Trayectoria Trayectoria { get; }
        public TipoFuente Fuente => Trayectoria.Fuente;
        public string Direccion => Trayectoria.AircraftAddress;
        public string Callsign => Trayectoria.AircraftIdentification;
        public int NumeroPuntos => Trayectoria.Puntos.Count;
        public double TiempoInicio => Trayectoria.TiempoInicio;
        public double TiempoFin => Trayectoria.TiempoFin;
        public string Etiqueta { get; }

        private readonly double[] _tiempos;

        public AeronaveSimulada(Trayectoria t)
        {
            Trayectoria = t;

            _tiempos = new double[t.Puntos.Count];
            for (int i = 0; i < _tiempos.Length; i++)
                _tiempos[i] = t.Puntos[i].TiempoSeg;

            Etiqueta = !string.IsNullOrWhiteSpace(t.AircraftIdentification)
                ? t.AircraftIdentification.Trim()
                : (string.IsNullOrEmpty(t.AircraftAddress) ? "----" : t.AircraftAddress);
        }

        /// <summary>La aeronave solo existe dentro de su intervalo temporal.</summary>
        public bool EstaActiva(double t) =>
            _tiempos.Length > 0 && t >= TiempoInicio && t <= TiempoFin;

        /// <summary>
        /// Calcula el estado interpolado en el instante t. Devuelve false si la
        /// aeronave no esta activa en ese instante (debe ocultarse).
        /// </summary>
        public bool TryEstado(double t, out EstadoAeronave estado)
        {
            estado = null;

            var puntos = Trayectoria.Puntos;
            if (puntos.Count == 0 || !EstaActiva(t))
                return false;

            int i = IndiceTramo(t);
            var p0 = puntos[i];

            // Si estamos en (o mas alla de) el ultimo punto conocido, se usa ese.
            if (i >= puntos.Count - 1)
            {
                estado = DesdePunto(p0, RumboDibujo(i, p0));
                return true;
            }

            var p1 = puntos[i + 1];
            double dt = p1.TiempoSeg - p0.TiempoSeg;

            // Sin salto temporal util: ultima posicion conocida.
            if (dt <= 0.0)
            {
                estado = DesdePunto(p0, RumboDibujo(i, p0));
                return true;
            }

            double f = (t - p0.TiempoSeg) / dt;
            if (f < 0) f = 0; else if (f > 1) f = 1;

            double lat = p0.Latitud + (p1.Latitud - p0.Latitud) * f;
            double lon = p0.Longitud + (p1.Longitud - p0.Longitud) * f;

            double? alt = (p0.AltitudFt.HasValue && p1.AltitudFt.HasValue)
                ? p0.AltitudFt.Value + (p1.AltitudFt.Value - p0.AltitudFt.Value) * f
                : (p0.AltitudFt ?? p1.AltitudFt);

            estado = new EstadoAeronave
            {
                Latitud = lat,
                Longitud = lon,
                AltitudFt = alt,
                Fuente = Fuente,

                // Los datos discretos proceden del ultimo punto REAL (p0).
                Callsign = p0.AircraftIdentification,
                Direccion = p0.AircraftAddress,
                Mode3A = p0.Mode3A,
                GroundSpeedKt = p0.GroundSpeedKt,
                HeadingReal = p0.Heading,
                TrackNumber = p0.TrackNumber,
                TiempoUltimoMensaje = p0.TiempoSeg,

                // Rumbo del icono: heading decodificado si existe; si no, el
                // rumbo geografico entre los dos puntos reales (NO es un heading).
                RumboIcono = p0.Heading ??
                    RumboGeografico(p0.Latitud, p0.Longitud, p1.Latitud, p1.Longitud)
            };

            return true;
        }

        private EstadoAeronave DesdePunto(PuntoTrayectoria p, double rumbo) => new EstadoAeronave
        {
            Latitud = p.Latitud,
            Longitud = p.Longitud,
            AltitudFt = p.AltitudFt,
            Fuente = Fuente,
            Callsign = p.AircraftIdentification,
            Direccion = p.AircraftAddress,
            Mode3A = p.Mode3A,
            GroundSpeedKt = p.GroundSpeedKt,
            HeadingReal = p.Heading,
            TrackNumber = p.TrackNumber,
            TiempoUltimoMensaje = p.TiempoSeg,
            RumboIcono = rumbo
        };

        private double RumboDibujo(int i, PuntoTrayectoria p)
        {
            if (p.Heading.HasValue)
                return p.Heading.Value;

            var puntos = Trayectoria.Puntos;
            if (i > 0)
                return RumboGeografico(puntos[i - 1].Latitud, puntos[i - 1].Longitud, p.Latitud, p.Longitud);
            if (i < puntos.Count - 1)
                return RumboGeografico(p.Latitud, p.Longitud, puntos[i + 1].Latitud, puntos[i + 1].Longitud);
            return 0.0;
        }

        /// <summary>Indice del ultimo punto cuyo tiempo es &lt;= t (busqueda binaria).</summary>
        private int IndiceTramo(double t)
        {
            if (t <= _tiempos[0]) return 0;
            int hi = _tiempos.Length - 1;
            if (t >= _tiempos[hi]) return hi;

            int lo = 0;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (_tiempos[mid] <= t) lo = mid + 1;
                else hi = mid - 1;
            }
            return hi;   // ultimo con tiempo <= t
        }

        /// <summary>Rumbo geografico inicial (grados, 0 = Norte) entre dos puntos.</summary>
        public static double RumboGeografico(double lat1, double lon1, double lat2, double lon2)
        {
            double lat1Rad = lat1 * Math.PI / 180.0;
            double lat2Rad = lat2 * Math.PI / 180.0;
            double dLonRad = (lon2 - lon1) * Math.PI / 180.0;

            double y = Math.Sin(dLonRad) * Math.Cos(lat2Rad);
            double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
                       Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLonRad);

            double grados = Math.Atan2(y, x) * 180.0 / Math.PI;
            return (grados + 360.0) % 360.0;
        }
    }
}
