using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASTERIX_Decoder_p1;

namespace ASTERIX_Decoder
{
    internal class GestorTrayectorias
    {
        // Si pasan más de 120 segundos entre dos puntos del mismo avión,
        // consideramos que empieza una nueva trayectoria.
        const double MAX_HUECO_TRAYECTORIA = 120.0;


        // ============================================================
        // CONVERSIÓN CAT048 -> PUNTOS DE TRAYECTORIA
        // ============================================================
        //
        // Recibe los mensajes radar que ya han sido:
        // - decodificados
        // - corregidos en altitud
        // - convertidos a coordenadas WGS84
        // - filtrados
        //
        // Los transforma al formato común PuntoTrayectoria para que,
        // a partir de aquí, la simulación no tenga que conocer CAT048.
        public static List<Trayectoria> CrearDesdeCAT048(
            IEnumerable<MensajeCAT048> mensajes)
        {
            var puntos = new List<PuntoTrayectoria>();

            foreach (var msg in mensajes)
            {
                // Sin Aircraft Address no podemos asociar el punto
                // de forma suficientemente fiable a una aeronave.
                if (string.IsNullOrEmpty(msg.AircraftAddressHex))
                    continue;

                // Elegimos la mejor altitud disponible.
                // Por debajo de 6000 ft será normalmente la corregida con QNH.
                // Si no existe, utilizamos la altitud estándar.
                double? altitud =
                    msg.AltitudCorregidaQNH ??
                    msg.AltitudEstandarFt;

                // Convertimos el MensajeCAT048 al formato común.
                puntos.Add(
                    new PuntoTrayectoria
                    {
                        // Tiempo en segundos desde medianoche.
                        TiempoSeg = msg.TiempoDiaSeg,

                        // Posición WGS84 calculada previamente.
                        Latitud = msg.Latitud,
                        Longitud = msg.Longitud,

                        AltitudFt = altitud,

                        // Identificación de la aeronave.
                        AircraftAddress =
                            msg.AircraftAddressHex,

                        AircraftIdentification =
                            msg.AircraftIdentification,

                        Mode3A =
                            msg.Mode3ACodigo,

                        // Indicamos que este punto procede del radar.
                        Fuente =
                            TipoFuente.Radar,

                        // Datos disponibles en CAT048 que pueden ser útiles
                        // posteriormente al mostrar información del avión.
                        GroundSpeedKt =
                            msg.GroundSpeedKt,

                        Heading =
                            msg.Heading,

                        TrackNumber =
                            msg.TrackNumber
                    }
                );
            }

            // Una vez tenemos todos los puntos normalizados,
            // los agrupamos para construir vuelos/trayectorias.
            return CrearTrayectorias(puntos);
        }


        // ============================================================
        // CONVERSIÓN CAT021 -> PUNTOS DE TRAYECTORIA
        // ============================================================
        //
        // Hace lo mismo que el método anterior, pero partiendo de ADS-B.
        // El resultado final tiene exactamente el mismo formato común.
        public static List<Trayectoria> CrearDesdeCAT021(
            IEnumerable<MensajeCAT021> mensajes)
        {
            var puntos = new List<PuntoTrayectoria>();

            foreach (var msg in mensajes)
            {
                // Necesitamos un Aircraft Address para asociar el punto.
                if (string.IsNullOrEmpty(msg.AircraftAddressHex))
                    continue;

                // Para representar una posición necesitamos necesariamente
                // latitud, longitud y tiempo.
                if (!msg.Latitud.HasValue ||
                    !msg.Longitud.HasValue ||
                    !msg.TiempoRecepcionPosicion.HasValue)
                    continue;

                // Igual que en CAT048, elegimos la mejor altitud disponible.
                double? altitud =
                    msg.AltitudCorregidaQNH ??
                    msg.AltitudEstandarFt;

                // Convertimos el mensaje ADS-B al formato común.
                puntos.Add(
                    new PuntoTrayectoria
                    {
                        TiempoSeg =
                            msg.TiempoRecepcionPosicion.Value,

                        Latitud =
                            msg.Latitud.Value,

                        Longitud =
                            msg.Longitud.Value,

                        AltitudFt =
                            altitud,

                        AircraftAddress =
                            msg.AircraftAddressHex,

                        AircraftIdentification =
                            msg.AircraftIdentification,

                        Mode3A =
                            msg.Mode3ACodigo,

                        // Indicamos que este punto procede de ADS-B.
                        Fuente =
                            TipoFuente.Adsb,

                        // Actualmente estos datos no se decodifican
                        // en nuestra CAT021, por lo que quedan vacíos.
                        GroundSpeedKt =
                            null,

                        Heading =
                            null,

                        TrackNumber =
                            null
                    }
                );
            }

            // Construimos las trayectorias a partir de los puntos ADS-B.
            return CrearTrayectorias(puntos);
        }


