using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ASTERIX_Decoder_p1
{
    internal static class DecoderCAT021
    {
        const bool MODO_DEBUG = false;

        private class DataItemInfo
        {
            public string Nombre;
            public int Longitud;
            public bool Decodificar;

            public DataItemInfo(string nombre, int longitud, bool decodificar)
            {
                Nombre = nombre;
                Longitud = longitud;
                Decodificar = decodificar;
            }
        }

        // Decodifica el FSPEC y los Data Items presentes.
        internal static MensajeCAT021 DecodificarMensaje(
            byte[] datos,
            int posicion,
            int len)
        {
            var msg = new MensajeCAT021
            {
                PosicionFichero = posicion
            };

            var frnPresentes = new List<int>(24);

            int posicionFspec = posicion + 3;
            bool hayExtension = true;
            int numeroOcteto = 0;

            while (hayExtension)
            {
                byte fspec = datos[posicionFspec];

                for (int bit = 7; bit >= 1; bit--)
                {
                    if ((fspec & (1 << bit)) != 0)
                    {
                        int frn =
                            numeroOcteto * 7 +
                            (8 - bit);

                        frnPresentes.Add(frn);
                    }
                }

                hayExtension = (fspec & 0x01) != 0;

                posicionFspec++;
                numeroOcteto++;
            }

            int posicionDataItem = posicionFspec;

            foreach (int frn in frnPresentes)
            {
                DataItemInfo item =
                    ObtenerDataItem(frn);

                if (item.Decodificar)
                {
                    DecodificarDataItem(
                        frn,
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                }
                else
                {
                    SaltarDataItem(
                        frn,
                        datos,
                        ref posicionDataItem
                    );
                }
            }

            return msg;
        }

        // Define qué FRN se decodifican y cuáles únicamente se saltan.
        private static DataItemInfo ObtenerDataItem(int frn)
        {
            switch (frn)
            {
                case 1:
                    return new DataItemInfo(
                        "I021/010 - Data Source Identification",
                        2,
                        true
                    );

                case 2:
                    return new DataItemInfo(
                        "I021/040 - Target Report Descriptor",
                        -1,
                        true
                    );

                case 7:
                    return new DataItemInfo(
                        "I021/131 - High Resolution Position WGS84",
                        8,
                        true
                    );

                case 11:
                    return new DataItemInfo(
                        "I021/080 - Target Address",
                        3,
                        true
                    );

                case 12:
                    return new DataItemInfo(
                        "I021/073 - Time of Message Reception of Position",
                        3,
                        true
                    );

                case 19:
                    return new DataItemInfo(
                        "I021/070 - Mode 3/A Code",
                        2,
                        true
                    );

                case 21:
                    return new DataItemInfo(
                        "I021/145 - Flight Level",
                        2,
                        true
                    );

                case 29:
                    return new DataItemInfo(
                        "I021/170 - Target Identification",
                        6,
                        true
                    );

                case 48:
                    return new DataItemInfo(
                        "RE - Reserved Expansion Field",
                        -1,
                        true
                    );

                default:
                    return new DataItemInfo(
                        "CAT021 no requerido",
                        -1,
                        false
                    );
            }
        }

        // Envía cada FRN a su función correspondiente.
        private static void DecodificarDataItem(
            int frn,
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            switch (frn)
            {
                case 1:
                    DecodificarFRN1(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 2:
                    DecodificarFRN2(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 7:
                    DecodificarFRN7(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 11:
                    DecodificarFRN11(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 12:
                    DecodificarFRN12(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 19:
                    DecodificarFRN19(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 21:
                    DecodificarFRN21(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 29:
                    DecodificarFRN29(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 48:
                    DecodificarFRN48(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                default:
                    SaltarDataItem(
                        frn,
                        datos,
                        ref posicionDataItem
                    );
                    break;
            }
        }

        // FRN1 - I021/010 - Data Source Identification
        private static void DecodificarFRN1(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            msg.Sac = datos[posicionDataItem];
            msg.Sic = datos[posicionDataItem + 1];

            Log($"I021/010 - SAC:{msg.Sac} SIC:{msg.Sic}");

            posicionDataItem += 2;
        }

        // FRN2 - I021/040 - Target Report Descriptor
        private static void DecodificarFRN2(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            byte primario = datos[posicionDataItem];
            posicionDataItem++;

            msg.Atp = (primario >> 5) & 0x07;
            msg.Arc = (primario >> 3) & 0x03;
            msg.Rc = (primario & 0x04) != 0;
            msg.Rab = (primario & 0x02) != 0;

            bool hayExtension =
                (primario & 0x01) != 0;

            if (hayExtension)
            {
                byte ext1 = datos[posicionDataItem];
                posicionDataItem++;

                msg.Dcr =
                    (ext1 & 0x80) != 0;

                msg.GroundBitStatus =
                    (ext1 & 0x40) != 0;

                msg.Sim =
                    (ext1 & 0x20) != 0;

                msg.Tst =
                    (ext1 & 0x10) != 0;

                msg.Saa =
                    (ext1 & 0x08) != 0;

                hayExtension =
                    (ext1 & 0x01) != 0;

                while (hayExtension)
                {
                    byte extN =
                        datos[posicionDataItem];

                    posicionDataItem++;

                    hayExtension =
                        (extN & 0x01) != 0;
                }
            }

            Log(
                $"I021/040 - GBS:{msg.GroundBitStatus} " +
                $"SIM:{msg.Sim} TST:{msg.Tst}"
            );
        }

        // FRN7 - I021/131 - High Resolution Position WGS-84
        private static void DecodificarFRN7(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            uint latUnsigned =
                ((uint)datos[posicionDataItem] << 24) |
                ((uint)datos[posicionDataItem + 1] << 16) |
                ((uint)datos[posicionDataItem + 2] << 8) |
                datos[posicionDataItem + 3];

            uint lonUnsigned =
                ((uint)datos[posicionDataItem + 4] << 24) |
                ((uint)datos[posicionDataItem + 5] << 16) |
                ((uint)datos[posicionDataItem + 6] << 8) |
                datos[posicionDataItem + 7];

            int latRaw =
                unchecked((int)latUnsigned);

            int lonRaw =
                unchecked((int)lonUnsigned);

            const double LSB =
                180.0 / 1073741824.0;

            msg.Latitud =
                latRaw * LSB;

            msg.Longitud =
                lonRaw * LSB;

            Log(
                $"I021/131 - LAT:{msg.Latitud} " +
                $"LON:{msg.Longitud}"
            );

            posicionDataItem += 8;
        }

        // FRN11 - I021/080 - Target Address
        private static void DecodificarFRN11(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            int address =
                (datos[posicionDataItem] << 16) |
                (datos[posicionDataItem + 1] << 8) |
                datos[posicionDataItem + 2];

            msg.AircraftAddressHex =
                address.ToString("X6");

            Log(
                $"I021/080 - Address:{msg.AircraftAddressHex}"
            );

            posicionDataItem += 3;
        }

        // FRN12 - I021/073 - Time of Message Reception of Position
        private static void DecodificarFRN12(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            int raw =
                (datos[posicionDataItem] << 16) |
                (datos[posicionDataItem + 1] << 8) |
                datos[posicionDataItem + 2];

            msg.TiempoRecepcionPosicion =
                raw / 128.0;

            Log(
                $"I021/073 - Tiempo:{msg.TiempoRecepcionPosicion}s"
            );

            posicionDataItem += 3;
        }

        // FRN19 - I021/070 - Mode 3/A
        private static void DecodificarFRN19(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            int valor =
                (datos[posicionDataItem] << 8) |
                datos[posicionDataItem + 1];

            int codigo =
                valor & 0x0FFF;

            msg.Mode3ACodigo =
                int.Parse(
                    Convert
                        .ToString(codigo, 8)
                        .PadLeft(4, '0')
                );

            Log(
                $"I021/070 - Mode3A:{msg.Mode3ACodigo}"
            );

            posicionDataItem += 2;
        }

        // FRN21 - I021/145 - Flight Level
        private static void DecodificarFRN21(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            int raw =
                (datos[posicionDataItem] << 8) |
                datos[posicionDataItem + 1];

            if ((raw & 0x8000) != 0)
                raw -= 0x10000;

            msg.FlightLevel =
                raw * 0.25;

            Log(
                $"I021/145 - FL:{msg.FlightLevel}"
            );

            posicionDataItem += 2;
        }

        // FRN29 - I021/170 - Target Identification
        private static void DecodificarFRN29(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            ulong valor = 0;

            for (int i = 0; i < 6; i++)
            {
                valor =
                    (valor << 8) |
                    datos[posicionDataItem + i];
            }

            var sb =
                new StringBuilder(8);

            for (int i = 0; i < 8; i++)
            {
                int desplazamiento =
                    42 - i * 6;

                int codigo =
                    (int)(
                        (valor >> desplazamiento)
                        & 0x3F
                    );

                sb.Append(
                    DecodificarCaracterIA5(codigo)
                );
            }

            msg.AircraftIdentification =
                sb.ToString().Trim();

            Log(
                $"I021/170 - ID:{msg.AircraftIdentification}"
            );

            posicionDataItem += 6;
        }

        // FRN48 - Reserved Expansion Field - BPS
        private static void DecodificarFRN48(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT021 msg)
        {
            int inicioRE =
                posicionDataItem;

            int longitudRE =
                datos[posicionDataItem];

            if (longitudRE < 2)
            {
                posicionDataItem += longitudRE;
                return;
            }

            posicionDataItem++;

            byte indicador =
                datos[posicionDataItem];

            posicionDataItem++;

            bool bpsPresente =
                (indicador & 0x80) != 0;

            if (bpsPresente)
            {
                int valor =
                    (datos[posicionDataItem] << 8) |
                    datos[posicionDataItem + 1];

                int rawBps =
                    valor & 0x0FFF;

                msg.BarometricPressureSetting =
                    800.0 + rawBps * 0.1;
            }

            posicionDataItem =
                inicioRE + longitudRE;
        }

        // Avanza los Data Items que no necesitamos decodificar.
        private static void SaltarDataItem(
            int frn,
            byte[] datos,
            ref int posicionDataItem)
        {
            switch (frn)
            {
                case 1:
                    posicionDataItem += 2;
                    break;

                case 2:
                    {
                        bool extension = true;

                        while (extension)
                        {
                            byte octeto =
                                datos[posicionDataItem];

                            extension =
                                (octeto & 0x01) != 0;

                            posicionDataItem++;
                        }

                        break;
                    }

                case 3:
                    posicionDataItem += 2;
                    break;

                case 4:
                    posicionDataItem += 1;
                    break;

                case 5:
                    posicionDataItem += 3;
                    break;

                case 6:
                    posicionDataItem += 6;
                    break;

                case 7:
                    posicionDataItem += 8;
                    break;

                case 8:
                    posicionDataItem += 3;
                    break;

                case 9:
                    posicionDataItem += 2;
                    break;

                case 10:
                    posicionDataItem += 2;
                    break;

                case 11:
                    posicionDataItem += 3;
                    break;

                case 12:
                    posicionDataItem += 3;
                    break;

                case 13:
                    posicionDataItem += 4;
                    break;

                case 14:
                    posicionDataItem += 3;
                    break;

                case 15:
                    posicionDataItem += 4;
                    break;

                case 16:
                    posicionDataItem += 2;
                    break;

                case 17:
                    {
                        bool extension = true;

                        while (extension)
                        {
                            byte octeto =
                                datos[posicionDataItem];

                            extension =
                                (octeto & 0x01) != 0;

                            posicionDataItem++;
                        }

                        break;
                    }

                case 18:
                    posicionDataItem += 1;
                    break;

                case 19:
                    posicionDataItem += 2;
                    break;

                case 20:
                    posicionDataItem += 2;
                    break;

                case 21:
                    posicionDataItem += 2;
                    break;

                case 22:
                    posicionDataItem += 2;
                    break;

                case 23:
                    posicionDataItem += 1;
                    break;

                case 24:
                    posicionDataItem += 2;
                    break;

                case 25:
                    posicionDataItem += 2;
                    break;

                case 26:
                    posicionDataItem += 4;
                    break;

                case 27:
                    posicionDataItem += 2;
                    break;

                case 28:
                    posicionDataItem += 3;
                    break;

                case 29:
                    posicionDataItem += 6;
                    break;

                case 30:
                    posicionDataItem += 1;
                    break;

                case 31:
                    {
                        byte primary =
                            datos[posicionDataItem];

                        bool ws =
                            (primary & 0x80) != 0;

                        bool wd =
                            (primary & 0x40) != 0;

                        bool tmp =
                            (primary & 0x20) != 0;

                        bool trb =
                            (primary & 0x10) != 0;

                        bool extension = true;

                        while (extension)
                        {
                            byte octeto =
                                datos[posicionDataItem];

                            extension =
                                (octeto & 0x01) != 0;

                            posicionDataItem++;
                        }

                        if (ws)
                            posicionDataItem += 2;

                        if (wd)
                            posicionDataItem += 2;

                        if (tmp)
                            posicionDataItem += 2;

                        if (trb)
                            posicionDataItem += 1;

                        break;
                    }

                case 32:
                    posicionDataItem += 2;
                    break;

                case 33:
                    posicionDataItem += 2;
                    break;

                case 34:
                    {
                        byte primary =
                            datos[posicionDataItem];

                        bool tisPresente =
                            (primary & 0x80) != 0;

                        bool tidPresente =
                            (primary & 0x40) != 0;

                        bool extensionPrimary = true;

                        while (extensionPrimary)
                        {
                            byte octeto =
                                datos[posicionDataItem];

                            extensionPrimary =
                                (octeto & 0x01) != 0;

                            posicionDataItem++;
                        }

                        if (tisPresente)
                        {
                            bool extensionTIS = true;

                            while (extensionTIS)
                            {
                                byte octeto =
                                    datos[posicionDataItem];

                                extensionTIS =
                                    (octeto & 0x01) != 0;

                                posicionDataItem++;
                            }
                        }

                        if (tidPresente)
                        {
                            int rep =
                                datos[posicionDataItem];

                            posicionDataItem++;
                            posicionDataItem += rep * 15;
                        }

                        break;
                    }

                case 35:
                    posicionDataItem += 1;
                    break;

                case 36:
                    posicionDataItem += 1;
                    break;

                case 37:
                    {
                        byte primary =
                            datos[posicionDataItem];

                        bool hayExtension =
                            (primary & 0x01) != 0;

                        posicionDataItem++;

                        if (hayExtension)
                            posicionDataItem++;

                        break;
                    }

                case 38:
                    posicionDataItem += 1;
                    break;

                case 39:
                    {
                        int rep =
                            datos[posicionDataItem];

                        posicionDataItem++;
                        posicionDataItem += rep * 8;

                        break;
                    }

                case 40:
                    posicionDataItem += 7;
                    break;

                case 41:
                    posicionDataItem += 1;
                    break;

                case 42:
                    {
                        int numeroSubcamposPresentes = 0;
                        int numeroOctetoPrimary = 0;

                        bool extension = true;

                        while (extension)
                        {
                            byte octeto =
                                datos[posicionDataItem];

                            posicionDataItem++;

                            int camposValidos;

                            if (numeroOctetoPrimary <= 2)
                                camposValidos = 7;
                            else if (numeroOctetoPrimary == 3)
                                camposValidos = 2;
                            else
                                camposValidos = 0;

                            for (
                                int bit = 7;
                                bit >= 8 - camposValidos;
                                bit--
                            )
                            {
                                if ((octeto & (1 << bit)) != 0)
                                    numeroSubcamposPresentes++;
                            }

                            extension =
                                (octeto & 0x01) != 0;

                            numeroOctetoPrimary++;
                        }

                        posicionDataItem +=
                            numeroSubcamposPresentes;

                        break;
                    }

                case 43:
                case 44:
                case 45:
                case 46:
                case 47:
                    break;

                case 48:
                    {
                        int longitudRE =
                            datos[posicionDataItem];

                        posicionDataItem +=
                            longitudRE;

                        break;
                    }

                case 49:
                    {
                        int longitudSP =
                            datos[posicionDataItem];

                        posicionDataItem +=
                            longitudSP;

                        break;
                    }
            }
        }

        // Conversión de caracteres IA-5.
        private static char DecodificarCaracterIA5(int codigo)
        {
            if (codigo >= 1 && codigo <= 26)
            {
                return (char)(
                    'A' + codigo - 1
                );
            }

            if (codigo >= 48 && codigo <= 57)
            {
                return (char)(
                    '0' + codigo - 48
                );
            }

            if (codigo == 32)
                return ' ';

            return ' ';
        }

        // Calcula la altitud corregida con QNH por debajo de 6000 ft.
        internal static void CalcularAltitudesQNH(
            MensajeCAT021[] mensajes)
        {
            const double QNH_ESTANDAR = 1013.25;
            const double ALTITUD_TRANSICION = 6000.0;

            var ultimoQnhPorAvion =
                new Dictionary<string, double>();

            foreach (var msg in mensajes)
            {
                string avion =
                    msg.AircraftAddressHex;

                double? qnh =
                    msg.BarometricPressureSetting;

                bool bpNoEstandar =
                    qnh.HasValue &&
                    (qnh.Value < 1013.0 ||
                     qnh.Value > 1013.5);

                if (
                    bpNoEstandar &&
                    !string.IsNullOrEmpty(avion)
                )
                {
                    ultimoQnhPorAvion[avion] =
                        qnh.Value;
                }

                if (!msg.FlightLevel.HasValue)
                    continue;

                double altitudModeC =
                    msg.FlightLevel.Value * 100.0;

                if (
                    altitudModeC >=
                    ALTITUD_TRANSICION
                )
                {
                    msg.AltitudEstandarFt =
                        altitudModeC;

                    continue;
                }

                double? qnhUsado;

                if (bpNoEstandar)
                {
                    qnhUsado = qnh;
                }
                else if (
                    !string.IsNullOrEmpty(avion) &&
                    ultimoQnhPorAvion.TryGetValue(
                        avion,
                        out double qnhPrevio
                    )
                )
                {
                    qnhUsado = qnhPrevio;
                }
                else
                {
                    qnhUsado = qnh;
                }

                if (qnhUsado.HasValue)
                {
                    msg.AltitudCorregidaQNH =
                        altitudModeC +
                        (
                            qnhUsado.Value -
                            QNH_ESTANDAR
                        ) * 30.0;
                }
                else
                {
                    msg.AltitudEstandarFt =
                        altitudModeC;
                }
            }
        }

        // Exporta los mensajes CAT021 a CSV.
        internal static void ExportarCSV(
            IEnumerable<MensajeCAT021> mensajes,
            string ruta,
            FiltroCAT021 filtro)
        {
            var cultura =
                new CultureInfo("es-ES");

            using (
                var writer =
                    new StreamWriter(
                        ruta,
                        false,
                        Encoding.UTF8,
                        bufferSize: 1 << 20
                    )
            )
            {
                writer.WriteLine(
                    "PosicionFichero;" +
                    "SAC;SIC;" +
                    "GroundBitStatus;" +
                    "Latitud;Longitud;" +
                    "AircraftAddress;" +
                    "TiempoRecepcionPosicion;" +
                    "Mode3A;" +
                    "FlightLevel;" +
                    "AltitudEstandarFt;" +
                    "AltitudCorregidaQNH;" +
                    "AircraftIdentification;" +
                    "BarometricPressureSetting;" +
                    "MotivoDescartado"
                );

                foreach (var m in mensajes)
                {
                    writer.WriteLine(
                        string.Join(
                            ";",
                            m.PosicionFichero,
                            m.Sac,
                            m.Sic,

                            m.GroundBitStatus.HasValue
                                ? (
                                    m.GroundBitStatus.Value
                                        ? "1"
                                        : "0"
                                )
                                : "",

                            FormatearNullable(
                                m.Latitud,
                                cultura,
                                8
                            ),

                            FormatearNullable(
                                m.Longitud,
                                cultura,
                                8
                            ),

                            m.AircraftAddressHex ?? "",

                            FormatearNullable(
                                m.TiempoRecepcionPosicion,
                                cultura,
                                3
                            ),

                            m.Mode3ACodigo.HasValue
                                ? m.Mode3ACodigo.Value
                                    .ToString("D4")
                                : "",

                            FormatearNullable(
                                m.FlightLevel,
                                cultura,
                                2
                            ),

                            FormatearNullable(
                                m.AltitudEstandarFt,
                                cultura,
                                2
                            ),

                            FormatearNullable(
                                m.AltitudCorregidaQNH,
                                cultura,
                                2
                            ),

                            m.AircraftIdentification ?? "",

                            FormatearNullable(
                                m.BarometricPressureSetting,
                                cultura,
                                1
                            ),

                            filtro.MotivoDescarte(m)
                        )
                    );
                }
            }
        }

        private static string FormatearNullable(
            double? valor,
            CultureInfo cultura,
            int decimales)
        {
            if (!valor.HasValue)
                return "";

            return valor.Value.ToString(
                "F" + decimales,
                cultura
            );
        }

        private static void Log(string texto)
        {
            if (MODO_DEBUG)
                Console.WriteLine(texto);
        }
    }
}