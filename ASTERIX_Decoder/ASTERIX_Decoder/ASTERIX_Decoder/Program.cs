using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ASTERIX_Decoder;

namespace ASTERIX_Decoder_p1
{
    // Guarda la posición, categoría y longitud de cada mensaje ASTERIX.
    internal struct Segmento
    {
        public int Inicio;
        public int Cat;
        public int Len;
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // Ficheros de entrada.
            string rutaRadar = @"C:\PGTA\P2\ASTERIX_Decoder\ASTERIX_Decoder\asterix_combinado.ast";
            string rutaAdsb = @"C:\PGTA\P2\ASTERIX_Decoder\ASTERIX_Decoder\asterix_combinado.ast";

            var cronometro = Stopwatch.StartNew();

            // Lectura completa de los dos ficheros.
            byte[] datosRadar = File.ReadAllBytes(rutaRadar);
            byte[] datosAdsb = File.ReadAllBytes(rutaAdsb);

            Console.WriteLine($"Fichero radar leído: {datosRadar.Length} bytes");
            Console.WriteLine($"Fichero ADS-B leído: {datosAdsb.Length} bytes");

            // Se escanean los bloques ASTERIX y se selecciona la categoría correspondiente.
            var segmentosCat048 = Escanear(datosRadar)
                .Where(s => s.Cat == 48)
                .ToList();

            var segmentosCat021 = Escanear(datosAdsb)
                .Where(s => s.Cat == 21)
                .ToList();

            Console.WriteLine($"Mensajes CAT048: {segmentosCat048.Count}");
            Console.WriteLine($"Mensajes CAT021: {segmentosCat021.Count}");

            // Número máximo de hilos utilizados en las partes paralelizables.
            var opciones = new ParallelOptions
            {
                MaxDegreeOfParallelism = 4
            };

            // ============================================================
            // CAT048 - RADAR
            // ============================================================

            var mensajes048 = new MensajeCAT048[segmentosCat048.Count];

            // Decodificación independiente de cada mensaje.
            Parallel.For(
                0,
                segmentosCat048.Count,
                opciones,
                i =>
                {
                    var seg = segmentosCat048[i];

                    mensajes048[i] =
                        DecoderCAT048.DecodificarMensaje(
                            datosRadar,
                            seg.Inicio,
                            seg.Len
                        );
                }
            );

            // La corrección QNH es secuencial porque puede necesitar
            // recordar el último QNH conocido para cada aeronave.
            DecoderCAT048.CalcularAltitudesQNH(mensajes048);

            // Una vez conocida la altitud, se calcula la posición geodésica.
            Parallel.For(
                0,
                mensajes048.Length,
                opciones,
                i =>
                {
                    DecoderCAT048.CalcularPosicionGeodesica(
                        mensajes048[i]
                    );
                }
            );

            // Configuración de filtros CAT048.
            var filtro048 = new FiltroCAT048
            {
                EliminarFlNoValidado = false,
                EliminarFlGarbled = false,
                EliminarFueraDelArea = true,
                EliminarBlancoPuro = true,
                EliminarTransponderFijo = true,
                EliminarEnTierra = true
            };

            var mensajes048Filtrados =
                filtro048.Aplicar(mensajes048);

            // ============================================================
            // CAT021 - ADS-B
            // ============================================================

            var mensajes021 = new MensajeCAT021[segmentosCat021.Count];

            // Decodificación independiente de cada mensaje ADS-B.
            Parallel.For(
                0,
                segmentosCat021.Count,
                opciones,
                i =>
                {
                    var seg = segmentosCat021[i];

                    mensajes021[i] =
                        DecoderCAT021.DecodificarMensaje(
                            datosAdsb,
                            seg.Inicio,
                            seg.Len
                        );
                }
            );

            // Corrección de altitud utilizando BPS/QNH cuando corresponde.
            DecoderCAT021.CalcularAltitudesQNH(mensajes021);

            // Configuración de filtros CAT021.
            var filtro021 = new FiltroCAT021
            {
                EliminarEnTierra = true,
                EliminarFueraDelArea = true,
                EliminarTransponderFijo = true
            };

            var mensajes021Filtrados =
                filtro021.Aplicar(mensajes021);

            // Creación de trayectorias para la simulación.
            var trayectoriasRadar =
                GestorTrayectorias.CrearDesdeCAT048(
                    mensajes048Filtrados
                );

            var trayectoriasAdsb =
                GestorTrayectorias.CrearDesdeCAT021(
                    mensajes021Filtrados
                );
            GestorTrayectorias.ValidarTrayectorias(
                trayectoriasRadar,
                "RADAR"
            );

