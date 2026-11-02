namespace ASTERIX_Decoder_p1
{
    internal class MensajeCAT021
    {
        public int PosicionFichero;

        // I021/010 - Data Source Identification
        public int Sac, Sic;

        // I021/040 - Target Report Descriptor
        public int? Atp;
        public int? Arc;
        public bool? Rc;
        public bool? Rab;

        public bool? Dcr;
        public bool? GroundBitStatus;
        public bool? Sim;
        public bool? Tst;
        public bool? Saa;

        // I021/080 - Target Address
        public string AircraftAddressHex;

        // I021/131 - High Resolution Position WGS-84
        public double? Latitud;
        public double? Longitud;

        // I021/073 - Time of Message Reception of Position
        public double? TiempoRecepcionPosicion;

        // I021/070 - Mode 3/A
        public int? Mode3ACodigo;

        // I021/145 - Flight Level
        public double? FlightLevel;

        // I021/170 - Target Identification
        public string AircraftIdentification;

        // Reserved Expansion Field - Barometric Pressure Setting
        public double? BarometricPressureSetting;

        // Altitudes calculadas a partir de Flight Level y QNH.
        public double? AltitudEstandarFt;
        public double? AltitudCorregidaQNH;
    }
}