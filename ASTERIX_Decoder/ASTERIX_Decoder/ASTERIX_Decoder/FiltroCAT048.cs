using System.Collections.Generic;

namespace ASTERIX_Decoder_p1
{
    internal class FiltroCAT048
    {
        // Límites geográficos del área de estudio.
        const double AREA_LAT_MIN = 40.9;
        const double AREA_LAT_MAX = 41.7;
        const double AREA_LON_MIN = 1.5;
        const double AREA_LON_MAX = 2.6;

        // Filtros configurables.
        public bool EliminarFlNoValidado;
        public bool EliminarFlGarbled;
        public bool EliminarFueraDelArea;
        public bool EliminarBlancoPuro;
        public bool EliminarTransponderFijo;
        public bool EliminarEnTierra;

        public bool HayFiltrosActivos =>
            EliminarFlNoValidado ||
            EliminarFlGarbled ||
            EliminarFueraDelArea ||
            EliminarBlancoPuro ||
            EliminarTransponderFijo ||
            EliminarEnTierra;

        // Comprueba si el mensaje está dentro del área geográfica definida.
        static bool DentroDelAreaGeografica(MensajeCAT048 m)
        {
            return
                m.Latitud >= AREA_LAT_MIN &&
                m.Latitud <= AREA_LAT_MAX &&
                m.Longitud >= AREA_LON_MIN &&
                m.Longitud <= AREA_LON_MAX;
        }

        // Determina si el blanco está reportado como en tierra.
        static bool EstaEnTierra(MensajeCAT048 m)
        {
            return m.Stat == 1 || m.Stat == 3;
        }

        // Aplica los filtros activos y devuelve una nueva lista.
        public List<MensajeCAT048> Aplicar(MensajeCAT048[] mensajes)
        {
            var resultado =
                new List<MensajeCAT048>(mensajes.Length);

            foreach (var m in mensajes)
            {
                if (EliminarFlNoValidado && m.FlV == 1)
                    continue;

                if (EliminarFlGarbled && m.FlG == 1)
                    continue;

                if (EliminarFueraDelArea &&
                    !DentroDelAreaGeografica(m))
                    continue;

                if (EliminarBlancoPuro &&
                    m.Typ >= 0 &&
                    m.Typ <= 3)
                    continue;

                if (EliminarTransponderFijo &&
                    m.Mode3ACodigo == 7777)
                    continue;

                if (EliminarEnTierra &&
                    EstaEnTierra(m))
                    continue;

                resultado.Add(m);
            }

            return resultado;
        }

        // Devuelve el motivo por el que se descartaría el mensaje.
        public string MotivoDescarte(MensajeCAT048 m)
        {
            var motivos = new List<string>();

            if (EliminarFlNoValidado && m.FlV == 1)
                motivos.Add("FL_no_validado");

            if (EliminarFlGarbled && m.FlG == 1)
                motivos.Add("FL_garbled");

            if (EliminarFueraDelArea &&
                !DentroDelAreaGeografica(m))
                motivos.Add("Fuera_del_area");

            if (EliminarBlancoPuro &&
                m.Typ >= 0 &&
                m.Typ <= 3)
                motivos.Add("Blanco_puro");

            if (EliminarTransponderFijo &&
                m.Mode3ACodigo == 7777)
                motivos.Add("Transponder_fijo");

            if (EliminarEnTierra &&
                EstaEnTierra(m))
                motivos.Add("En_tierra_STAT");

            return string.Join("+", motivos);
        }
    }
}