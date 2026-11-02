#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ASTERIX_Decoder_p1.Gui
{
    /// <summary>
    /// Reloj virtual de la simulacion. Avanza el tiempo simulado segun el tiempo
    /// real transcurrido multiplicado por la velocidad elegida, de modo que a 1x
    /// un segundo real equivale a un segundo simulado. Los tiempos estan en
    /// segundos desde medianoche (nunca se usa la hora del ordenador).
    ///
    /// Usa un Timer de Windows Forms (hilo de interfaz), por lo que los eventos
    /// se disparan en el hilo correcto para actualizar controles.
    /// </summary>
    internal sealed class SimuladorTrafico
    {
        private readonly System.Windows.Forms.Timer _timer;
        private readonly Stopwatch _reloj = new Stopwatch();
        private double _ultimoSegReal;

        public List<AeronaveSimulada> Aeronaves { get; private set; } = new List<AeronaveSimulada>();

        public double TiempoMin { get; private set; }
        public double TiempoMax { get; private set; }
        public double TiempoActual { get; private set; }
        public double Velocidad { get; set; } = 1.0;
        public bool Reproduciendo { get; private set; }

        public bool HayDatos => Aeronaves.Count > 0 && TiempoMax > TiempoMin;

        /// <summary>Se dispara cada vez que cambia el tiempo simulado.</summary>
        public event Action<double> TiempoCambiado;

        /// <summary>Se dispara al cambiar el estado play/pausa/stop.</summary>
        public event Action ReproduccionCambiada;

        public SimuladorTrafico()
        {
            _timer = new System.Windows.Forms.Timer { Interval = 40 };   // ~25 fps
            _timer.Tick += (s, e) => Tick();
        }

        public void Cargar(IEnumerable<AeronaveSimulada> aeronaves)
        {
            Parar();
            Aeronaves = new List<AeronaveSimulada>(aeronaves);

            TiempoMin = double.MaxValue;
            TiempoMax = double.MinValue;

            foreach (var a in Aeronaves)
            {
                if (a.NumeroPuntos == 0) continue;
                if (a.TiempoInicio < TiempoMin) TiempoMin = a.TiempoInicio;
                if (a.TiempoFin > TiempoMax) TiempoMax = a.TiempoFin;
            }

            if (!HayDatos)
            {
                TiempoMin = 0;
                TiempoMax = 0;
            }

            TiempoActual = TiempoMin;
            TiempoCambiado?.Invoke(TiempoActual);
            ReproduccionCambiada?.Invoke();
        }

        public void Play()
        {
            if (!HayDatos || Reproduciendo) return;

            // Si estaba al final, se reinicia al principio.
            if (TiempoActual >= TiempoMax) TiempoActual = TiempoMin;

            Reproduciendo = true;
            _ultimoSegReal = 0;
            _reloj.Restart();
            _timer.Start();
            ReproduccionCambiada?.Invoke();
        }

        public void Pausa()
        {
            if (!Reproduciendo) return;
            Reproduciendo = false;
            _timer.Stop();
            _reloj.Stop();
            ReproduccionCambiada?.Invoke();
        }

        public void Parar()
        {
            Reproduciendo = false;
            _timer.Stop();
            _reloj.Reset();
            TiempoActual = TiempoMin;
            TiempoCambiado?.Invoke(TiempoActual);
            ReproduccionCambiada?.Invoke();
        }

        public void Reiniciar()
        {
            bool estaba = Reproduciendo;
            TiempoActual = TiempoMin;
            if (estaba)
            {
                _ultimoSegReal = 0;
                _reloj.Restart();
            }
            TiempoCambiado?.Invoke(TiempoActual);
        }

        /// <summary>Avanza o retrocede una cantidad de segundos simulados.</summary>
        public void Avanzar(double segundos) => Seek(TiempoActual + segundos);

        /// <summary>Salta a un instante concreto dentro del intervalo cargado.</summary>
        public void Seek(double tiempo)
        {
            TiempoActual = Clamp(tiempo);
            if (Reproduciendo)
            {
                _ultimoSegReal = 0;
                _reloj.Restart();
            }
            TiempoCambiado?.Invoke(TiempoActual);
        }

        private void Tick()
        {
            if (!Reproduciendo) return;

            double ahora = _reloj.Elapsed.TotalSeconds;
            double delta = ahora - _ultimoSegReal;
            _ultimoSegReal = ahora;

            TiempoActual += delta * Velocidad;

            if (TiempoActual >= TiempoMax)
            {
                TiempoActual = TiempoMax;
                TiempoCambiado?.Invoke(TiempoActual);
                Pausa();          // llega al final y se detiene
                return;
            }

            TiempoCambiado?.Invoke(TiempoActual);
        }

        private double Clamp(double t)
        {
            if (t < TiempoMin) return TiempoMin;
            if (t > TiempoMax) return TiempoMax;
            return t;
        }
    }
}
