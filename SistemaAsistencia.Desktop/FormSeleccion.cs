using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SistemaAsistencia.Desktop
{
    public partial class FormSeleccion : Form
    {
        private System.Windows.Forms.Timer _reloj = null!;
        private Label lblHora = null!;
        private Label lblFecha = null!;

        private readonly Color TALMA_AZUL = Color.FromArgb(0, 48, 135);
        private readonly Color TALMA_VERDE = Color.FromArgb(106, 170, 0);
        private readonly Color VERDE_BTN = Color.FromArgb(22, 163, 74);
        private readonly Color ROJO_BTN = Color.FromArgb(220, 38, 38);

        public FormSeleccion()
        {
            InitializeComponent();
            ConfigurarFormulario();
            IniciarReloj();
        }

        private void ConfigurarFormulario()
        {
            this.Text = "Talma — Control de Asistencia";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(0, 24, 48);
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Application.Exit(); };

            try
            {
                string ruta = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fondo.png");
                if (System.IO.File.Exists(ruta))
                {
                    this.BackgroundImage = Image.FromFile(ruta);
                    this.BackgroundImageLayout = ImageLayout.Stretch;
                }
            }
            catch { }

            Panel overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(145, 0, 20, 55)
            };
            this.Controls.Add(overlay);

            // HEADER
            Panel header = new Panel
            {
                Height = 88,
                Dock = DockStyle.Top,
                BackColor = Color.FromArgb(252, 0, 48, 135)
            };
            header.Paint += (s, pe) =>
                pe.Graphics.FillRectangle(new SolidBrush(TALMA_VERDE), 0, header.Height - 4, header.Width, 4);
            overlay.Controls.Add(header);

            PictureBox picLogoH = new PictureBox
            {
                Size = new Size(165, 76),
                Location = new Point(18, 6),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(0, 48, 135)
            };
            try
            {
                string ruta = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.webp");
                if (System.IO.File.Exists(ruta)) picLogoH.Image = Image.FromFile(ruta);
            }
            catch { }
            header.Controls.Add(picLogoH);

            header.Controls.Add(new Panel { Size = new Size(2, 52), Location = new Point(190, 18), BackColor = Color.FromArgb(80, 255, 255, 255) });
            header.Controls.Add(new Label { Text = "CONTROL DE ASISTENCIA", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.FromArgb(210, 255, 255, 255), Location = new Point(204, 20), AutoSize = true, BackColor = Color.Transparent });
            header.Controls.Add(new Label { Text = "Talma Servicios Aeroportuarios", Font = new Font("Segoe UI", 10), ForeColor = TALMA_VERDE, Location = new Point(204, 52), AutoSize = true, BackColor = Color.Transparent });

            lblHora = new Label { Text = DateTime.Now.ToString("hh:mm:ss tt").ToUpper(), Font = new Font("Segoe UI", 26, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, Size = new Size(320, 52), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            header.Controls.Add(lblHora);

            lblFecha = new Label { Text = DateTime.Now.ToString("dddd, dd MMMM yyyy"), Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(170, 255, 255, 255), AutoSize = false, Size = new Size(320, 28), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            header.Controls.Add(lblFecha);

            // FOOTER
            Panel footer = new Panel
            {
                Height = 88,
                Dock = DockStyle.Bottom,
                BackColor = Color.FromArgb(252, 0, 48, 135)
            };
            footer.Paint += (s, pe) =>
                pe.Graphics.FillRectangle(new SolidBrush(TALMA_VERDE), 0, 0, footer.Width, 4);
            overlay.Controls.Add(footer);

            PictureBox picLogoF = new PictureBox
            {
                Size = new Size(145, 76),
                Location = new Point(18, 6),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(0, 48, 135)
            };
            try
            {
                string ruta = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.webp");
                if (System.IO.File.Exists(ruta)) picLogoF.Image = Image.FromFile(ruta);
            }
            catch { }
            footer.Controls.Add(picLogoF);

            footer.Controls.Add(new Panel { Size = new Size(2, 50), Location = new Point(172, 19), BackColor = Color.FromArgb(80, 255, 255, 255) });
            footer.Controls.Add(new Label { Text = "Talma Servicios Aeroportuarios", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(210, 255, 255, 255), Location = new Point(186, 20), AutoSize = true, BackColor = Color.Transparent });
            footer.Controls.Add(new Label { Text = "Comprometidos con la excelencia operacional", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(140, 255, 255, 255), Location = new Point(186, 50), AutoSize = true, BackColor = Color.Transparent });

            Label lblCopy = new Label { Text = "© 2026", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(90, 255, 255, 255), AutoSize = false, Size = new Size(100, 88), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            footer.Controls.Add(lblCopy);

            // CONTENIDO AL CARGAR
            this.Load += (s, e) => {
                lblHora.Location = new Point(header.Width - 336, 8);
                lblFecha.Location = new Point(header.Width - 336, 56);
                lblCopy.Location = new Point(footer.Width - 110, 0);

                int btnW = (int)(this.Width * 0.60);
                int btnH = (int)((this.Height - 88 - 88) * 0.21);
                int btnX = (this.Width - btnW) / 2;
                int areaH = this.Height - 88 - 88;
                int startY = 88 + (areaH - btnH * 2 - 24 - 44) / 2;

                overlay.Controls.Add(new Label
                {
                    Text = "Selecciona una opción para registrar tu asistencia",
                    Font = new Font("Segoe UI", 13),
                    ForeColor = Color.FromArgb(200, 255, 255, 255),
                    AutoSize = false,
                    Size = new Size(btnW, 36),
                    Location = new Point(btnX, startY - 46),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                });

                var btnEntrada = CrearBoton(VERDE_BTN, Color.FromArgb(74, 222, 128),
                    "ENTRADA", "Registra tu hora de ingreso", "→",
                    new Rectangle(btnX, startY, btnW, btnH));
                btnEntrada.Click += (bs, be) => AbrirMarcado("ENTRADA");
                overlay.Controls.Add(btnEntrada);

                overlay.Controls.Add(new Label
                {
                    Text = new string('─', 80),
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(55, 255, 255, 255),
                    AutoSize = false,
                    Size = new Size(btnW, 24),
                    Location = new Point(btnX, startY + btnH),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                });

                var btnSalida = CrearBoton(ROJO_BTN, Color.FromArgb(248, 113, 113),
                    "SALIDA", "Registra tu hora de salida", "←",
                    new Rectangle(btnX, startY + btnH + 24, btnW, btnH));
                btnSalida.Click += (bs, be) => AbrirMarcado("SALIDA");
                overlay.Controls.Add(btnSalida);
            };
        }

        private Panel CrearBoton(Color bgColor, Color borderColor,
            string titulo, string subtitulo, string simbolo, Rectangle bounds)
        {
            var panel = new Panel
            {
                Size = bounds.Size,
                Location = bounds.Location,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            panel.Paint += (s, pe) => {
                var g = pe.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                int r = 20;
                var rect = new Rectangle(1, 1, panel.Width - 3, panel.Height - 3);

                using var path = RoundedPath(rect, r);
                g.FillPath(new SolidBrush(bgColor), path);
                using var pen = new Pen(borderColor, 2.5f);
                g.DrawPath(pen, path);
                panel.Region = new Region(RoundedPath(new Rectangle(0, 0, panel.Width, panel.Height), r));

                // Círculo ícono
                int cR = (int)(panel.Height * 0.36);
                int cX = 60 + cR;
                int cY = panel.Height / 2;
                g.FillEllipse(new SolidBrush(Color.FromArgb(45, 255, 255, 255)),
                    cX - cR, cY - cR, cR * 2, cR * 2);

                var sfC = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                int symSize = Math.Max(14, (int)(cR * 0.85));
                g.DrawString(simbolo,
                    new Font("Segoe UI", symSize, FontStyle.Bold),
                    Brushes.White,
                    new RectangleF(cX - cR, cY - cR, cR * 2, cR * 2), sfC);

                // Texto centrado — subtítulo más pequeño y más separado
                int tX = cX + cR + 30;
                int tSize = Math.Max(20, (int)(panel.Height * 0.30));
                int sSize = Math.Max(10, (int)(panel.Height * 0.10)); // más pequeño
                float totalTextH = tSize + sSize + 36;                       // más separado
                float textStartY = (panel.Height - totalTextH) / 2f;

                // Título
                g.DrawString(titulo,
                    new Font("Segoe UI", tSize, FontStyle.Bold),
                    Brushes.White,
                    new PointF(tX, textStartY));

                // Subtítulo
                g.DrawString(subtitulo,
                    new Font("Segoe UI", sSize),
                    new SolidBrush(Color.FromArgb(215, 255, 255, 255)),
                    new PointF(tX, textStartY + tSize + 36)); // más separado
            };

            panel.MouseEnter += (s, e) => { panel.BackColor = Color.FromArgb(20, 255, 255, 255); panel.Invalidate(); };
            panel.MouseLeave += (s, e) => { panel.BackColor = Color.Transparent; panel.Invalidate(); };

            return panel;
        }

        private GraphicsPath RoundedPath(Rectangle b, int r)
        {
            var p = new GraphicsPath();
            p.AddArc(b.X, b.Y, r * 2, r * 2, 180, 90);
            p.AddArc(b.Right - r * 2, b.Y, r * 2, r * 2, 270, 90);
            p.AddArc(b.Right - r * 2, b.Bottom - r * 2, r * 2, r * 2, 0, 90);
            p.AddArc(b.X, b.Bottom - r * 2, r * 2, r * 2, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void AbrirMarcado(string tipo)
        {
            var f = new FormMarcado2(tipo);
            f.ShowDialog(this);
        }

        private void IniciarReloj()
        {
            _reloj = new System.Windows.Forms.Timer { Interval = 1000 };
            _reloj.Tick += (s, e) => {
                if (lblHora != null) lblHora.Text = DateTime.Now.ToString("hh:mm:ss tt").ToUpper();
                if (lblFecha != null) lblFecha.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            };
            _reloj.Start();
        }
    }
}