#nullable disable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using GMap.NET;
using GMap.NET.WindowsForms;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Marcador de aeronave dibujado sobre el mapa. Representa el avion con un
    /// pequeno icono orientado segun su rumbo y una etiqueta con el callsign
    /// (o el Aircraft Address si no hay callsign). El color distingue la fuente.
    /// </summary>
    [Serializable]
    internal sealed class MarcadorAvion : GMapMarker
    {
        public Color Color = Color.White;
        public double Rumbo;              // grados (0 = Norte)
        public string Etiqueta = "";
        public bool Seleccionado;
        public bool MostrarEtiqueta = true;

        [NonSerialized] public AeronaveSimulada Aeronave;

        public MarcadorAvion(PointLatLng p) : base(p)
        {
            Size = new Size(22, 22);
            Offset = new Point(-11, -11);   // centra el icono sobre la coordenada
        }

        public override void OnRender(Graphics g)
        {
            float cx = LocalPosition.X + Size.Width / 2f;
            float cy = LocalPosition.Y + Size.Height / 2f;

            GraphicsState estado = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Halo cuando esta seleccionada.
            if (Seleccionado)
            {
                using var halo = new SolidBrush(Color.FromArgb(70, Tema.Seleccion));
                g.FillEllipse(halo, cx - 14f, cy - 14f, 28f, 28f);
            }

            // Icono orientado segun el rumbo.
            g.TranslateTransform(cx, cy);
            g.RotateTransform((float)Rumbo);

            PointF[] avion =
            {
                new PointF(0f, -9f),
                new PointF(6f, 7f),
                new PointF(0f, 3.5f),
                new PointF(-6f, 7f)
            };

            using (var relleno = new SolidBrush(Color))
            using (var borde = new Pen(Seleccionado ? Color.White : Color.FromArgb(190, 8, 14, 24),
                                       Seleccionado ? 2f : 1.2f))
            {
                g.FillPolygon(relleno, avion);
                g.DrawPolygon(borde, avion);
            }

            g.Restore(estado);

            // Etiqueta (sin rotar), a la derecha del icono.
            if (MostrarEtiqueta && !string.IsNullOrEmpty(Etiqueta))
            {
                using var fuente = new Font("Segoe UI", 7.75f, FontStyle.Bold);
                SizeF sz = g.MeasureString(Etiqueta, fuente);
                float lx = cx + 11f;
                float ly = cy - sz.Height / 2f;

                using var fondo = new SolidBrush(Color.FromArgb(165, 6, 12, 22));
                using var texto = new SolidBrush(Seleccionado ? Tema.Seleccion : Color.White);
                g.FillRectangle(fondo, lx - 2f, ly, sz.Width + 4f, sz.Height);
                g.DrawString(Etiqueta, fuente, texto, lx, ly);
            }
        }
    }
}
