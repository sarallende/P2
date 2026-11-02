#nullable disable
using System;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Proveedor de mapa propio basado en las teselas oscuras de CARTO (que a su
    /// vez usan datos de OpenStreetMap).
    ///
    /// Se usa porque los servidores de teselas de OpenStreetMap bloquean (403
    /// "Access blocked") las peticiones de aplicaciones de escritorio como las de
    /// GMap.NET, incluso fijando un User-Agent propio. CARTO permite este uso y,
    /// ademas, el estilo oscuro combina con el tema de la aplicacion.
    /// </summary>
    internal sealed class ProveedorMapaOscuro : GMapProvider
    {
        public static readonly ProveedorMapaOscuro Instance = new ProveedorMapaOscuro();

        private readonly Guid _id = new Guid("9E0F2B7A-3C41-4D58-8E6B-2A1C9D7F4B30");
        private GMapProvider[] _overlays;

        public override Guid Id => _id;
        public override string Name => "CARTO Oscuro (OSM)";
        public override PureProjection Projection => MercatorProjection.Instance;

        public override GMapProvider[] Overlays =>
            _overlays ??= new GMapProvider[] { this };

        public override PureImage GetTileImage(GPoint pos, int zoom)
        {
            string url = ConstruirUrl(pos, zoom);
            return GetTileImageUsingHttp(url);
        }

        private string ConstruirUrl(GPoint pos, int zoom)
        {
            // Rotacion entre subdominios a/b/c/d para repartir la carga.
            char servidor = "abcd"[GetServerNum(pos, 4)];
            return $"https://{servidor}.basemaps.cartocdn.com/dark_all/{zoom}/{pos.X}/{pos.Y}.png";
        }
    }
}
