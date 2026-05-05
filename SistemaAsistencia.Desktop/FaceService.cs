using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace SistemaAsistencia.Desktop
{
    /// <summary>
    /// Servicio de reconocimiento facial usando OpenCV.
    /// - Detecta rostros con Haar Cascade
    /// - Compara usando histogramas normalizados (simple y rápido)
    /// - Guarda/carga encodings como JSON en BD
    /// </summary>
    public class FaceService : IDisposable
    {
        private VideoCapture? _camara;
        private CascadeClassifier _detector;
        private bool _disposed = false;

        // Ruta del clasificador Haar (se descarga con OpenCvSharp4)
        private static readonly string _cascadePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "haarcascade_frontalface_default.xml");

        public bool CamaraDisponible { get; private set; } = false;

        public FaceService()
        {
            _detector = new CascadeClassifier();

            // Intentar cargar el cascade
            if (File.Exists(_cascadePath))
                _detector.Load(_cascadePath);
        }

        // ── Iniciar cámara ────────────────────────────────────────
        public bool IniciarCamara(int deviceIndex = 0)
        {
            try
            {
                _camara = new VideoCapture(deviceIndex);
                CamaraDisponible = _camara.IsOpened();
                return CamaraDisponible;
            }
            catch
            {
                CamaraDisponible = false;
                return false;
            }
        }

        // ── Capturar frame actual como Bitmap ─────────────────────
        public Bitmap? CapturarFrame()
        {
            if (_camara == null || !_camara.IsOpened()) return null;
            using var frame = new Mat();
            _camara.Read(frame);
            if (frame.Empty()) return null;
            return BitmapConverter.ToBitmap(frame);
        }

        // ── Capturar frame con rectángulo del rostro detectado ────
        public Bitmap? CapturarFrameConRostro(out bool rostroDetectado)
        {
            rostroDetectado = false;
            if (_camara == null || !_camara.IsOpened()) return null;

            using var frame = new Mat();
            _camara.Read(frame);
            if (frame.Empty()) return null;

            var rostros = DetectarRostros(frame);
            rostroDetectado = rostros.Length > 0;

            // Dibujar rectángulo verde si hay rostro
            if (rostroDetectado)
            {
                foreach (var r in rostros)
                    Cv2.Rectangle(frame, r, new Scalar(106, 170, 0), 2);
            }

            return BitmapConverter.ToBitmap(frame);
        }

        // ── Detectar rostros en un Mat ────────────────────────────
        private OpenCvSharp.Rect[] DetectarRostros(Mat frame)
        {
            if (_detector.Empty()) return Array.Empty<OpenCvSharp.Rect>();

            using var gris = new Mat();
            Cv2.CvtColor(frame, gris, ColorConversionCodes.BGR2GRAY);
            Cv2.EqualizeHist(gris, gris);

            return _detector.DetectMultiScale(
                gris,
                scaleFactor: 1.1,
                minNeighbors: 5,
                minSize: new OpenCvSharp.Size(80, 80));
        }

        // ── Generar encoding desde frame de cámara ────────────────
        /// <summary>
        /// Captura el rostro actual y genera un encoding (vector de histograma).
        /// Retorna el encoding como string JSON para guardar en BD.
        /// </summary>
        public string? GenerarEncodingDesdeCaptura()
        {
            if (_camara == null || !_camara.IsOpened()) return null;

            using var frame = new Mat();
            _camara.Read(frame);
            if (frame.Empty()) return null;

            return GenerarEncodingDesdeMat(frame);
        }

        // ── Generar encoding desde archivo de imagen ──────────────
        public string? GenerarEncodingDesdeArchivo(string rutaImagen)
        {
            if (!File.Exists(rutaImagen)) return null;
            using var img = Cv2.ImRead(rutaImagen);
            if (img.Empty()) return null;
            return GenerarEncodingDesdeMat(img);
        }

        // ── Generar encoding desde Bitmap ─────────────────────────
        public string? GenerarEncodingDesdeBitmap(Bitmap bmp)
        {
            using var mat = BitmapConverter.ToMat(bmp);
            return GenerarEncodingDesdeMat(mat);
        }

        // ── Lógica interna de encoding ────────────────────────────
        private string? GenerarEncodingDesdeMat(Mat frame)
        {
            try
            {
                var rostros = DetectarRostros(frame);
                if (rostros.Length == 0) return null;

                // Tomar el rostro más grande
                var rostro = rostros[0];
                foreach (var r in rostros)
                    if (r.Width * r.Height > rostro.Width * rostro.Height)
                        rostro = r;

                using var recortado = new Mat(frame, rostro);
                using var redim = new Mat();
                Cv2.Resize(recortado, redim, new OpenCvSharp.Size(100, 100));

                using var gris = new Mat();
                Cv2.CvtColor(redim, gris, ColorConversionCodes.BGR2GRAY);

                // Calcular histograma LBP-like (Local Binary Pattern simplificado)
                var encoding = CalcularHistograma(gris);

                return JsonSerializer.Serialize(encoding);
            }
            catch
            {
                return null;
            }
        }

        // ── Calcular histograma normalizado (256 bins) ────────────
        private float[] CalcularHistograma(Mat gris)
        {
            var hist = new Mat();
            var images = new Mat[] { gris };
            var channels = new int[] { 0 };
            var histSize = new int[] { 256 };
            var ranges = new Rangef[] { new Rangef(0, 256) };

            Cv2.CalcHist(images, channels, null, hist, 1, histSize, ranges);
            Cv2.Normalize(hist, hist, 0, 1, NormTypes.MinMax);

            var resultado = new float[256];
            for (int i = 0; i < 256; i++)
                resultado[i] = hist.At<float>(i);

            hist.Dispose();
            return resultado;
        }

        // ── Comparar encoding guardado con rostro actual ──────────
        /// <summary>
        /// Retorna true si el rostro actual coincide con el encoding guardado.
        /// threshold: 0.0 = idéntico, 1.0 = completamente diferente
        /// </summary>
        public bool CompararConCamara(string encodingGuardado, double threshold = 0.35)
        {
            if (_camara == null || !_camara.IsOpened()) return false;

            using var frame = new Mat();
            _camara.Read(frame);
            if (frame.Empty()) return false;

            return CompararEncodings(encodingGuardado, frame, threshold);
        }

        private bool CompararEncodings(string encodingGuardado, Mat frame, double threshold)
        {
            try
            {
                var rostros = DetectarRostros(frame);
                if (rostros.Length == 0) return false;

                var rostro = rostros[0];
                using var recortado = new Mat(frame, rostro);
                using var redim = new Mat();
                Cv2.Resize(recortado, redim, new OpenCvSharp.Size(100, 100));

                using var gris = new Mat();
                Cv2.CvtColor(redim, gris, ColorConversionCodes.BGR2GRAY);

                var histActual = CalcularHistograma(gris);
                var histGuardado = JsonSerializer.Deserialize<float[]>(encodingGuardado);

                if (histGuardado == null) return false;

                // Correlación de histogramas (1.0 = idéntico, 0.0 = diferente)
                double correlacion = CorrelacionHistogramas(histActual, histGuardado);

                // correlacion > (1 - threshold) = son similares
                return correlacion > (1.0 - threshold);
            }
            catch
            {
                return false;
            }
        }

        private double CorrelacionHistogramas(float[] h1, float[] h2)
        {
            if (h1.Length != h2.Length) return 0;

            double sum1 = 0, sum2 = 0, sum12 = 0, sq1 = 0, sq2 = 0;
            int n = h1.Length;

            for (int i = 0; i < n; i++)
            {
                sum1 += h1[i];
                sum2 += h2[i];
                sum12 += h1[i] * h2[i];
                sq1 += h1[i] * h1[i];
                sq2 += h2[i] * h2[i];
            }

            double num = n * sum12 - sum1 * sum2;
            double den = Math.Sqrt((n * sq1 - sum1 * sum1) * (n * sq2 - sum2 * sum2));

            return den == 0 ? 0 : num / den;
        }

        // ── Detener cámara ────────────────────────────────────────
        public void DetenerCamara()
        {
            _camara?.Release();
            _camara?.Dispose();
            _camara = null;
            CamaraDisponible = false;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DetenerCamara();
                _detector.Dispose();
                _disposed = true;
            }
        }
    }
}