namespace ASTERIX_Decoder_p1
{
    internal class MensajeCAT048
    {
        public int PosicionFichero;

        // I048/010 - Data Source Identifier
        public int Sac, Sic;

        // I048/140 - Time of Day
        public double TiempoDiaSeg;

        // I048/020 - Target Report Descriptor
        public int Typ;
        public bool Sim, Spi;
        public int Rdp, Rab;

        // I048/040 - Measured Position
        public double Rho, Theta;

        // Posición calculada en coordenadas geodésicas.
        public double Latitud;
        public double Longitud;
        public double AlturaGeodesicaM;

        // I048/070 - Mode 3/A
        public int Mode3ACodigo;
        public int Mode3AV, Mode3AG, Mode3AL;

        // I048/090 - Flight Level
        public double? FlightLevel;
        public int FlV, FlG;

        // Altitudes calculadas a partir de Flight Level y QNH.
        public double? AltitudEstandarFt;
        public double? AltitudCorregidaQNH;

        // I048/130 - Radar Plot Characteristics
        public double? Srl, Prl, Apd;
        public int? Srr;
        public int? Sam, Pam;
        public double? Rpd;

        // I048/220 - Aircraft Address
        public string AircraftAddressHex;

        // I048/240 - Aircraft Identification
        public string AircraftIdentification;

        // I048/250 - Mode S MB Data
        public double? Bds40McpAltitude;
        public double? Bds40FmsAltitude;
        public double? Bds40BarometricPressure;

        public double? Bds50RollAngle;
        public double? Bds50TrueTrackAngle;
        public double? Bds50GroundSpeed;
        public double? Bds50TrackAngleRate;
        public double? Bds50TrueAirspeed;

        public double? Bds60MagneticHeading;
        public double? Bds60Ias;
        public double? Bds60Mach;
        public double? Bds60BaroRate;
        public double? Bds60Ivv;

        // I048/161 - Track Number
        public int TrackNumber;

        // I048/200 - Track Velocity
        public double GroundSpeedKt;
        public double Heading;

        // I048/170 - Track Status
        public int Cnf, Rad, Dou, Mah, Cdm;
        public int? Tre, Gho, Sup, Tcc;

        // I048/230 - Communications / ACAS
        public int Com, Stat, Si, Mssc, Arc, Aic, B1a, B1b;
    }
}