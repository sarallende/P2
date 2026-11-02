using System.Collections.Generic;

namespace ASTERIX_Decoder_p1
{
    internal class FiltroCAT021
    {
        // Límites geográficos del área de estudio.
        const double AREA_LAT_MIN = 40.9;
        const double AREA_LAT_MAX = 41.7;
        const double AREA_LON_MIN = 1.5;
        const double AREA_LON_MAX = 2.6;

        // Filtros configurables.
        public bool EliminarEnTierra;
        public bool EliminarFueraDelArea;
        public bool EliminarTransponderFijo;

        public bool HayFiltrosActivos =>
            EliminarEnTierra ||
            EliminarFueraDelArea ||
            EliminarTransponderFijo;

        // Comprueba si el mensaje está dentro del área geográfica definida.
        static bool DentroDelAreaGeografica(MensajeCAT021 m)
        {
            return
                m.Latitud.HasValue &&
                m.Longitud.HasValue &&
                m.Latitud.Value >= AREA_LAT_MIN &&
                m.Latitud.Value <= AREA_LAT_MAX &&
                m.Longitud.Value >= AREA_LON_MIN &&
                m.Longitud.Value <= AREA_LON_MAX;
        }

        // Aplica los filtros activos y devuelve una nueva lista.
        public List<MensajeCAT021> Aplicar(MensajeCAT021[] mensajes)
        {
            var resultado =
                new List<MensajeCAT021>(mensajes.Length);

            foreach (var m in mensajes)
            {
                if (EliminarEnTierra &&
                    m.GroundBitStatus == true)
                    continue;

                if (EliminarFueraDelArea &&
                    !DentroDelAreaGeografica(m))
                    continue;

                if (EliminarTransponderFijo &&
                    m.Mode3ACodigo == 7777)
                    continue;

                resultado.Add(m);
            }

            return resultado;
        }

        // Devuelve el motivo por el que se descartaría el mensaje.
        public string MotivoDescarte(MensajeCAT021 m)
        {
            var motivos = new List<string>();

            if (EliminarEnTierra &&
                m.GroundBitStatus == true)
                motivos.Add("EnTierra_GBS");

            if (EliminarFueraDelArea &&
                !DentroDelAreaGeografica(m))
                motivos.Add("Fuera_del_area");

            if (EliminarTransponderFijo &&
                m.Mode3ACodigo == 7777)
                motivos.Add("Transponder_fijo");

            return string.Join("+", motivos);
        }
    }
}