        // ============================================================
        // CREACIÓN DE TRAYECTORIAS
        // ============================================================
        //
        // Recibe una lista de puntos ya normalizados y decide cuáles
        // pertenecen al mismo vuelo.
        private static List<Trayectoria> CrearTrayectorias(
            List<PuntoTrayectoria> puntos)
        {
            var resultado = new List<Trayectoria>();

            // Primero agrupamos por Aircraft Address y por fuente.
            //
            // La fuente también forma parte del grupo para evitar fusionar
            // una trayectoria Radar y una ADS-B aunque sean del mismo avión.
            var grupos =
                puntos
                .GroupBy(
                    p => new
                    {
                        p.AircraftAddress,
                        p.Fuente
                    }
                );

            // Procesamos cada aeronave/fuente por separado.
            foreach (var grupo in grupos)
            {
                // Ordenamos todos sus puntos cronológicamente.
                var puntosOrdenados =
                    grupo
                    .OrderBy(p => p.TiempoSeg)
                    .ToList();

                // "actual" representa la trayectoria que estamos construyendo.
                Trayectoria actual = null;

                // Guardamos el punto anterior para calcular el hueco temporal.
                PuntoTrayectoria puntoAnterior = null;

                foreach (var punto in puntosOrdenados)
                {
                    // Si todavía no hay trayectoria, este punto inicia una.
                    bool nuevaTrayectoria =
                        actual == null;

                    if (!nuevaTrayectoria &&
                        puntoAnterior != null)
                    {
                        // Calculamos el tiempo transcurrido respecto
                        // al punto anterior del mismo avión.
                        double hueco =
                            punto.TiempoSeg -
                            puntoAnterior.TiempoSeg;

                        // Si pasan más de 120 segundos sin mensajes,
                        // consideramos que empieza otra trayectoria.
                        if (hueco > MAX_HUECO_TRAYECTORIA)
                            nuevaTrayectoria = true;

                        // Si tenemos dos callsigns distintos para el mismo
                        // Aircraft Address, consideramos que es otro vuelo.
                        if (CambioIdentificacion(
                            actual.AircraftIdentification,
                            punto.AircraftIdentification))
                        {
                            nuevaTrayectoria = true;
                        }
                    }

                    // Creamos una nueva trayectoria cuando corresponde.
                    if (nuevaTrayectoria)
                    {
                        actual =
                            new Trayectoria
                            {
                                AircraftAddress =
                                    punto.AircraftAddress,

                                AircraftIdentification =
                                    punto.AircraftIdentification,

                                Fuente =
                                    punto.Fuente
                            };

                        resultado.Add(actual);
                    }

                    // Puede ocurrir que los primeros mensajes no tengan
                    // Aircraft Identification y aparezca posteriormente.
                    // En ese caso completamos la trayectoria existente
                    // en lugar de crear una nueva.
                    else if (
                        string.IsNullOrEmpty(
                            actual.AircraftIdentification
                        ) &&
                        !string.IsNullOrEmpty(
                            punto.AircraftIdentification
                        )
                    )
                    {
                        actual.AircraftIdentification =
                            punto.AircraftIdentification;
                    }

                    // Añadimos el punto a la trayectoria correspondiente.
                    actual.Puntos.Add(punto);

                    // Este punto pasa a ser la referencia para el siguiente.
                    puntoAnterior = punto;
                }
            }

            return resultado;
        }


        // ============================================================
        // CAMBIO DE IDENTIFICACIÓN
        // ============================================================
        //
        // Comprueba si dos Aircraft Identification distintos indican
        // que debemos separar dos vuelos del mismo Aircraft Address.
        private static bool CambioIdentificacion(
            string identificacionActual,
            string identificacionNueva)
        {
            // Si alguna identificación está vacía no podemos afirmar
            // que haya cambiado el vuelo, así que mantenemos la trayectoria.
            if (string.IsNullOrEmpty(identificacionActual) ||
                string.IsNullOrEmpty(identificacionNueva))
            {
                return false;
            }

            // Si ambas existen y son diferentes, se considera otro vuelo.
            return !string.Equals(
                identificacionActual,
                identificacionNueva,
                StringComparison.OrdinalIgnoreCase
            );
        }