            GestorTrayectorias.ValidarTrayectorias(
                trayectoriasAdsb,
                "ADS-B"
            );

            cronometro.Stop();

            // ============================================================
            // RESULTADOS
            // ============================================================

            Console.WriteLine();
            Console.WriteLine("====================================");
            Console.WriteLine("FIN DEL PROCESADO");
            Console.WriteLine($"CAT048 decodificados: {mensajes048.Length}");
            Console.WriteLine($"CAT048 filtrados: {mensajes048Filtrados.Count}");
            Console.WriteLine($"CAT021 decodificados: {mensajes021.Length}");
            Console.WriteLine($"CAT021 filtrados: {mensajes021Filtrados.Count}");
            Console.WriteLine($"Trayectorias Radar: {trayectoriasRadar.Count}");
            Console.WriteLine($"Trayectorias ADS-B: {trayectoriasAdsb.Count}");
            Console.WriteLine($"Tiempo total: {cronometro.ElapsedMilliseconds} ms");
            Console.WriteLine("====================================");

            Console.WriteLine();

            Console.WriteLine("Ejemplo trayectoria Radar:");

            var ejemploRadar =
                trayectoriasRadar
                .OrderByDescending(t => t.Puntos.Count)
                .FirstOrDefault();

            if (ejemploRadar != null)
            {
                Console.WriteLine(
                    $"{ejemploRadar.AircraftIdentification} | " +
                    $"{ejemploRadar.AircraftAddress}"
                );

                Console.WriteLine(
                    $"Puntos: {ejemploRadar.Puntos.Count}"
                );

                Console.WriteLine(
                    $"Inicio: {ejemploRadar.TiempoInicio:F3}"
                );

                Console.WriteLine(
                    $"Fin: {ejemploRadar.TiempoFin:F3}"
                );
            }

            Console.WriteLine();

            Console.WriteLine("Ejemplo trayectoria ADS-B:");

            var ejemploAdsb =
                trayectoriasAdsb
                .OrderByDescending(t => t.Puntos.Count)
                .FirstOrDefault();

            if (ejemploAdsb != null)
            {
                Console.WriteLine(
                    $"{ejemploAdsb.AircraftIdentification} | " +
                    $"{ejemploAdsb.AircraftAddress}"
                );

                Console.WriteLine(
                    $"Puntos: {ejemploAdsb.Puntos.Count}"
                );

                Console.WriteLine(
                    $"Inicio: {ejemploAdsb.TiempoInicio:F3}"
                );

                Console.WriteLine(
                    $"Fin: {ejemploAdsb.TiempoFin:F3}"
                );
            }

            // ============================================================
            // EXPORTACIÓN CSV
            // ============================================================

            // Se genera un CSV completo y otro con los filtros aplicados
            // para cada categoría.
            DecoderCAT048.ExportarCSV(
                mensajes048,
                Path.Combine(@"C:\PGTA\P2", "salida_CAT048.csv"),
                filtro048
            );

            DecoderCAT048.ExportarCSV(
                mensajes048Filtrados,
                Path.Combine(@"C:\PGTA\P2", "salida_CAT048_filtro.csv"),
                filtro048
            );

            DecoderCAT021.ExportarCSV(
                mensajes021,
                Path.Combine(@"C:\PGTA\P2", "salida_CAT021.csv"),
                filtro021
            );

            DecoderCAT021.ExportarCSV(
                mensajes021Filtrados,
                Path.Combine(@"C:\PGTA\P2", "salida_CAT021_filtro.csv"),
                filtro021
            );

            Console.WriteLine("CSV generados en C:\\PGTA\\P2");
            Console.WriteLine("Pulsa ENTER para cerrar...");
            Console.ReadLine();
        }

        // Recorre secuencialmente el fichero leyendo únicamente CAT y LEN.
        // Devuelve la posición de inicio de cada bloque ASTERIX.
        static List<Segmento> Escanear(byte[] datos)
        {
            var segmentos = new List<Segmento>(200_000);

            int posicion = 0;

            while (posicion + 2 < datos.Length)
            {
                int cat = datos[posicion];

                int len =
                    (datos[posicion + 1] << 8) |
                    datos[posicion + 2];

                // Evita continuar si el LEN es inválido o sale del fichero.
                if (len <= 0 || posicion + len > datos.Length)
                {
                    Console.WriteLine(
                        $"ERROR en posición {posicion} | LEN: {len}"
                    );

                    break;
                }

                segmentos.Add(
                    new Segmento
                    {
                        Inicio = posicion,
                        Cat = cat,
                        Len = len
                    }
                );

                posicion += len;
            }

            return segmentos;
        }
    }
}