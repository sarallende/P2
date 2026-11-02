using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASTERIX_Decoder
{
    internal enum TipoFuente
    {
        Radar,
        Adsb
    }

    internal class PuntoTrayectoria
    {
        // Tiempo común para la simulación, en segundos desde medianoche.
        public double TiempoSeg;

        // Posición del blanco.
        public double Latitud;
        public double Longitud;
        public double? AltitudFt;

        // Identificación.
        public string AircraftAddress;
        public string AircraftIdentification;
        public int? Mode3A;

        // Fuente del dato.
        public TipoFuente Fuente;

        // Información adicional útil.
        public double? GroundSpeedKt;
        public double? Heading;
        public int? TrackNumber;
    }
}
