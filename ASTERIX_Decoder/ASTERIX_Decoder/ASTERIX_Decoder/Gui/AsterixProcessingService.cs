#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ASTERIX_Decoder;   // Trayectoria, GestorTrayectorias, TipoFuente

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>Informacion de progreso durante la carga.</summary>
    internal sealed class ProgresoCarga
    {
        public string Etapa = "";
        public int Porcentaje;
    }

    /// <summary>Resumen de la operacion de carga para la barra superior.</summary>
    internal sealed class ResumenCarga
    {
        public string NombreArchivo = "";
        public long TamanoBytes;
        public int MensajesCat048;
        public int MensajesCat021;
        public long MilisegundosProcesado;
    }

    /// <summary>Resultado completo del procesado de uno o varios ficheros ASTERIX.</summary>
    internal sealed class DatosAsterix
    {
        public MensajeCAT048[] Mensajes048 = Array.Empty<MensajeCAT048>();
        public MensajeCAT021[] Mensajes021 = Array.Empty<MensajeCAT021>();
        public List<MensajeCAT048> Mensajes048Filtrados = new();
        public List<MensajeCAT021> Mensajes021Filtrados = new();
        public List<Trayectoria> TrayectoriasRadar = new();
        public List<Trayectoria> TrayectoriasAdsb = new();
        public FiltroCAT048 Filtro048 = new();
        public FiltroCAT021 Filtro021 = new();
        public ResumenCarga Resumen = new();
    }

    /// <summary>
    /// Orquesta el procesado ASTERIX reutilizando EXCLUSIVAMENTE las clases del
    /// nucleo (DecoderCAT048/021, FiltroCAT048/021, GestorTrayectorias). No
    /// reimplementa ninguna logica de decodificacion, filtrado ni asociacion de
    /// vuelos; solo copia el orden de operaciones que ya establece Program.cs.
    ///
    /// Conserva los mensajes decodificados sin filtrar en memoria, de forma que
    /// cambiar los filtros no obliga a volver a leer ni decodificar el fichero.
    /// </summary>
    internal sealed class AsterixProcessingService
    {
        // Un bloque ASTERIX localizado dentro de un buffer concreto.
        private readonly struct Bloque
        {
            public readonly byte[] Buffer;
            public readonly int Inicio;
            public readonly int Longitud;

            public Bloque(byte[] buffer, int inicio, int longitud)
            {
                Buffer = buffer;
                Inicio = inicio;
                Longitud = longitud;
            }
        }

        // Mensajes decodificados completos (sin filtrar). Se guardan para poder
        // reaplicar filtros sin volver a decodificar.
        private MensajeCAT048[] _mensajes048 = Array.Empty<MensajeCAT048>();
        private MensajeCAT021[] _mensajes021 = Array.Empty<MensajeCAT021>();

        /// <summary>true cuando ya hay un fichero cargado y decodificado.</summary>
        public bool HayDatos { get; private set; }

        /// <summary>Carga y procesa en segundo plano (no bloquea la interfaz).</summary>
        public Task<DatosAsterix> CargarAsync(
            string[] rutas,
            FiltroCAT048 filtro048,
            FiltroCAT021 filtro021,
            IProgress<ProgresoCarga> progreso,
            CancellationToken ct = default)
        {
            return Task.Run(() => Procesar(rutas, filtro048, filtro021, progreso, ct), ct);
        }

        private DatosAsterix Procesar(
            string[] rutas,
            FiltroCAT048 filtro048,
            FiltroCAT021 filtro021,
            IProgress<ProgresoCarga> progreso,
            CancellationToken ct)
        {
            var crono = Stopwatch.StartNew();
            long totalBytes = 0;

            var bloques048 = new List<Bloque>();
            var bloques021 = new List<Bloque>();

            // 1-3. Leer cada fichero, escanear bloques e identificar categoria/longitud.
            progreso?.Report(new ProgresoCarga { Etapa = "Leyendo archivo(s)...", Porcentaje = 8 });

            foreach (var ruta in rutas)
            {
                ct.ThrowIfCancellationRequested();

                byte[] datos = File.ReadAllBytes(ruta);
                totalBytes += datos.LongLength;
                Escanear(datos, bloques048, bloques021);
            }

            progreso?.Report(new ProgresoCarga { Etapa = "Decodificando CAT048 (Radar)...", Porcentaje = 35 });

            var opciones = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount),
                CancellationToken = ct
            };

            // 4. Decodificar CAT048 en paralelo (independiente por mensaje).
            var mensajes048 = new MensajeCAT048[bloques048.Count];
            Parallel.For(0, bloques048.Count, opciones, i =>
            {
                var b = bloques048[i];
                mensajes048[i] = DecoderCAT048.DecodificarMensaje(b.Buffer, b.Inicio, b.Longitud);
            });

            // 5. Correccion QNH (secuencial: recuerda el ultimo QNH por aeronave).
            progreso?.Report(new ProgresoCarga { Etapa = "Corrigiendo altitudes QNH...", Porcentaje = 52 });
            DecoderCAT048.CalcularAltitudesQNH(mensajes048);

            // 6. Posicion geodesica CAT048 (ya se conoce la altitud).
            progreso?.Report(new ProgresoCarga { Etapa = "Calculando posiciones geodesicas...", Porcentaje = 62 });
            Parallel.For(0, mensajes048.Length, opciones, i =>
            {
                DecoderCAT048.CalcularPosicionGeodesica(mensajes048[i]);
            });

            // 7. Decodificar CAT021 en paralelo.
            progreso?.Report(new ProgresoCarga { Etapa = "Decodificando CAT021 (ADS-B)...", Porcentaje = 75 });
            var mensajes021 = new MensajeCAT021[bloques021.Count];
            Parallel.For(0, bloques021.Count, opciones, i =>
            {
                var b = bloques021[i];
                mensajes021[i] = DecoderCAT021.DecodificarMensaje(b.Buffer, b.Inicio, b.Longitud);
            });

            // 8. Correccion de altitud CAT021.
            DecoderCAT021.CalcularAltitudesQNH(mensajes021);

            // Se conservan los mensajes decodificados completos.
            _mensajes048 = mensajes048;
            _mensajes021 = mensajes021;
            HayDatos = true;

            // 9-10. Filtrado y generacion de trayectorias para la simulacion.
            progreso?.Report(new ProgresoCarga { Etapa = "Filtrando y generando trayectorias...", Porcentaje = 90 });
            var datos2 = ConstruirResultado(filtro048, filtro021);

            crono.Stop();

            datos2.Resumen = new ResumenCarga
            {
                NombreArchivo = rutas.Length == 1
                    ? Path.GetFileName(rutas[0])
                    : $"{rutas.Length} archivos",
                TamanoBytes = totalBytes,
                MensajesCat048 = mensajes048.Length,
                MensajesCat021 = mensajes021.Length,
                MilisegundosProcesado = crono.ElapsedMilliseconds
            };

            progreso?.Report(new ProgresoCarga { Etapa = "Listo", Porcentaje = 100 });
            return datos2;
        }

        /// <summary>
        /// Reaplica los filtros y regenera las trayectorias usando los mensajes
        /// ya decodificados en memoria. No vuelve a leer ni decodificar el fichero.
        /// </summary>
        public DatosAsterix ReaplicarFiltros(FiltroCAT048 filtro048, FiltroCAT021 filtro021)
        {
            if (!HayDatos)
                return new DatosAsterix();

            var datos = ConstruirResultado(filtro048, filtro021);
            datos.Resumen = new ResumenCarga
            {
                NombreArchivo = "(filtros reaplicados)",
                MensajesCat048 = _mensajes048.Length,
                MensajesCat021 = _mensajes021.Length
            };
            return datos;
        }

        private DatosAsterix ConstruirResultado(FiltroCAT048 filtro048, FiltroCAT021 filtro021)
        {
            var filtrados048 = filtro048.Aplicar(_mensajes048);
            var filtrados021 = filtro021.Aplicar(_mensajes021);

            var trayectoriasRadar = GestorTrayectorias.CrearDesdeCAT048(filtrados048);
            var trayectoriasAdsb = GestorTrayectorias.CrearDesdeCAT021(filtrados021);

            return new DatosAsterix
            {
                Mensajes048 = _mensajes048,
                Mensajes021 = _mensajes021,
                Mensajes048Filtrados = filtrados048,
                Mensajes021Filtrados = filtrados021,
                TrayectoriasRadar = trayectoriasRadar,
                TrayectoriasAdsb = trayectoriasAdsb,
                Filtro048 = filtro048,
                Filtro021 = filtro021
            };
        }

        /// <summary>
        /// Recorre un buffer leyendo unicamente CAT y LEN de cada bloque ASTERIX,
        /// igual que el escaner de Program.cs. Reparte los bloques por categoria.
        /// </summary>
        private static void Escanear(byte[] datos, List<Bloque> bloques048, List<Bloque> bloques021)
        {
            int posicion = 0;

            while (posicion + 2 < datos.Length)
            {
                int cat = datos[posicion];
                int len = (datos[posicion + 1] << 8) | datos[posicion + 2];

                // LEN invalido o fuera del fichero: se detiene el escaneo de forma segura.
                if (len <= 0 || posicion + len > datos.Length)
                    break;

                if (cat == 48)
                    bloques048.Add(new Bloque(datos, posicion, len));
                else if (cat == 21)
                    bloques021.Add(new Bloque(datos, posicion, len));

                posicion += len;
            }
        }
    }
}
