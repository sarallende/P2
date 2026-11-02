#nullable disable
using System;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Proveedor de mapa propio que usa las teselas oscuras de Esri
    /// ("World Dark Gray Base"), gratuitas y SIN API key.
    ///
    /// Motivo: los servidores de OpenStreetMap bloquean (403 "Access blocked") las
    /// peticiones de aplicaciones como GMap.NET, y CARTO pasó a exigir una clave
    /// de pago ("API KEY REQUIRED"). Esri permite este uso sin clave y su estilo
    /// oscuro combina con el tema de la aplicacion.
    ///
    /// Nota: las teselas reales de Esri llegan hasta el zoom 16 (por eso el mapa
    /// limita el zoom maximo a 16).
    /// </summary>
    internal sealed class ProveedorMapaOscuro : GMapProvider
    {
        public static readonly ProveedorMapaOscuro Instance = new ProveedorMapaOscuro();

        // Guid nuevo: evita reutilizar teselas antiguas guardadas en cache.
        private readonly Guid _id = new Guid("4C8D1E6F-2A3B-4C5D-9E0F-1122334455AA");
        private GMapProvider[] _overlays;

        public override Guid Id => _id;
        public override string Name => "Esri Oscuro";
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
            // Esri "World Dark Gray Base". El orden de la URL es zoom / fila(Y) / columna(X).
            return "https://server.arcgisonline.com/ArcGIS/rest/services/" +
                   "Canvas/World_Dark_Gray_Base/MapServer/tile/" +
                   $"{zoom}/{pos.Y}/{pos.X}";
        }
    }
}
