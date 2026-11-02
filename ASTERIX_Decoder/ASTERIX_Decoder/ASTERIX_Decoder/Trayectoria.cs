using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ASTERIX_Decoder
{
    internal class Trayectoria
    {
        // Identificación del vuelo.
        public string AircraftAddress;
        public string AircraftIdentification;

        // Fuente de los datos.
        public TipoFuente Fuente;

        // Puntos que forman la trayectoria.
        public List<PuntoTrayectoria> Puntos =
            new List<PuntoTrayectoria>();

        // Tiempo inicial y final de la trayectoria.
        public double TiempoInicio =>
            Puntos.Count > 0
                ? Puntos[0].TiempoSeg
                : 0;

        public double TiempoFin =>
            Puntos.Count > 0
                ? Puntos[Puntos.Count - 1].TiempoSeg
                : 0;
    }
}
