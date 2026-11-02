using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ASTERIX_Decoder_p1
{
    internal static class DecoderCAT048
    {
        const bool MODO_DEBUG = false;

        // Posición del radar de Barcelona.
        const double RADAR_LAT_DEG =
            41.0 + 18.0 / 60.0 + 2.5284 / 3600.0;

        const double RADAR_LON_DEG =
            2.0 + 6.0 / 60.0 + 7.4095 / 3600.0;

        const double RADAR_ELEVACION_TERRENO_M = 2.007;
        const double RADAR_ALTURA_ANTENA_M = 25.25;

        const double NM_A_METROS = 1852.0;
        const double FT_A_METROS = 0.3048;
        const double DEG_A_RAD = Math.PI / 180.0;
        const double RAD_A_DEG = 180.0 / Math.PI;

        // Parámetros WGS-84.
        const double WGS84_A = 6378137.0;
        const double WGS84_E2 = 0.00669437999013;

        static readonly double RadarLatRad =
            RADAR_LAT_DEG * DEG_A_RAD;

        static readonly double RadarLonRad =
            RADAR_LON_DEG * DEG_A_RAD;

        static readonly double RadarAlturaM =
            RADAR_ELEVACION_TERRENO_M +
            RADAR_ALTURA_ANTENA_M;

        static readonly double RadarRadioTierraM =
            CalcularRadioTierra(RadarLatRad);

        private class DataItemInfo
        {
            public string Nombre;
            public int Longitud;
            public bool Decodificar;

            public DataItemInfo(
                string nombre,
                int longitud,
                bool decodificar)
            {
                Nombre = nombre;
                Longitud = longitud;
                Decodificar = decodificar;
            }
        }

        // Decodifica el FSPEC y los Data Items presentes.
        internal static MensajeCAT048 DecodificarMensaje(
            byte[] datos,
            int posicion,
            int len)
        {
            var msg = new MensajeCAT048
            {
                PosicionFichero = posicion
            };

            var frnPresentes =
                new List<int>(16);

            int posicionFspec =
                posicion + 3;

            bool hayExtension = true;
            int numeroOcteto = 0;

            while (hayExtension)
            {
                byte fspec =
                    datos[posicionFspec];

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

                hayExtension =
                    (fspec & 0x01) != 0;

                posicionFspec++;
                numeroOcteto++;
            }

            int posicionDataItem =
                posicionFspec;

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

        // FRN1 - I048/010 - Data Source Identifier
        private static void DecodificarFRN1(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            msg.Sac =
                datos[posicionDataItem];

            msg.Sic =
                datos[posicionDataItem + 1];

            Log(
                $"I048/010 - SAC:{msg.Sac} SIC:{msg.Sic}"
            );

            posicionDataItem += 2;
        }

        // FRN2 - I048/140 - Time of Day
        private static void DecodificarFRN2(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int valorTiempo =
                (datos[posicionDataItem] << 16) |
                (datos[posicionDataItem + 1] << 8) |
                datos[posicionDataItem + 2];

            msg.TiempoDiaSeg =
                valorTiempo / 128.0;

            Log(
                $"I048/140 - Tiempo:{msg.TiempoDiaSeg}s"
            );

            posicionDataItem += 3;
        }

        // FRN3 - I048/020 - Target Report Descriptor
        private static void DecodificarFRN3(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int inicio =
                posicionDataItem;

            bool extension = true;

            while (extension)
            {
                byte octeto =
                    datos[posicionDataItem];

                extension =
                    (octeto & 0x01) != 0;

                posicionDataItem++;
            }

            byte primerOcteto =
                datos[inicio];

            msg.Typ =
                (primerOcteto >> 5) & 0x07;

            msg.Sim =
                ((primerOcteto >> 4) & 0x01) != 0;

            msg.Rdp =
                (primerOcteto >> 3) & 0x01;

            msg.Spi =
                ((primerOcteto >> 2) & 0x01) != 0;

            msg.Rab =
                (primerOcteto >> 1) & 0x01;

            Log(
                $"I048/020 - TYP:{msg.Typ} SIM:{msg.Sim} SPI:{msg.Spi}"
            );
        }

        // FRN4 - I048/040 - Measured Position
        private static void DecodificarFRN4(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int rhoRaw =
                (datos[posicionDataItem] << 8) |
                datos[posicionDataItem + 1];

            int thetaRaw =
                (datos[posicionDataItem + 2] << 8) |
                datos[posicionDataItem + 3];

            msg.Rho =
                rhoRaw / 256.0;

            msg.Theta =
                thetaRaw * 360.0 / 65536.0;

            Log(
                $"I048/040 - RHO:{msg.Rho}NM THETA:{msg.Theta}º"
            );

            posicionDataItem += 4;
        }

        // FRN5 - I048/070 - Mode 3/A
        private static void DecodificarFRN5(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int valor =
                (datos[posicionDataItem] << 8) |
                datos[posicionDataItem + 1];

            msg.Mode3AV =
                (valor >> 15) & 0x01;

            msg.Mode3AG =
                (valor >> 14) & 0x01;

            msg.Mode3AL =
                (valor >> 13) & 0x01;

            int codigo =
                valor & 0x0FFF;

            msg.Mode3ACodigo =
                int.Parse(
                    Convert
                        .ToString(codigo, 8)
                        .PadLeft(4, '0')
                );

            Log(
                $"I048/070 - Mode3A:{msg.Mode3ACodigo}"
            );

            posicionDataItem += 2;
        }

        // FRN6 - I048/090 - Flight Level
        private static void DecodificarFRN6(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int valor =
                (datos[posicionDataItem] << 8) |
                datos[posicionDataItem + 1];

            msg.FlV =
                (valor >> 15) & 0x01;

            msg.FlG =
                (valor >> 14) & 0x01;

            int rawFlightLevel =
                valor & 0x3FFF;

            if ((rawFlightLevel & 0x2000) != 0)
                rawFlightLevel -= 0x4000;

            msg.FlightLevel =
                rawFlightLevel * 0.25;

            Log(
                $"I048/090 - FL:{msg.FlightLevel}"
            );

            posicionDataItem += 2;
        }

        // FRN7 - I048/130 - Radar Plot Characteristics
        private static void DecodificarFRN7(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            byte primary =
                datos[posicionDataItem];

            posicionDataItem++;

            bool srlPresente =
                (primary & 0x80) != 0;

            bool srrPresente =
                (primary & 0x40) != 0;

            bool samPresente =
                (primary & 0x20) != 0;

            bool prlPresente =
                (primary & 0x10) != 0;

            bool pamPresente =
                (primary & 0x08) != 0;

            bool rpdPresente =
                (primary & 0x04) != 0;

            bool apdPresente =
                (primary & 0x02) != 0;

            if (srlPresente)
            {
                msg.Srl =
                    datos[posicionDataItem] *
                    360.0 / 8192.0;

                posicionDataItem++;
            }

            if (srrPresente)
            {
                msg.Srr =
                    datos[posicionDataItem];

                posicionDataItem++;
            }

            if (samPresente)
            {
                msg.Sam =
                    unchecked(
                        (sbyte)datos[posicionDataItem]
                    );

                posicionDataItem++;
            }

            if (prlPresente)
            {
                msg.Prl =
                    datos[posicionDataItem] *
                    360.0 / 8192.0;

                posicionDataItem++;
            }

            if (pamPresente)
            {
                msg.Pam =
                    unchecked(
                        (sbyte)datos[posicionDataItem]
                    );

                posicionDataItem++;
            }

            if (rpdPresente)
            {
                msg.Rpd =
                    unchecked(
                        (sbyte)datos[posicionDataItem]
                    ) / 256.0;

                posicionDataItem++;
            }

            if (apdPresente)
            {
                msg.Apd =
                    unchecked(
                        (sbyte)datos[posicionDataItem]
                    ) * 360.0 / 16384.0;

                posicionDataItem++;
            }

            Log("I048/130 decodificado");
        }

        // FRN8 - I048/220 - Aircraft Address
        private static void DecodificarFRN8(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int aircraftAddress =
                (datos[posicionDataItem] << 16) |
                (datos[posicionDataItem + 1] << 8) |
                datos[posicionDataItem + 2];

            msg.AircraftAddressHex =
                aircraftAddress.ToString("X6");

            Log(
                $"I048/220 - {msg.AircraftAddressHex}"
            );

            posicionDataItem += 3;
        }

        // FRN9 - I048/240 - Aircraft Identification
        private static void DecodificarFRN9(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
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
                $"I048/240 - {msg.AircraftIdentification}"
            );

            posicionDataItem += 6;
        }

        // FRN10 - I048/250 - Mode S MB Data
        private static void DecodificarFRN10(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int rep =
                datos[posicionDataItem];

            posicionDataItem++;

            for (int i = 0; i < rep; i++)
            {
                byte codigoBDS =
                    datos[posicionDataItem + 7];

                int bds1 =
                    (codigoBDS >> 4) & 0x0F;

                int bds2 =
                    codigoBDS & 0x0F;

                if (bds1 == 4 && bds2 == 0)
                {
                    DecodificarBDS40(
                        datos,
                        posicionDataItem,
                        msg
                    );
                }
                else if (bds1 == 5 && bds2 == 0)
                {
                    DecodificarBDS50(
                        datos,
                        posicionDataItem,
                        msg
                    );
                }
                else if (bds1 == 6 && bds2 == 0)
                {
                    DecodificarBDS60(
                        datos,
                        posicionDataItem,
                        msg
                    );
                }

                posicionDataItem += 8;
            }

            Log(
                $"I048/250 - REP:{rep}"
            );
        }

        // FRN11 - I048/161 - Track Number
        private static void DecodificarFRN11(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int valor =
                (datos[posicionDataItem] << 8) |
                datos[posicionDataItem + 1];

            msg.TrackNumber =
                valor & 0x0FFF;

            Log(
                $"I048/161 - Track:{msg.TrackNumber}"
            );

            posicionDataItem += 2;
        }

        // FRN13 - I048/200 - Track Velocity
        private static void DecodificarFRN13(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int speedRaw =
                (datos[posicionDataItem] << 8) |
                datos[posicionDataItem + 1];

            int headingRaw =
                (datos[posicionDataItem + 2] << 8) |
                datos[posicionDataItem + 3];

            double groundSpeedNMs =
                speedRaw / 16384.0;

            msg.GroundSpeedKt =
                groundSpeedNMs * 3600.0;

            msg.Heading =
                headingRaw * 360.0 / 65536.0;

            Log(
                $"I048/200 - GS:{msg.GroundSpeedKt}kt HDG:{msg.Heading}º"
            );

            posicionDataItem += 4;
        }

        // FRN14 - I048/170 - Track Status
        private static void DecodificarFRN14(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int inicio =
                posicionDataItem;

            bool extension = true;

            while (extension)
            {
                byte octeto =
                    datos[posicionDataItem];

                extension =
                    (octeto & 0x01) != 0;

                posicionDataItem++;
            }

            byte primerOcteto =
                datos[inicio];

            msg.Cnf =
                (primerOcteto >> 7) & 0x01;

            msg.Rad =
                (primerOcteto >> 5) & 0x03;

            msg.Dou =
                (primerOcteto >> 4) & 0x01;

            msg.Mah =
                (primerOcteto >> 3) & 0x01;

            msg.Cdm =
                (primerOcteto >> 1) & 0x03;

            if (posicionDataItem - inicio >= 2)
            {
                byte extension1 =
                    datos[inicio + 1];

                msg.Tre =
                    (extension1 >> 7) & 0x01;

                msg.Gho =
                    (extension1 >> 6) & 0x01;

                msg.Sup =
                    (extension1 >> 5) & 0x01;

                msg.Tcc =
                    (extension1 >> 4) & 0x01;
            }

            Log(
                $"I048/170 - CNF:{msg.Cnf} RAD:{msg.Rad}"
            );
        }

        // FRN21 - I048/230 - Communications / ACAS
        private static void DecodificarFRN21(
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
        {
            int valor =
                (datos[posicionDataItem] << 8) |
                datos[posicionDataItem + 1];

            msg.Com =
                (valor >> 13) & 0x07;

            msg.Stat =
                (valor >> 10) & 0x07;

            msg.Si =
                (valor >> 9) & 0x01;

            msg.Mssc =
                (valor >> 7) & 0x01;

            msg.Arc =
                (valor >> 6) & 0x01;

            msg.Aic =
                (valor >> 5) & 0x01;

            msg.B1a =
                (valor >> 4) & 0x01;

            msg.B1b =
                valor & 0x0F;

            Log(
                $"I048/230 - COM:{msg.Com} STAT:{msg.Stat}"
            );

            posicionDataItem += 2;
        }

        // Envía cada FRN a su función correspondiente.
        private static void DecodificarDataItem(
            int frn,
            byte[] datos,
            ref int posicionDataItem,
            MensajeCAT048 msg)
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

                case 3:
                    DecodificarFRN3(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 4:
                    DecodificarFRN4(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 5:
                    DecodificarFRN5(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 6:
                    DecodificarFRN6(
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

                case 8:
                    DecodificarFRN8(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 9:
                    DecodificarFRN9(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 10:
                    DecodificarFRN10(
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

                case 13:
                    DecodificarFRN13(
                        datos,
                        ref posicionDataItem,
                        msg
                    );
                    break;

                case 14:
                    DecodificarFRN14(
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
            }
        }

        // Avanza los Data Items que no necesitamos decodificar.
        private static void SaltarDataItem(
            int frn,
            byte[] datos,
            ref int posicionDataItem)
        {
            switch (frn)
            {
                case 12:
                    posicionDataItem += 4;
                    break;

                case 15:
                    posicionDataItem += 4;
                    break;

                case 16:
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

                case 17:
                    posicionDataItem += 2;
                    break;

                case 18:
                    posicionDataItem += 4;
                    break;

                case 19:
                    posicionDataItem += 2;
                    break;

                case 20:
                    {
                        byte primary =
                            datos[posicionDataItem];

                        posicionDataItem++;

                        bool calPresente =
                            (primary & 0x80) != 0;

                        bool rdsPresente =
                            (primary & 0x40) != 0;

                        if (calPresente)
                            posicionDataItem += 2;

                        if (rdsPresente)
                        {
                            int rep =
                                datos[posicionDataItem];

                            posicionDataItem++;
                            posicionDataItem += rep * 6;
                        }

                        break;
                    }

                case 22:
                    posicionDataItem += 7;
                    break;

                case 23:
                    posicionDataItem += 1;
                    break;

                case 24:
                    posicionDataItem += 2;
                    break;

                case 25:
                    posicionDataItem += 1;
                    break;

                case 26:
                    posicionDataItem += 2;
                    break;

                case 27:
                    {
                        int longitudSP =
                            datos[posicionDataItem];

                        posicionDataItem +=
                            longitudSP;

                        break;
                    }

                case 28:
                    {
                        int longitudRE =
                            datos[posicionDataItem];

                        posicionDataItem +=
                            longitudRE;

                        break;
                    }
            }
        }

        // BDS 4,0 - Selected Vertical Intention
        private static void DecodificarBDS40(
            byte[] datos,
            int posicion,
            MensajeCAT048 msg)
        {
            ulong valor = 0;

            for (int i = 0; i < 7; i++)
            {
                valor =
                    (valor << 8) |
                    datos[posicion + i];
            }

            int statusMCP =
                (int)((valor >> 55) & 0x01);

            int rawMCP =
                (int)((valor >> 43) & 0x0FFF);

            int statusFMS =
                (int)((valor >> 42) & 0x01);

            int rawFMS =
                (int)((valor >> 30) & 0x0FFF);

            int statusBP =
                (int)((valor >> 29) & 0x01);

            int rawBP =
                (int)((valor >> 17) & 0x0FFF);

            if (statusMCP == 1)
            {
                msg.Bds40McpAltitude =
                    rawMCP * 16.0;
            }

            if (statusFMS == 1)
            {
                msg.Bds40FmsAltitude =
                    rawFMS * 16.0;
            }

            if (statusBP == 1)
            {
                msg.Bds40BarometricPressure =
                    800.0 + rawBP * 0.1;
            }
        }

        // BDS 5,0 - Track and Turn Report
        private static void DecodificarBDS50(
            byte[] datos,
            int posicion,
            MensajeCAT048 msg)
        {
            ulong valor = 0;

            for (int i = 0; i < 7; i++)
            {
                valor =
                    (valor << 8) |
                    datos[posicion + i];
            }

            int statusRoll =
                (int)((valor >> 55) & 0x01);

            int rawRoll =
                (int)((valor >> 45) & 0x03FF);

            if ((rawRoll & 0x0200) != 0)
                rawRoll -= 0x0400;

            int statusTrack =
                (int)((valor >> 44) & 0x01);

            int rawTrack =
                (int)((valor >> 33) & 0x07FF);

            if ((rawTrack & 0x0400) != 0)
                rawTrack -= 0x0800;

            int statusGS =
                (int)((valor >> 32) & 0x01);

            int rawGS =
                (int)((valor >> 22) & 0x03FF);

            int statusTAR =
                (int)((valor >> 21) & 0x01);

            int rawTAR =
                (int)((valor >> 11) & 0x03FF);

            if ((rawTAR & 0x0200) != 0)
                rawTAR -= 0x0400;

            int statusTAS =
                (int)((valor >> 10) & 0x01);

            int rawTAS =
                (int)(valor & 0x03FF);

            if (statusRoll == 1)
            {
                msg.Bds50RollAngle =
                    rawRoll * (45.0 / 256.0);
            }

            if (statusTrack == 1)
            {
                msg.Bds50TrueTrackAngle =
                    rawTrack * (90.0 / 512.0);
            }

            if (statusGS == 1)
            {
                msg.Bds50GroundSpeed =
                    rawGS * 2.0;
            }

            if (statusTAR == 1)
            {
                msg.Bds50TrackAngleRate =
                    rawTAR * (8.0 / 256.0);
            }

            if (statusTAS == 1)
            {
                msg.Bds50TrueAirspeed =
                    rawTAS * 2.0;
            }
        }

        // BDS 6,0 - Heading and Speed Report
        private static void DecodificarBDS60(
            byte[] datos,
            int posicion,
            MensajeCAT048 msg)
        {
            ulong valor = 0;

            for (int i = 0; i < 7; i++)
            {
                valor =
                    (valor << 8) |
                    datos[posicion + i];
            }

            int statusHeading =
                (int)((valor >> 55) & 0x01);

            int rawHeading =
                (int)((valor >> 44) & 0x07FF);

            if ((rawHeading & 0x0400) != 0)
                rawHeading -= 0x0800;

            int statusIAS =
                (int)((valor >> 43) & 0x01);

            int rawIAS =
                (int)((valor >> 33) & 0x03FF);

            int statusMach =
                (int)((valor >> 32) & 0x01);

            int rawMach =
                (int)((valor >> 22) & 0x03FF);

            int statusBaroRate =
                (int)((valor >> 21) & 0x01);

            int rawBaroRate =
                (int)((valor >> 11) & 0x03FF);

            if ((rawBaroRate & 0x0200) != 0)
                rawBaroRate -= 0x0400;

            int statusIVV =
                (int)((valor >> 10) & 0x01);

            int rawIVV =
                (int)(valor & 0x03FF);

            if ((rawIVV & 0x0200) != 0)
                rawIVV -= 0x0400;

            if (statusHeading == 1)
            {
                msg.Bds60MagneticHeading =
                    rawHeading * (90.0 / 512.0);
            }

            if (statusIAS == 1)
            {
                msg.Bds60Ias =
                    rawIAS;
            }

            if (statusMach == 1)
            {
                msg.Bds60Mach =
                    rawMach * (2.048 / 512.0);
            }

            if (statusBaroRate == 1)
            {
                msg.Bds60BaroRate =
                    rawBaroRate * 32.0;
            }

            if (statusIVV == 1)
            {
                msg.Bds60Ivv =
                    rawIVV * 32.0;
            }
        }

        // Corrección QNH por debajo de 6000 ft.
        internal static void CalcularAltitudesQNH(
            MensajeCAT048[] mensajes)
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
                    msg.Bds40BarometricPressure;

                bool bpNoEstandar =
                    qnh.HasValue &&
                    (
                        qnh.Value < 1013.0 ||
                        qnh.Value > 1013.5
                    );

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

        // Conversión de posición radar a coordenadas geodésicas WGS-84.
        internal static void CalcularPosicionGeodesica(
            MensajeCAT048 msg)
        {
            double rhoM =
                msg.Rho * NM_A_METROS;

            double thetaRad =
                msg.Theta * DEG_A_RAD;

            double alturaAvionM;

            if (msg.AltitudCorregidaQNH.HasValue)
            {
                alturaAvionM =
                    msg.AltitudCorregidaQNH.Value *
                    FT_A_METROS;
            }
            else if (msg.AltitudEstandarFt.HasValue)
            {
                alturaAvionM =
                    msg.AltitudEstandarFt.Value *
                    FT_A_METROS;
            }
            else
            {
                alturaAvionM =
                    RadarAlturaM;
            }

            double el =
                CalcularElevacion(
                    rhoM,
                    alturaAvionM
                );

            double xLocal =
                rhoM *
                Math.Cos(el) *
                Math.Sin(thetaRad);

            double yLocal =
                rhoM *
                Math.Cos(el) *
                Math.Cos(thetaRad);

            double zLocal =
                rhoM *
                Math.Sin(el);

            double sinLat =
                Math.Sin(RadarLatRad);

            double cosLat =
                Math.Cos(RadarLatRad);

            double sinLon =
                Math.Sin(RadarLonRad);

            double cosLon =
                Math.Cos(RadarLonRad);

            double nu =
                WGS84_A /
                Math.Sqrt(
                    1.0 -
                    WGS84_E2 *
                    sinLat *
                    sinLat
                );

            double tx =
                (nu + RadarAlturaM) *
                cosLat *
                cosLon;

            double ty =
                (nu + RadarAlturaM) *
                cosLat *
                sinLon;

            double tz =
                (
                    nu * (1.0 - WGS84_E2) +
                    RadarAlturaM
                ) * sinLat;

            double xGeo =
                -sinLon * xLocal -
                sinLat * cosLon * yLocal +
                cosLat * cosLon * zLocal +
                tx;

            double yGeo =
                cosLon * xLocal -
                sinLat * sinLon * yLocal +
                cosLat * sinLon * zLocal +
                ty;

            double zGeo =
                cosLat * yLocal +
                sinLat * zLocal +
                tz;

            double dxy =
                Math.Sqrt(
                    xGeo * xGeo +
                    yGeo * yGeo
                );

            double lat =
                Math.Atan(
                    (zGeo / dxy) /
                    (
                        1.0 -
                        (
                            WGS84_A *
                            WGS84_E2
                        ) /
                        Math.Sqrt(
                            dxy * dxy +
                            zGeo * zGeo
                        )
                    )
                );

            double nuIter =
                WGS84_A /
                Math.Sqrt(
                    1.0 -
                    WGS84_E2 *
                    Math.Sin(lat) *
                    Math.Sin(lat)
                );

            double altura =
                dxy / Math.Cos(lat) -
                nuIter;

            double latAnterior =
                lat >= 0
                    ? -0.1
                    : 0.1;

            int iteraciones = 0;

            while (
                Math.Abs(
                    lat - latAnterior
                ) > 1e-8 &&
                iteraciones < 50
            )
            {
                latAnterior = lat;

                lat =
                    Math.Atan(
                        (
                            zGeo *
                            (
                                1.0 +
                                altura / nuIter
                            )
                        ) /
                        (
                            dxy *
                            (
                                (1.0 - WGS84_E2) +
                                altura / nuIter
                            )
                        )
                    );

                nuIter =
                    WGS84_A /
                    Math.Sqrt(
                        1.0 -
                        WGS84_E2 *
                        Math.Sin(lat) *
                        Math.Sin(lat)
                    );

                altura =
                    dxy / Math.Cos(lat) -
                    nuIter;

                iteraciones++;
            }

            double lon =
                Math.Atan2(
                    yGeo,
                    xGeo
                );

            msg.Latitud =
                lat * RAD_A_DEG;

            msg.Longitud =
                lon * RAD_A_DEG;

            msg.AlturaGeodesicaM =
                altura;
        }

        private static double CalcularRadioTierra(
            double latRad)
        {
            double sinLat =
                Math.Sin(latRad);

            return
                (
                    WGS84_A *
                    (1.0 - WGS84_E2)
                ) /
                Math.Pow(
                    1.0 -
                    WGS84_E2 *
                    sinLat *
                    sinLat,
                    1.5
                );
        }

        private static double CalcularElevacion(
            double rhoM,
            double alturaAvionM)
        {
            if (rhoM < 1e-10)
                return 0.0;

            double numerador =
                2 *
                RadarRadioTierraM *
                (
                    alturaAvionM -
                    RadarAlturaM
                ) +
                alturaAvionM *
                alturaAvionM -
                RadarAlturaM *
                RadarAlturaM -
                rhoM *
                rhoM;

            double denominador =
                2 *
                rhoM *
                (
                    RadarRadioTierraM +
                    RadarAlturaM
                );

            double seno =
                numerador /
                denominador;

            if (
                seno > -1.0 &&
                seno < 1.0
            )
            {
                return Math.Asin(seno);
            }

            return Math.PI / 2.0;
        }

        // Define qué FRN se decodifican y cuáles se saltan.
        private static DataItemInfo ObtenerDataItem(
            int frn)
        {
            switch (frn)
            {
                case 1:
                    return new DataItemInfo(
                        "I048/010 - Data Source Identifier",
                        2,
                        true
                    );

                case 2:
                    return new DataItemInfo(
                        "I048/140 - Time of Day",
                        3,
                        true
                    );

                case 3:
                    return new DataItemInfo(
                        "I048/020 - Target Report Descriptor",
                        -1,
                        true
                    );

                case 4:
                    return new DataItemInfo(
                        "I048/040 - Measured Position",
                        4,
                        true
                    );

                case 5:
                    return new DataItemInfo(
                        "I048/070 - Mode 3/A",
                        2,
                        true
                    );

                case 6:
                    return new DataItemInfo(
                        "I048/090 - Flight Level",
                        2,
                        true
                    );

                case 7:
                    return new DataItemInfo(
                        "I048/130 - Radar Plot Characteristics",
                        -1,
                        true
                    );

                case 8:
                    return new DataItemInfo(
                        "I048/220 - Aircraft Address",
                        3,
                        true
                    );

                case 9:
                    return new DataItemInfo(
                        "I048/240 - Aircraft Identification",
                        6,
                        true
                    );

                case 10:
                    return new DataItemInfo(
                        "I048/250 - Mode S MB Data",
                        -1,
                        true
                    );

                case 11:
                    return new DataItemInfo(
                        "I048/161 - Track Number",
                        2,
                        true
                    );

                case 12:
                    return new DataItemInfo(
                        "I048/042 - Cartesian Position",
                        4,
                        false
                    );

                case 13:
                    return new DataItemInfo(
                        "I048/200 - Track Velocity",
                        4,
                        true
                    );

                case 14:
                    return new DataItemInfo(
                        "I048/170 - Track Status",
                        -1,
                        true
                    );

                case 15:
                    return new DataItemInfo(
                        "I048/210 - Track Quality",
                        4,
                        false
                    );

                case 16:
                    return new DataItemInfo(
                        "I048/030 - Warning/Error",
                        -1,
                        false
                    );

                case 17:
                    return new DataItemInfo(
                        "I048/080 - Mode 3/A Confidence",
                        2,
                        false
                    );

                case 18:
                    return new DataItemInfo(
                        "I048/100 - Mode C",
                        4,
                        false
                    );

                case 19:
                    return new DataItemInfo(
                        "I048/110 - 3D Radar Height",
                        2,
                        false
                    );

                case 20:
                    return new DataItemInfo(
                        "I048/120 - Radial Doppler Speed",
                        -1,
                        false
                    );

                case 21:
                    return new DataItemInfo(
                        "I048/230 - Communications / ACAS",
                        2,
                        true
                    );

                default:
                    return new DataItemInfo(
                        "FRN no definido",
                        -1,
                        false
                    );
            }
        }

        // Exporta los mensajes CAT048 a CSV.
        internal static void ExportarCSV(
            IEnumerable<MensajeCAT048> mensajes,
            string ruta,
            FiltroCAT048 filtro)
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
                    "TiempoDiaSeg;" +
                    "TYP;TipoDeteccion;SIM;RDP;SPI;RAB;" +
                    "RHO;THETA;Latitud;Longitud;AlturaGeodesicaM;" +
                    "Mode3A;Mode3A_V;Mode3A_G;Mode3A_L;" +
                    "FlightLevel;AltitudEstandarFt;AltitudCorregidaQNH;FL_V;FL_G;" +
                    "SRL;SRR;SAM;PRL;PAM;RPD;APD;" +
                    "AircraftAddress;AircraftIdentification;" +
                    "BDS40_MCP_Altitude;BDS40_FMS_Altitude;BDS40_BarometricPressure;" +
                    "BDS50_RollAngle;BDS50_TrueTrackAngle;BDS50_GroundSpeed;BDS50_TrackAngleRate;BDS50_TrueAirspeed;" +
                    "BDS60_MagneticHeading;BDS60_IAS;BDS60_Mach;BDS60_BaroRate;BDS60_IVV;" +
                    "TrackNumber;" +
                    "GroundSpeed;Heading;" +
                    "CNF;RAD;TipoTrack;DOU;MAH;CDM;VerticalMode;" +
                    "TRE;GHO;SUP;TCC;" +
                    "COM;COM_Texto;" +
                    "STAT;STAT_Texto;" +
                    "SI;MSSC;ARC;AIC;B1A;B1B;" +
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
                            m.TiempoDiaSeg.ToString(cultura),
                            m.Typ,
                            ObtenerTipoDeteccion(m.Typ),
                            m.Sim ? "1" : "0",
                            m.Rdp,
                            m.Spi ? "1" : "0",
                            m.Rab,
                            m.Rho.ToString(cultura),
                            m.Theta.ToString(cultura),
                            m.Latitud.ToString("F6", cultura),
                            m.Longitud.ToString("F6", cultura),
                            Math.Round(
                                m.AlturaGeodesicaM,
                                1
                            ).ToString(cultura),
                            m.Mode3ACodigo.ToString("D4"),
                            m.Mode3AV,
                            m.Mode3AG,
                            m.Mode3AL,
                            FormatearNullable(
                                m.FlightLevel,
                                cultura
                            ),
                            FormatearNullable(
                                m.AltitudEstandarFt,
                                cultura
                            ),
                            FormatearNullable(
                                m.AltitudCorregidaQNH,
                                cultura
                            ),
                            m.FlV,
                            m.FlG,
                            FormatearNullable(
                                m.Srl,
                                cultura
                            ),
                            FormatearNullable(m.Srr),
                            FormatearNullable(m.Sam),
                            FormatearNullable(
                                m.Prl,
                                cultura
                            ),
                            FormatearNullable(m.Pam),
                            FormatearNullable(
                                m.Rpd,
                                cultura
                            ),
                            FormatearNullable(
                                m.Apd,
                                cultura
                            ),
                            m.AircraftAddressHex ?? "",
                            m.AircraftIdentification ?? "",
                            FormatearNullable(
                                m.Bds40McpAltitude,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds40FmsAltitude,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds40BarometricPressure,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds50RollAngle,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds50TrueTrackAngle,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds50GroundSpeed,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds50TrackAngleRate,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds50TrueAirspeed,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds60MagneticHeading,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds60Ias,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds60Mach,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds60BaroRate,
                                cultura
                            ),
                            FormatearNullable(
                                m.Bds60Ivv,
                                cultura
                            ),
                            m.TrackNumber,
                            m.GroundSpeedKt.ToString(cultura),
                            m.Heading.ToString(cultura),
                            m.Cnf,
                            m.Rad,
                            ObtenerTipoTrack(m.Rad),
                            m.Dou,
                            m.Mah,
                            m.Cdm,
                            ObtenerVerticalMode(m.Cdm),
                            FormatearNullable(m.Tre),
                            FormatearNullable(m.Gho),
                            FormatearNullable(m.Sup),
                            FormatearNullable(m.Tcc),
                            m.Com,
                            ObtenerCOM(m.Com),
                            m.Stat,
                            ObtenerFlightStatus(m.Stat),
                            m.Si,
                            m.Mssc,
                            m.Arc,
                            m.Aic,
                            m.B1a,
                            m.B1b,
                            filtro.MotivoDescarte(m)
                        )
                    );
                }
            }
        }

        private static string ObtenerCOM(
            int com)
        {
            switch (com)
            {
                case 0:
                    return "Surveillance only";

                case 1:
                    return "Comm A + Comm B";

                case 2:
                    return "Comm A + Comm B + Uplink ELM";

                case 3:
                    return "Comm A + Comm B + Uplink/Downlink ELM";

                case 4:
                    return "Level 5 Transponder";

                default:
                    return "No asignado";
            }
        }

        private static string ObtenerFlightStatus(
            int stat)
        {
            switch (stat)
            {
                case 0:
                    return "Airborne, no alert, no SPI";

                case 1:
                    return "On ground, no alert, no SPI";

                case 2:
                    return "Airborne, alert";

                case 3:
                    return "On ground, alert";

                case 4:
                    return "Alert + SPI";

                case 5:
                    return "No alert + SPI";

                case 6:
                    return "No asignado";

                case 7:
                    return "Unknown";

                default:
                    return "Desconocido";
            }
        }

        private static string ObtenerTipoDeteccion(
            int typ)
        {
            switch (typ)
            {
                case 0:
                    return "No detection";

                case 1:
                    return "Single PSR detection";

                case 2:
                    return "Single SSR detection";

                case 3:
                    return "SSR + PSR detection";

                case 4:
                    return "Single Mode S All-Call";

                case 5:
                    return "Single Mode S Roll-Call";

                case 6:
                    return "Mode S All-Call + PSR";

                case 7:
                    return "Mode S Roll-Call + PSR";

                default:
                    return "Desconocido";
            }
        }

        private static string ObtenerTipoTrack(
            int rad)
        {
            switch (rad)
            {
                case 0:
                    return "Combined Track";

                case 1:
                    return "PSR Track";

                case 2:
                    return "SSR/Mode S Track";

                case 3:
                    return "Invalid";

                default:
                    return "Desconocido";
            }
        }

        private static string ObtenerVerticalMode(
            int cdm)
        {
            switch (cdm)
            {
                case 0:
                    return "Maintaining";

                case 1:
                    return "Climbing";

                case 2:
                    return "Descending";

                case 3:
                    return "Unknown";

                default:
                    return "Desconocido";
            }
        }

        private static char DecodificarCaracterIA5(
            int codigo)
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

        private static string FormatearNullable(
            double? valor,
            CultureInfo cultura)
        {
            return valor.HasValue
                ? Math.Round(
                    valor.Value,
                    2
                ).ToString(cultura)
                : "";
        }

        private static string FormatearNullable(
            int? valor)
        {
            return valor.HasValue
                ? valor.Value.ToString()
                : "";
        }

        private static void Log(string texto)
        {
            if (MODO_DEBUG)
                Console.WriteLine(texto);
        }
    }
}