using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SistemaAsistencia.Desktop
{
    public class FormOverlay : Form
    {
        private readonly string _estado;
        private readonly string _hora;
        private readonly string _nombre;
        private readonly string _cargoArea;
        private readonly string _mensaje;
        private readonly int _duracionMs;

        private Color _colorMain;
        private Color _colorBg1;
        private Color _colorBg2;
        private string _iconoTxt = "";
        private string _estadoTxt = "";

        private System.Windows.Forms.Timer _timerCierre = null!;
        private System.Windows.Forms.Timer _timerProgress = null!;
        private Panel _pBar = null!;
        private Panel _pTrack = null!;
        private int _tick = 0;
        private int _total = 0;

        public FormOverlay(string estado, string hora, string nombre,
            string cargoArea = "", string mensaje = "", int duracionMs = 4500)
        {
            _estado = estado;
            _hora = hora;
            _nombre = nombre;
            _cargoArea = cargoArea;
            _mensaje = mensaje;
            _duracionMs = duracionMs;

            ConfigurarColores();
            Build();
        }

        private void ConfigurarColores()
        {
            switch (_estado)
            {
                case "PUNTUAL":
                    _colorMain = Color.FromArgb(22, 163, 74);
                    _colorBg1 = Color.FromArgb(3, 30, 12);
                    _colorBg2 = Color.FromArgb(2, 18, 7);
                    _iconoTxt = "✓"; _estadoTxt = "ENTRADA PUNTUAL"; break;
                case "A_TIEMPO":
                    _colorMain = Color.FromArgb(22, 163, 74);
                    _colorBg1 = Color.FromArgb(3, 30, 12);
                    _colorBg2 = Color.FromArgb(2, 18, 7);
                    _iconoTxt = "✓"; _estadoTxt = "ENTRADA A TIEMPO"; break;
                case "TARDANZA":
                    _colorMain = Color.FromArgb(234, 88, 12);
                    _colorBg1 = Color.FromArgb(28, 10, 2);
                    _colorBg2 = Color.FromArgb(16, 6, 1);
                    _iconoTxt = "!"; _estadoTxt = "ENTRADA CON TARDANZA"; break;
                case "SALIDA_ANTICIPADA":
                    _colorMain = Color.FromArgb(217, 119, 6);
                    _colorBg1 = Color.FromArgb(26, 18, 2);
                    _colorBg2 = Color.FromArgb(15, 10, 1);
                    _iconoTxt = "!"; _estadoTxt = "SALIDA ANTICIPADA"; break;
                case "SALIDA":
                case "REGISTRADA":
                    _colorMain = Color.FromArgb(37, 99, 235);
                    _colorBg1 = Color.FromArgb(2, 10, 32);
                    _colorBg2 = Color.FromArgb(1, 6, 20);
                    _iconoTxt = "→"; _estadoTxt = "SALIDA REGISTRADA"; break;
                default:
                    _colorMain = Color.FromArgb(220, 38, 38);
                    _colorBg1 = Color.FromArgb(22, 3, 3);
                    _colorBg2 = Color.FromArgb(13, 2, 2);
                    _iconoTxt = "✕"; _estadoTxt = "ERROR"; break;
            }
        }

        private void Build()
        {
            // ── Form sin bordes, encima del padre ─────────────────
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            // Fondo negro semitransparente — Opacity lo hace ver el form padre
            this.BackColor = Color.FromArgb(20, 20, 20);
            this.Opacity = 0.85; // 85% opaco = 15% transparente
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Cerrar(); };
            this.Click += (s, e) => Cerrar();

            // ── Tarjeta centrada ──────────────────────────────────
            bool esError = _estado == "ERROR";
            int cardW = 420;
            int padX = 24;

            int totalH = 4 + 10 + 18 + 80 + 24
                       + (esError ? 60 : 76)
                       + 34
                       + (!string.IsNullOrEmpty(_cargoArea) ? 24 : 0)
                       + 12 + 50 + 14 + 22 + 14;

            var card = new Panel
            {
                Size = new Size(cardW, totalH),
                BackColor = Color.Transparent
            };
            card.Paint += (s, pe) => PintarCard(pe, card);
            card.Click += (s, e) => Cerrar();
            this.Controls.Add(card);

            // Centrar la tarjeta al cargar y al resize
            this.Load += (s, e) => Centrar(card);
            this.Resize += (s, e) => Centrar(card);

            int cy = 0;

            // Barra color top
            card.Controls.Add(new Panel
            {
                Size = new Size(cardW, 4),
                Location = new Point(0, 0),
                BackColor = _colorMain
            });
            cy = 10;

            // Tag TALMA
            card.Controls.Add(new Label
            {
                Text = "TALMA",
                Font = new Font("Arial Black", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, _colorMain),
                Location = new Point(14, cy),
                AutoSize = true,
                BackColor = Color.Transparent
            });
            cy += 18;

            // Ícono
            var icono = new Panel
            {
                Size = new Size(72, 72),
                Location = new Point((cardW - 72) / 2, cy),
                BackColor = Color.Transparent
            };
            icono.Paint += (s, pe) => PintarIcono(pe, icono);
            card.Controls.Add(icono);
            cy += 80;

            // Estado
            card.Controls.Add(Lbl(_estadoTxt,
                new Font("Arial", 9, FontStyle.Bold), _colorMain,
                cardW, padX, cy, 20));
            cy += 24;

            // Hora
            int horaH = esError ? 56 : 74;
            card.Controls.Add(Lbl(_hora,
                new Font("Courier New", esError ? 20 : 44, FontStyle.Bold),
                _colorMain, cardW, padX, cy, horaH));
            cy += horaH + 4;

            // Nombre
            if (!string.IsNullOrEmpty(_nombre))
            {
                card.Controls.Add(Lbl(_nombre,
                    new Font("Arial Black", 11, FontStyle.Bold),
                    Color.White, cardW, padX, cy, 30));
                cy += 34;
            }

            // Cargo/área
            if (!string.IsNullOrEmpty(_cargoArea))
            {
                card.Controls.Add(Lbl(_cargoArea,
                    new Font("Arial", 9),
                    Color.FromArgb(130, 150, 190),
                    cardW, padX, cy, 20));
                cy += 24;
            }

            // Separador
            card.Controls.Add(new Panel
            {
                Size = new Size(cardW - padX * 2, 1),
                Location = new Point(padX, cy),
                BackColor = Color.FromArgb(40, 255, 255, 255)
            });
            cy += 12;

            // Mensaje
            card.Controls.Add(Lbl(_mensaje,
                new Font("Arial", 10),
                Color.FromArgb(150, 170, 210),
                cardW, padX, cy, 46));
            cy += 50;

            // Progress track
            _pTrack = new Panel
            {
                Size = new Size(cardW - padX * 2, 4),
                Location = new Point(padX, cy),
                BackColor = Color.FromArgb(40, _colorMain)
            };
            card.Controls.Add(_pTrack);

            _pBar = new Panel
            {
                Size = new Size(cardW - padX * 2, 4),
                Location = new Point(0, 0),
                BackColor = _colorMain
            };
            _pTrack.Controls.Add(_pBar);
            cy += 10;

            // Cerrando
            card.Controls.Add(Lbl("CERRANDO...",
                new Font("Arial", 7),
                Color.FromArgb(60, 255, 255, 255),
                cardW, padX, cy, 18,
                ContentAlignment.MiddleRight));

            // ── Timers ────────────────────────────────────────────
            _total = _duracionMs / 50;
            _timerProgress = new System.Windows.Forms.Timer { Interval = 50 };
            _timerProgress.Tick += (s, e) =>
            {
                _tick++;
                if (!_pTrack.IsDisposed && _pTrack.Width > 0)
                {
                    double ratio = 1.0 - (double)_tick / _total;
                    _pBar.Width = (int)(_pTrack.Width * Math.Max(0, ratio));
                }
                if (_tick >= _total) _timerProgress.Stop();
            };

            _timerCierre = new System.Windows.Forms.Timer { Interval = _duracionMs };
            _timerCierre.Tick += (s, e) => Cerrar();

            this.Shown += (s, e) =>
            {
                _pBar.Width = _pTrack.Width;
                _timerProgress.Start();
                _timerCierre.Start();
            };
        }

        private void Centrar(Panel card)
        {
            card.Location = new Point(
                (this.ClientSize.Width - card.Width) / 2,
                (this.ClientSize.Height - card.Height) / 2);
        }

        private void PintarCard(PaintEventArgs pe, Panel card)
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
            using var path = RRect(rect, 14);
            using var gb = new LinearGradientBrush(
                new Rectangle(0, 0, card.Width, card.Height),
                _colorBg1, _colorBg2, 145f);
            g.FillPath(gb, path);

            using var pen = new Pen(Color.FromArgb(65, _colorMain), 1.5f);
            g.DrawPath(pen, path);

            card.Region = new Region(RRect(new Rectangle(0, 0, card.Width, card.Height), 14));
        }

        private void PintarIcono(PaintEventArgs pe, Panel p)
        {
            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(3, 3, p.Width - 6, p.Height - 6);
            using var path = new GraphicsPath();
            path.AddEllipse(rect);

            using var pgb = new PathGradientBrush(path)
            {
                CenterColor = _colorMain,
                SurroundColors = new[] { Color.FromArgb(45, _colorMain) },
                CenterPoint = new PointF(rect.X + rect.Width * 0.35f,
                                            rect.Y + rect.Height * 0.35f)
            };
            g.FillEllipse(pgb, rect);

            using var pen = new Pen(Color.FromArgb(90, _colorMain), 1.5f);
            g.DrawEllipse(pen, rect);

            var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            using var font = new Font("Arial Black", 24, FontStyle.Bold);
            g.DrawString(_iconoTxt, font, Brushes.White,
                new RectangleF(rect.X, rect.Y, rect.Width, rect.Height), sf);
        }

        private void Cerrar()
        {
            _timerCierre?.Stop();
            _timerProgress?.Stop();
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timerCierre?.Dispose();
            _timerProgress?.Dispose();
            base.OnFormClosing(e);
        }

        private static Label Lbl(string txt, Font font, Color color,
            int cardW, int padX, int y, int h,
            ContentAlignment align = ContentAlignment.MiddleCenter) =>
            new Label
            {
                Text = txt,
                Font = font,
                ForeColor = color,
                Size = new Size(cardW - padX * 2, h),
                Location = new Point(padX, y),
                TextAlign = align,
                BackColor = Color.Transparent
            };

        private static GraphicsPath RRect(Rectangle b, int r)
        {
            var p = new GraphicsPath();
            p.AddArc(b.X, b.Y, r * 2, r * 2, 180, 90);
            p.AddArc(b.Right - r * 2, b.Y, r * 2, r * 2, 270, 90);
            p.AddArc(b.Right - r * 2, b.Bottom - r * 2, r * 2, r * 2, 0, 90);
            p.AddArc(b.X, b.Bottom - r * 2, r * 2, r * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        // ── Método estático de conveniencia ───────────────────────
        public static void Mostrar(Form parent, string estado, string hora,
            string nombre, string cargoArea = "", string? msgOverride = null,
            int duracionMs = 4500)
        {
            string msg = msgOverride ?? Mensaje(estado);
            var ov = new FormOverlay(estado, hora, nombre, cargoArea, msg, duracionMs);

            // Mismo tamaño y posición que el form padre
            ov.Bounds = parent.Bounds;
            ov.Show(parent);
        }

        private static string Mensaje(string estado) => estado switch
        {
            "PUNTUAL" => "¡Llegaste antes de tu hora!\nBuen turno hoy 💪",
            "A_TIEMPO" => "¡Bienvenido!\nQue tengas un excelente turno",
            "TARDANZA" => "Recuerda llegar a tiempo.\nLas tardanzas generan amonestaciones.",
            "SALIDA_ANTICIPADA" => "Saliste antes de tu hora.\nEsto quedará registrado.",
            "SALIDA" => "¡Gracias por tu jornada de hoy!\nHasta mañana 👋",
            "REGISTRADA" => "¡Gracias por tu jornada de hoy!\nHasta mañana 👋",
            _ => "Verifique el número e intente nuevamente."
        };
    }
}