        // ============================================================
        // VALIDACIÓN DE TRAYECTORIAS
        // ============================================================
        //
        // Este método se utiliza durante el desarrollo para comprobar
        // automáticamente que todas las trayectorias creadas son coherentes.
        public static void ValidarTrayectorias(
            List<Trayectoria> trayectorias,
            string nombreFuente)
        {
            int correctas = 0;
            int conErrores = 0;

            Console.WriteLine();
            Console.WriteLine($"VALIDACIÓN {nombreFuente}");
            Console.WriteLine("------------------------------------");

            foreach (var trayectoria in trayectorias)
            {
                bool valida = true;

                // Una trayectoria debe contener al menos un punto.
                if (trayectoria.Puntos == null ||
                    trayectoria.Puntos.Count == 0)
                {
                    valida = false;
                }

                // Comparamos cada punto con el anterior.
                for (int i = 1; i < trayectoria.Puntos.Count; i++)
                {
                    var anterior =
                        trayectoria.Puntos[i - 1];

                    var actual =
                        trayectoria.Puntos[i];

                    // Comprobamos que el tiempo siempre avance.
                    double diferenciaTiempo =
                        actual.TiempoSeg -
                        anterior.TiempoSeg;

                    if (diferenciaTiempo < 0)
                    {
                        Console.WriteLine(
                            $"ERROR tiempo: " +
                            $"{trayectoria.AircraftIdentification} | " +
                            $"{trayectoria.AircraftAddress}"
                        );

                        valida = false;
                        break;
                    }

                    // Si existe un hueco >120 s, el gestor debería haber
                    // creado dos trayectorias distintas.
                    if (diferenciaTiempo > MAX_HUECO_TRAYECTORIA)
                    {
                        Console.WriteLine(
                            $"ERROR hueco >120s: " +
                            $"{trayectoria.AircraftIdentification} | " +
                            $"{trayectoria.AircraftAddress} | " +
                            $"{diferenciaTiempo:F2}s"
                        );

                        valida = false;
                        break;
                    }

                    // Todos los puntos deben pertenecer al mismo avión.
                    if (actual.AircraftAddress !=
                        trayectoria.AircraftAddress)
                    {
                        Console.WriteLine(
                            $"ERROR AircraftAddress: " +
                            $"{trayectoria.AircraftAddress}"
                        );

                        valida = false;
                        break;
                    }

                    // Radar y ADS-B nunca deben mezclarse dentro
                    // de una misma trayectoria.
                    if (actual.Fuente != trayectoria.Fuente)
                    {
                        Console.WriteLine(
                            $"ERROR Fuente: " +
                            $"{trayectoria.AircraftAddress}"
                        );

                        valida = false;
                        break;
                    }

                    // Comprobamos que las coordenadas estén
                    // dentro de los límites físicos posibles.
                    if (actual.Latitud < -90 ||
                        actual.Latitud > 90 ||
                        actual.Longitud < -180 ||
                        actual.Longitud > 180)
                    {
                        Console.WriteLine(
                            $"ERROR coordenadas: " +
                            $"{trayectoria.AircraftIdentification} | " +
                            $"{trayectoria.AircraftAddress}"
                        );

                        valida = false;
                        break;
                    }

                    // Si tenemos identificación tanto en la trayectoria
                    // como en el punto, ambas deben coincidir.
                    if (
                        !string.IsNullOrEmpty(
                            trayectoria.AircraftIdentification
                        ) &&
                        !string.IsNullOrEmpty(
                            actual.AircraftIdentification
                        ) &&
                        !string.Equals(
                            trayectoria.AircraftIdentification,
                            actual.AircraftIdentification,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        Console.WriteLine(
                            $"ERROR identificación: " +
                            $"{trayectoria.AircraftAddress}"
                        );

                        valida = false;
                        break;
                    }
                }

                // Contabilizamos el resultado de la trayectoria completa.
                if (valida)
                    correctas++;
                else
                    conErrores++;
            }

            // Resumen global de la validación.
            Console.WriteLine(
                $"Trayectorias totales: {trayectorias.Count}"
            );

            Console.WriteLine(
                $"Correctas: {correctas}"
            );

            Console.WriteLine(
                $"Con errores: {conErrores}"
            );
        }
    }
}