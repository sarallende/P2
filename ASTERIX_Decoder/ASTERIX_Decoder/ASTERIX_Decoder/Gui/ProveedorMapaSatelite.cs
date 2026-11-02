#nullable disable
using System;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Proveedor de mapa propio que usa la imagen de satelite de Esri
    /// ("World Imagery"), gratuita y SIN API key. Muestra el terreno real
    /// (mar, costa, relieve y zona urbana) visto desde el aire.
    ///
    /// Se usa un proveedor propio porque OpenStreetMap bloquea (403) a las apps de
    /// escritorio como GMap.NET y CARTO paso a exigir una clave de pago.
    /// </summary>
    internal sealed class ProveedorMapaSatelite : GMapProvider
    {
        public static readonly ProveedorMapaSatelite Instance = new ProveedorMapaSatelite();

        // Guid propio: evita reutilizar teselas de otros proveedores guardadas en cache.
        private readonly Guid _id = new Guid("7B2F9D14-6E85-4A31-B0C9-55AA66BB77CC");
        private GMapProvider[] _overlays;

        public override Guid Id => _id;
        public override string Name => "Esri Satelite";
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
            // Esri "World Imagery" (satelite). El orden de la URL es zoom / fila(Y) / columna(X).
            return "https://server.arcgisonline.com/ArcGIS/rest/services/" +
                   "World_Imagery/MapServer/tile/" +
                   $"{zoom}/{pos.Y}/{pos.X}";
        }
    }
}
