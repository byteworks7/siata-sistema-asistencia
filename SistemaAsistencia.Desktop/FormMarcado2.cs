using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SistemaAsistencia.Desktop
{
    public partial class FormMarcado2 : Form
    {
        // ── Colores ───────────────────────────────────────────────
        private readonly Color C_AZUL = Color.FromArgb(0, 48, 135);
        private readonly Color C_VERDE = Color.FromArgb(106, 170, 0);
        private readonly Color C_VERDE_BTN = Color.FromArgb(22, 163, 74);
        private readonly Color C_AZUL_BTN = Color.FromArgb(37, 99, 235);
        private readonly Color C_BG = Color.FromArgb(240, 242, 245);
        private readonly Color C_WHITE = Color.White;
        private readonly Color C_BORDER = Color.FromArgb(224, 229, 238);
        private readonly Color C_KEY_BG = Color.FromArgb(245, 247, 250);
        private readonly Color C_TEXTO = Color.FromArgb(26, 26, 46);
        private readonly Color C_SUBTEXTO = Color.FromArgb(150, 160, 180);
        private readonly Color C_ROJO = Color.FromArgb(220, 38, 38);

        // ── Estado ────────────────────────────────────────────────
        private readonly HttpClient _httpClient;
        private const string API_URL = "http://localhost:5071/api";
        private string _tipoMarcado;
        private dynamic? _trabajadorEncontrado = null;
        private string _dni = "";
        private string? _encodingGenerado = null; // encoding generado desde foto BD
        private System.Windows.Forms.Timer _reloj = null!;

        // ── Facial ────────────────────────────────────────────────
        private FaceService _faceService = null!;
        private System.Windows.Forms.Timer _timerCamara = null!;
        private System.Windows.Forms.Timer _timerCuenta = null!;
        private int _cuentaRegresiva = 5;
        private bool _procesandoFacial = false;

        // ── Controles ─────────────────────────────────────────────
        private Label lblReloj = null!, lblFecha = null!;
        private Label lblDniDisplay = null!;
        private Label lblBuscando = null!;

        private Panel panelDer = null!;
        private Panel panelEspera = null!;
        private Panel panelTrabajador = null!;
        private PictureBox picFoto = null!;
        private Label lblNombre = null!;
        private Label lblCargoArea = null!;
        private Label lblMensaje = null!;
        private Panel panelSemana = null!;
        private Panel[] panelesDias = null!;
        private Label[] lblDiaNombre = null!;
        private Label[] lblDiaHora = null!;
        private Label[] lblDiaEstado = null!;

        // Panel facial
        private Panel panelFacial = null!;
        private PictureBox picCamara = null!;
        private Label lblFacialMsg = null!;
        private Label lblCuenta = null!;
        private Button btnCancelarFacial = null!;

        // ── Constructor ───────────────────────────────────────────
        public FormMarcado2(string tipo = "ENTRADA")
        {
            InitializeComponent();
            _tipoMarcado = tipo;
            _httpClient = new HttpClient();
            _faceService = new FaceService();
            Build();
            IniciarReloj();
        }

        // ══════════════════════════════════════════════════════════
        // BUILD
        // ══════════════════════════════════════════════════════════
        private void Build()
        {
            this.Text = "Talma — Control de Asistencia";
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = C_BG;
            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) this.Close(); };

            BuildHeader();
            BuildFooter();

            var main = new Panel { Dock = DockStyle.Fill, BackColor = C_BG };
            this.Controls.Add(main);
            main.BringToFront();

            BuildIzquierdo(main);
            BuildDerecho(main);

            this.Load += (s, e) => { AjustarLayout(); IniciarCamara(); };
            this.FormClosing += (s, e) => LimpiarRecursos();
            main.Resize += (s, e) => AjustarLayout();
        }

        private void IniciarCamara()
        {
            bool ok = _faceService.IniciarCamara(0);
            if (!ok)
                System.Diagnostics.Debug.WriteLine("Sin cámara disponible — modo manual");
        }

        private void LimpiarRecursos()
        {
            _timerCamara?.Stop();
            _timerCuenta?.Stop();
            _faceService?.Dispose();
        }

        // ── HEADER ────────────────────────────────────────────────
        private void BuildHeader()
        {
            var h = new Panel { Height = 70, Dock = DockStyle.Top, BackColor = C_AZUL };
            h.Paint += (s, pe) =>
                pe.Graphics.FillRectangle(new SolidBrush(C_VERDE), 0, h.Height - 4, h.Width, 4);
            this.Controls.Add(h);

            var logo = new Panel { Size = new Size(90, 38), Location = new Point(16, 16), BackColor = C_VERDE };
            logo.Controls.Add(new Label
            {
                Text = "TALMA",
                Font = new Font("Arial Black", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });
            h.Controls.Add(logo);

            h.Controls.Add(new Panel { Size = new Size(2, 38), Location = new Point(118, 16), BackColor = Color.FromArgb(60, 255, 255, 255) });

            string icono = _tipoMarcado == "ENTRADA" ? "→" : "←";
            string titulo = _tipoMarcado == "ENTRADA" ? "REGISTRAR ENTRADA" : "REGISTRAR SALIDA";

            h.Controls.Add(new Label { Text = $"{icono}  {titulo}", Font = new Font("Arial", 15, FontStyle.Bold), ForeColor = Color.White, Location = new Point(132, 10), AutoSize = true, BackColor = Color.Transparent });
            h.Controls.Add(new Label { Text = "Talma Servicios Aeroportuarios", Font = new Font("Arial", 9), ForeColor = C_VERDE, Location = new Point(134, 40), AutoSize = true, BackColor = Color.Transparent });

            lblReloj = new Label { Font = new Font("Arial Black", 20, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, Size = new Size(220, 36), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblFecha = new Label { Font = new Font("Arial", 9), ForeColor = Color.FromArgb(180, 210, 255), AutoSize = false, Size = new Size(220, 20), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            h.Controls.Add(lblReloj);
            h.Controls.Add(lblFecha);
            h.Layout += (s, e) => { lblReloj.Location = new Point(h.Width - 232, 10); lblFecha.Location = new Point(h.Width - 232, 44); };
        }

        private void BuildFooter()
        {
            var f = new Panel { Height = 34, Dock = DockStyle.Bottom, BackColor = C_AZUL };
            f.Paint += (s, pe) => pe.Graphics.FillRectangle(new SolidBrush(C_VERDE), 0, 0, f.Width, 3);
            f.Controls.Add(new Label { Text = "Talma Servicios Aeroportuarios  —  Comprometidos con la excelencia operacional", Font = new Font("Arial", 9), ForeColor = Color.FromArgb(120, 160, 220), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
            this.Controls.Add(f);
        }

        // ── PANEL IZQUIERDO ───────────────────────────────────────
        private void BuildIzquierdo(Panel main)
        {
            var izq = new Panel { Width = 340, Dock = DockStyle.Left, BackColor = C_WHITE };
            izq.Paint += (s, pe) => pe.Graphics.FillRectangle(new SolidBrush(C_VERDE), izq.Width - 4, 0, 4, izq.Height);
            main.Controls.Add(izq);

            izq.Controls.Add(new Label { Text = "NÚMERO DE DNI", Font = new Font("Arial", 8, FontStyle.Bold), ForeColor = C_SUBTEXTO, Location = new Point(20, 20), AutoSize = true, BackColor = Color.Transparent });

            var dp = new Panel { Size = new Size(300, 72), Location = new Point(20, 42), BackColor = C_KEY_BG };
            dp.Paint += (s, pe) =>
            {
                pe.Graphics.DrawRectangle(new Pen(C_BORDER, 2), 1, 1, dp.Width - 2, dp.Height - 2);
                pe.Graphics.FillRectangle(new SolidBrush(C_VERDE), 0, dp.Height - 3, dp.Width, 3);
            };
            izq.Controls.Add(dp);

            lblDniDisplay = new Label { Font = new Font("Courier New", 34, FontStyle.Bold), ForeColor = C_TEXTO, AutoSize = false, Size = new Size(300, 72), Location = new Point(0, 0), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            dp.Controls.Add(lblDniDisplay);

            var cur = new Label { Text = "|", Font = new Font("Arial", 28, FontStyle.Bold), ForeColor = C_VERDE, AutoSize = true, BackColor = Color.Transparent, Location = new Point(270, 18) };
            dp.Controls.Add(cur);
            cur.BringToFront();
            var tc = new System.Windows.Forms.Timer { Interval = 530 };
            tc.Tick += (s, e) => cur.Visible = !cur.Visible;
            tc.Start();

            lblBuscando = new Label { Text = "", Font = new Font("Arial", 9), ForeColor = Color.FromArgb(37, 99, 235), Size = new Size(300, 22), Location = new Point(20, 120), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            izq.Controls.Add(lblBuscando);

            var teclado = new Panel { Size = new Size(300, 320), Location = new Point(20, 148), BackColor = Color.Transparent };
            izq.Controls.Add(teclado);

            string[] teclas = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "⌫", "0", "✕" };
            int tW = 92, tH = 72, gapX = 12, gapY = 10;

            for (int i = 0; i < 12; i++)
            {
                int col = i % 3, row = i / 3;
                string t = teclas[i];
                bool esBorrar = t == "⌫";
                bool esLimpiar = t == "✕";

                var btn = new Button
                {
                    Text = t,
                    Font = new Font(esBorrar || esLimpiar ? "Arial" : "Arial Black", esBorrar || esLimpiar ? 18 : 24, FontStyle.Bold),
                    Size = new Size(tW, tH),
                    Location = new Point(col * (tW + gapX), row * (tH + gapY)),
                    BackColor = esBorrar ? Color.FromArgb(255, 251, 235) : esLimpiar ? Color.FromArgb(255, 245, 245) : C_KEY_BG,
                    ForeColor = esBorrar ? Color.FromArgb(217, 119, 6) : esLimpiar ? C_ROJO : C_TEXTO,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = t
                };
                btn.FlatAppearance.BorderColor = esBorrar ? Color.FromArgb(253, 230, 138) : esLimpiar ? Color.FromArgb(252, 165, 165) : C_BORDER;
                btn.FlatAppearance.BorderSize = 2;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(229, 231, 235);
                btn.Click += Teclado_Click;
                teclado.Controls.Add(btn);
            }

            var btnReg = new Button
            {
                Text = "←   REGRESAR",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Size = new Size(300, 46),
                Location = new Point(20, izq.Height - 60),
                BackColor = C_KEY_BG,
                ForeColor = C_SUBTEXTO,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnReg.FlatAppearance.BorderColor = C_BORDER;
            btnReg.FlatAppearance.BorderSize = 2;
            btnReg.Click += (s, e) => this.Close();
            izq.Controls.Add(btnReg);
            izq.Resize += (s, e) => btnReg.Location = new Point(20, izq.Height - 60);
        }

        // ── PANEL DERECHO ─────────────────────────────────────────
        private void BuildDerecho(Panel main)
        {
            panelDer = new Panel { Dock = DockStyle.Fill, BackColor = C_BG };
            main.Controls.Add(panelDer);

            panelEspera = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            panelDer.Controls.Add(panelEspera);
            panelEspera.Controls.Add(new Label
            {
                Text = "Ingresa tu DNI para continuar",
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 210, 220),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            });

            panelTrabajador = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Visible = false };
            panelDer.Controls.Add(panelTrabajador);
            panelTrabajador.BringToFront();

            picFoto = new PictureBox { Size = new Size(100, 100), BackColor = C_WHITE, SizeMode = PictureBoxSizeMode.Zoom };
            picFoto.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(C_VERDE, 3);
                pe.Graphics.DrawEllipse(pen, 2, 2, picFoto.Width - 5, picFoto.Height - 5);
            };
            panelTrabajador.Controls.Add(picFoto);

            lblNombre = MkLabel("", new Font("Arial Black", 22, FontStyle.Bold), C_TEXTO, ContentAlignment.MiddleCenter);
            lblCargoArea = MkLabel("", new Font("Arial", 11, FontStyle.Bold), C_VERDE, ContentAlignment.MiddleCenter);
            lblMensaje = MkLabel("", new Font("Arial", 10), Color.FromArgb(80, 100, 140), ContentAlignment.MiddleCenter);
            panelTrabajador.Controls.Add(lblNombre);
            panelTrabajador.Controls.Add(lblCargoArea);
            panelTrabajador.Controls.Add(lblMensaje);

            panelSemana = new Panel { Height = 130, BackColor = C_WHITE };
            panelSemana.Paint += (s, pe) => pe.Graphics.DrawRectangle(new Pen(C_BORDER, 1), 0, 0, panelSemana.Width - 1, panelSemana.Height - 1);
            panelTrabajador.Controls.Add(panelSemana);

            panelSemana.Controls.Add(new Label { Text = "ASISTENCIA DE LA SEMANA ACTUAL", Font = new Font("Arial", 8, FontStyle.Bold), ForeColor = C_SUBTEXTO, Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });

            string[] dn = { "LUN", "MAR", "MIÉ", "JUE", "VIE", "SÁB" };
            panelesDias = new Panel[6]; lblDiaNombre = new Label[6]; lblDiaHora = new Label[6]; lblDiaEstado = new Label[6];

            for (int i = 0; i < 6; i++)
            {
                panelesDias[i] = new Panel { BackColor = C_KEY_BG, Location = new Point(0, 24) };
                panelSemana.Controls.Add(panelesDias[i]);

                lblDiaNombre[i] = new Label { Text = dn[i], Font = new Font("Arial", 9, FontStyle.Bold), ForeColor = C_SUBTEXTO, Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
                panelesDias[i].Controls.Add(lblDiaNombre[i]);

                lblDiaHora[i] = new Label { Text = "--:--", Font = new Font("Courier New", 10, FontStyle.Bold), ForeColor = C_SUBTEXTO, AutoSize = false, Height = 24, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
                panelesDias[i].Controls.Add(lblDiaHora[i]);
                lblDiaHora[i].Location = new Point(0, 28);

                lblDiaEstado[i] = new Label { Text = "—", Font = new Font("Arial", 8), ForeColor = C_SUBTEXTO, AutoSize = false, Height = 18, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
                panelesDias[i].Controls.Add(lblDiaEstado[i]);
                lblDiaEstado[i].Location = new Point(0, 56);
            }

            BuildPanelFacial();
            panelDer.Resize += (s, e) => AjustarDerecho();
        }

        private void BuildPanelFacial()
        {
            panelFacial = new Panel { BackColor = C_WHITE, Visible = false };
            panelTrabajador.Controls.Add(panelFacial);

            picCamara = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Black, BorderStyle = BorderStyle.None };
            panelFacial.Controls.Add(picCamara);

            lblFacialMsg = new Label
            {
                Text = "📷  Mire a la cámara para registrar",
                Font = new Font("Arial", 11, FontStyle.Bold),
                ForeColor = C_TEXTO,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = false
            };
            panelFacial.Controls.Add(lblFacialMsg);

            lblCuenta = new Label
            {
                Text = "5",
                Font = new Font("Arial Black", 32, FontStyle.Bold),
                ForeColor = C_VERDE,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = false
            };
            panelFacial.Controls.Add(lblCuenta);

            btnCancelarFacial = new Button
            {
                Text = "Cancelar",
                Font = new Font("Arial", 10),
                BackColor = C_KEY_BG,
                ForeColor = C_SUBTEXTO,
                FlatStyle = FlatStyle.Flat,
                Height = 36,
                Cursor = Cursors.Hand
            };
            btnCancelarFacial.FlatAppearance.BorderColor = C_BORDER;
            btnCancelarFacial.FlatAppearance.BorderSize = 1;
            btnCancelarFacial.Click += (s, e) => DetenerFacial();
            panelFacial.Controls.Add(btnCancelarFacial);
        }

        // ══════════════════════════════════════════════════════════
        // LAYOUT
        // ══════════════════════════════════════════════════════════
        private void AjustarLayout() => AjustarDerecho();

        private void AjustarDerecho()
        {
            if (panelDer == null || lblNombre == null) return;

            int w = panelDer.Width;
            int padX = Math.Max(30, (int)(w * 0.06));
            int btnW = w - padX * 2;
            int y = 20;

            picFoto.Location = new Point((w - picFoto.Width) / 2, y);
            y += picFoto.Height + 14;

            void Lay(Control c, int h) { c.Location = new Point(padX, y); c.Width = btnW; c.Height = h; y += h + 6; }

            Lay(lblNombre, 48);
            Lay(lblCargoArea, 24);
            Lay(lblMensaje, 24);
            y += 8;

            panelSemana.Location = new Point(padX, y);
            panelSemana.Width = btnW;
            y += panelSemana.Height + 16;

            if (panelesDias != null && btnW > 10)
            {
                int gap = 4, colW = (btnW - gap * 5) / 6;
                for (int i = 0; i < 6; i++)
                {
                    panelesDias[i].Location = new Point(i * (colW + gap), 24);
                    panelesDias[i].Size = new Size(colW, 102);
                    lblDiaHora[i].Width = colW;
                    lblDiaEstado[i].Width = colW;
                }
            }

            if (panelFacial != null)
            {
                int fh = Math.Min(200, panelDer.Height - y - 20);
                panelFacial.Location = new Point(padX, y);
                panelFacial.Size = new Size(btnW, fh);

                int camW = Math.Min(btnW - 160, 240);
                int camH = fh - 50;
                picCamara.Location = new Point(8, 8);
                picCamara.Size = new Size(camW, camH);

                int rx = camW + 16;
                lblFacialMsg.Location = new Point(rx, 8);
                lblFacialMsg.Size = new Size(btnW - rx - 8, 40);
                lblCuenta.Location = new Point(rx, 52);
                lblCuenta.Size = new Size(btnW - rx - 8, 60);
                btnCancelarFacial.Location = new Point(rx, fh - 44);
                btnCancelarFacial.Width = btnW - rx - 8;
            }
        }

        // ══════════════════════════════════════════════════════════
        // TECLADO
        // ══════════════════════════════════════════════════════════
        private void Teclado_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            string tag = btn.Tag?.ToString() ?? "";

            switch (tag)
            {
                case "⌫": if (_dni.Length > 0) _dni = _dni[..^1]; break;
                case "✕": _dni = ""; LimpiarTodo(); break;
                default: if (_dni.Length < 8) _dni += tag; break;
            }

            ActualizarDisplay();
            if (_dni.Length == 8) _ = BuscarTrabajador(_dni);
        }

        private void ActualizarDisplay()
        {
            lblDniDisplay.Text = _dni;
            if (_dni.Length < 8) lblBuscando.Text = "";
        }

        // ══════════════════════════════════════════════════════════
        // BÚSQUEDA
        // ══════════════════════════════════════════════════════════
        private async Task BuscarTrabajador(string dni)
        {
            lblBuscando.Text = "⏳ Buscando...";
            lblBuscando.ForeColor = Color.FromArgb(37, 99, 235);
            try
            {
                var resp = await _httpClient.GetAsync($"{API_URL}/trabajadores/dni/{dni}");
                if (!resp.IsSuccessStatusCode)
                {
                    lblBuscando.Text = "✗  DNI no encontrado";
                    lblBuscando.ForeColor = C_ROJO;
                    return;
                }

                var json = await resp.Content.ReadAsStringAsync();
                _trabajadorEncontrado = JsonConvert.DeserializeObject(json);

                int id = (int)_trabajadorEncontrado!.id;
                string nombre = $"{_trabajadorEncontrado!.nombres} {_trabajadorEncontrado!.apellidos}";
                string? foto = _trabajadorEncontrado!.fotoUrl;
                string? cargo = _trabajadorEncontrado!.cargo;
                string? area = _trabajadorEncontrado!.area;

                // ── GENERAR ENCODING DESDE FOTO EN BD ────────────
                _encodingGenerado = null;
                if (!string.IsNullOrEmpty(foto))
                {
                    try
                    {
                        // La foto está en base64 — convertir a Bitmap y generar encoding
                        string base64 = foto;
                        // Si tiene prefijo data:image/...;base64, quitarlo
                        int coma = base64.IndexOf(',');
                        if (coma >= 0) base64 = base64[(coma + 1)..];

                        byte[] bytes = Convert.FromBase64String(base64);
                        using var ms = new System.IO.MemoryStream(bytes);
                        using var bmp = new Bitmap(ms);
                        _encodingGenerado = _faceService.GenerarEncodingDesdeBitmap(bmp);

                        if (_encodingGenerado != null)
                            System.Diagnostics.Debug.WriteLine("✅ Encoding generado desde foto de BD");
                        else
                            System.Diagnostics.Debug.WriteLine("⚠️ No se detectó rostro en la foto de BD");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error generando encoding: {ex.Message}");
                        _encodingGenerado = null;
                    }
                }

                lblBuscando.Text = "";
                await VerificarYCorregirTipo(id);
                MostrarTrabajador(nombre, foto, cargo, area);
                await CargarSemana(id);
                IniciarFacial();
            }
            catch
            {
                lblBuscando.Text = "✗  Error de conexión";
                lblBuscando.ForeColor = C_ROJO;
            }
        }

        // ══════════════════════════════════════════════════════════
        // FACIAL
        // ══════════════════════════════════════════════════════════
        private void IniciarFacial()
        {
            if (!_faceService.CamaraDisponible)
            {
                MostrarBotonesManuales();
                return;
            }

            if (string.IsNullOrEmpty(_encodingGenerado))
            {
                // Sin foto o sin rostro detectado en foto — modo manual
                lblFacialMsg.Text = "⚠️  Sin datos faciales — verifique identidad";
                lblFacialMsg.ForeColor = Color.FromArgb(217, 119, 6);
                lblCuenta.Visible = false;
                panelFacial.Visible = true;
                MostrarBotonesManuales();
                return;
            }

            // Iniciar reconocimiento facial
            _cuentaRegresiva = 5;
            _procesandoFacial = false;

            string accion = _tipoMarcado == "ENTRADA" ? "su ENTRADA" : "su SALIDA";
            lblFacialMsg.Text = $"📷  Mire a la cámara\npara registrar {accion}";
            lblFacialMsg.ForeColor = C_TEXTO;
            lblCuenta.Text = "5";
            lblCuenta.ForeColor = C_VERDE;
            lblCuenta.Visible = true;
            panelFacial.Visible = true;

            _timerCamara = new System.Windows.Forms.Timer { Interval = 33 };
            _timerCamara.Tick += (s, e) => ActualizarCamara();
            _timerCamara.Start();

            _timerCuenta = new System.Windows.Forms.Timer { Interval = 1000 };
            _timerCuenta.Tick += async (s, e) => await TickCuenta();
            _timerCuenta.Start();
        }

        private void ActualizarCamara()
        {
            if (_procesandoFacial) return;
            try
            {
                var bmp = _faceService.CapturarFrameConRostro(out bool hayRostro);
                if (bmp != null)
                {
                    var anterior = picCamara.Image;
                    picCamara.Image = bmp;
                    picCamara.BackColor = hayRostro ? Color.FromArgb(106, 170, 0) : Color.Black;
                    anterior?.Dispose();
                }
            }
            catch { }
        }

        private async Task TickCuenta()
        {
            if (_procesandoFacial) return;

            if (!string.IsNullOrEmpty(_encodingGenerado))
            {
                bool coincide = await Task.Run(() =>
                    _faceService.CompararConCamara(_encodingGenerado!, 0.35));

                if (coincide)
                {
                    _procesandoFacial = true;
                    DetenerTimersFacial();
                    lblFacialMsg.Text = "✅  ¡Identidad confirmada!";
                    lblFacialMsg.ForeColor = C_VERDE_BTN;
                    lblCuenta.Visible = false;
                    await Task.Delay(600);
                    await ConfirmarMarcado();
                    return;
                }
            }

            _cuentaRegresiva--;
            lblCuenta.Text = _cuentaRegresiva.ToString();

            if (_cuentaRegresiva <= 2)
                lblCuenta.ForeColor = C_ROJO;
            else if (_cuentaRegresiva <= 3)
                lblCuenta.ForeColor = Color.FromArgb(234, 88, 12);

            if (_cuentaRegresiva <= 0)
            {
                DetenerTimersFacial();
                lblFacialMsg.Text = "❌  Rostro no coincide\nIntente nuevamente";
                lblFacialMsg.ForeColor = C_ROJO;
                lblCuenta.Visible = false;

                await Task.Delay(2000);
                if (!IsDisposed)
                {
                    panelFacial.Visible = false;
                    MostrarBotonesManuales();
                }
            }
        }

        private void DetenerFacial()
        {
            DetenerTimersFacial();
            panelFacial.Visible = false;
            MostrarBotonesManuales();
        }

        private void DetenerTimersFacial()
        {
            _timerCamara?.Stop();
            _timerCamara?.Dispose();
            _timerCuenta?.Stop();
            _timerCuenta?.Dispose();
            picCamara.Image = null;
        }

        // Botones manuales como respaldo
        private Button? _btnConfirmarManual = null;
        private Button? _btnNoSoyYo = null;

        private void MostrarBotonesManuales()
        {
            if (_btnConfirmarManual != null && !_btnConfirmarManual.IsDisposed)
            {
                _btnConfirmarManual.Visible = true;
                _btnNoSoyYo!.Visible = true;
                return;
            }

            int w = panelDer.Width;
            int padX = Math.Max(30, (int)(w * 0.06));
            int btnW = w - padX * 2;
            int y = panelSemana.Bottom + 16;
            if (panelFacial.Visible) y = panelFacial.Bottom + 10;

            _btnConfirmarManual = new Button
            {
                Text = _tipoMarcado == "ENTRADA"
                                ? "✅   SÍ, SOY YO — REGISTRAR ENTRADA"
                                : "🔵   SÍ, SOY YO — REGISTRAR SALIDA",
                Font = new Font("Arial Black", 12, FontStyle.Bold),
                Size = new Size(btnW, 58),
                Location = new Point(padX, y),
                BackColor = _tipoMarcado == "ENTRADA" ? C_VERDE_BTN : C_AZUL_BTN,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnConfirmarManual.FlatAppearance.BorderSize = 0;
            _btnConfirmarManual.Click += async (s, e) => await ConfirmarMarcado();
            panelTrabajador.Controls.Add(_btnConfirmarManual);

            _btnNoSoyYo = new Button
            {
                Text = "❌   NO SOY YO",
                Font = new Font("Arial", 11, FontStyle.Bold),
                Size = new Size(btnW, 42),
                Location = new Point(padX, y + 66),
                BackColor = C_WHITE,
                ForeColor = C_ROJO,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnNoSoyYo.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
            _btnNoSoyYo.FlatAppearance.BorderSize = 2;
            _btnNoSoyYo.Click += (s, e) => LimpiarTodo();
            panelTrabajador.Controls.Add(_btnNoSoyYo);
        }

        // ══════════════════════════════════════════════════════════
        // DETECCIÓN AUTOMÁTICA ENTRADA/SALIDA
        // ══════════════════════════════════════════════════════════
        private async Task VerificarYCorregirTipo(int idTrabajador)
        {
            try
            {
                var hoy = DateTime.Now;
                var resp = await _httpClient.GetAsync(
                    $"{API_URL}/asistencias/trabajador/{idTrabajador}?mes={hoy.Month}&anio={hoy.Year}");

                if (!resp.IsSuccessStatusCode) { SetMensajeGenerico(); return; }

                var json = await resp.Content.ReadAsStringAsync();
                var lista = JsonConvert.DeserializeObject<List<dynamic>>(json);

                string fechaHoy = hoy.ToString("yyyy-MM-dd");
                dynamic? hoyReg = null;
                if (lista != null)
                    foreach (var a in lista)
                        if ((string)a.fecha == fechaHoy) { hoyReg = a; break; }

                if (_tipoMarcado == "SALIDA" && (hoyReg == null || hoyReg.horaEntrada == null))
                {
                    _tipoMarcado = "ENTRADA";
                    string hs = await ObtenerHoraSalida(idTrabajador);
                    lblMensaje.Text = $"💡 ¿Quisiste decir ENTRADA? Salida: {hs}";
                    lblMensaje.ForeColor = Color.FromArgb(217, 119, 6);
                    return;
                }

                if (_tipoMarcado == "ENTRADA" && hoyReg != null &&
                    hoyReg.horaEntrada != null && hoyReg.horaSalida == null)
                {
                    _tipoMarcado = "SALIDA";
                    string heStr = (string?)hoyReg.horaEntrada ?? "";
                    if (heStr.Length > 5) heStr = heStr[..5];
                    lblMensaje.Text = $"💡 Ya marcaste entrada ({heStr}). ¿Vas a salir?";
                    lblMensaje.ForeColor = Color.FromArgb(37, 99, 235);
                    return;
                }

                SetMensajeGenerico();
            }
            catch { SetMensajeGenerico(); }
        }

        private void SetMensajeGenerico()
        {
            lblMensaje.Text = _tipoMarcado == "ENTRADA" ? "¡Buen turno hoy!" : "¡Gracias por tu jornada! 👋";
            lblMensaje.ForeColor = _tipoMarcado == "ENTRADA"
                ? Color.FromArgb(80, 120, 60)
                : Color.FromArgb(37, 99, 235);
        }

        private async Task<string> ObtenerHoraSalida(int idTrabajador)
        {
            try
            {
                var resp = await _httpClient.GetAsync($"{API_URL}/horarios/trabajador/{idTrabajador}");
                if (!resp.IsSuccessStatusCode) return "--:--";
                var json = await resp.Content.ReadAsStringAsync();
                var horarios = JsonConvert.DeserializeObject<List<dynamic>>(json);
                return horarios == null || horarios.Count == 0 ? "--:--" : (string?)horarios[0].horaSalida ?? "--:--";
            }
            catch { return "--:--"; }
        }

        // ══════════════════════════════════════════════════════════
        // MOSTRAR TRABAJADOR
        // ══════════════════════════════════════════════════════════
        private void MostrarTrabajador(string nombre, string? fotoBase64, string? cargo, string? area)
        {
            panelEspera.Visible = false;
            panelTrabajador.Visible = true;
            panelTrabajador.BringToFront();

            picFoto.Image = null;
            picFoto.BackColor = C_KEY_BG;

            if (!string.IsNullOrEmpty(fotoBase64))
            {
                try
                {
                    string base64 = fotoBase64;
                    int coma = base64.IndexOf(',');
                    if (coma >= 0) base64 = base64[(coma + 1)..];
                    byte[] bytes = Convert.FromBase64String(base64);
                    using var ms = new System.IO.MemoryStream(bytes);
                    picFoto.Image = new Bitmap(ms);
                    picFoto.BackColor = C_WHITE;
                }
                catch { }
            }

            lblNombre.Text = nombre.ToUpper();

            var partes = new List<string>();
            if (!string.IsNullOrEmpty(cargo)) partes.Add(cargo.ToUpper());
            if (!string.IsNullOrEmpty(area)) partes.Add(area.ToUpper());
            lblCargoArea.Text = string.Join("  •  ", partes);

            panelFacial.Visible = false;
            if (_btnConfirmarManual != null) _btnConfirmarManual.Visible = false;
            if (_btnNoSoyYo != null) _btnNoSoyYo.Visible = false;

            AjustarDerecho();
        }

        private void MostrarEspera()
        {
            panelEspera.Visible = true;
            panelTrabajador.Visible = false;
            _trabajadorEncontrado = null;
            _encodingGenerado = null;
        }

        // ══════════════════════════════════════════════════════════
        // SEMANA
        // ══════════════════════════════════════════════════════════
        private async Task CargarSemana(int idTrabajador)
        {
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    panelesDias[i].BackColor = C_KEY_BG;
                    lblDiaNombre[i].ForeColor = C_SUBTEXTO;
                    lblDiaHora[i].Text = "--:--";
                    lblDiaHora[i].ForeColor = C_SUBTEXTO;
                    lblDiaEstado[i].Text = "—";
                    lblDiaEstado[i].ForeColor = C_SUBTEXTO;
                }

                var resp = await _httpClient.GetAsync($"{API_URL}/calendario/{idTrabajador}/semana");
                if (!resp.IsSuccessStatusCode) return;

                var json = await resp.Content.ReadAsStringAsync();
                var dias = JsonConvert.DeserializeObject<List<dynamic>>(json);
                if (dias == null) return;

                for (int i = 0; i < Math.Min(dias.Count, 6); i++)
                {
                    var d = dias[i];
                    bool esD = (bool)d.esDescanso;
                    bool esH = (bool)d.esHoy;
                    string eE = (string?)d.estadoEntrada ?? "SIN_REGISTRO";
                    string eS = (string?)d.estadoSalida ?? "PENDIENTE";
                    string hE = (string?)d.horaEntrada ?? "--:--";

                    if (esH)
                    {
                        lblDiaNombre[i].ForeColor = C_VERDE;
                        int ci = i;
                        panelesDias[i].Paint += (s, pe) =>
                        {
                            pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            using var pen = new Pen(C_VERDE, 2);
                            pe.Graphics.DrawRectangle(pen, 1, 1, panelesDias[ci].Width - 3, panelesDias[ci].Height - 3);
                        };
                    }

                    if (esD)
                    {
                        panelesDias[i].BackColor = Color.FromArgb(248, 250, 252);
                        lblDiaHora[i].Text = "—";
                        lblDiaHora[i].ForeColor = Color.FromArgb(200, 210, 220);
                        lblDiaEstado[i].Text = "Desc.";
                        lblDiaEstado[i].ForeColor = Color.FromArgb(200, 210, 220);
                        continue;
                    }

                    lblDiaHora[i].Text = hE;

                    (Color bg, Color fg, string txt) = (eE, eS) switch
                    {
                        (var e, "SALIDA_NO_REGISTRADA") when e is "PUNTUAL" or "A_TIEMPO" or "TARDANZA"
                            => (Color.FromArgb(245, 240, 255), Color.FromArgb(124, 58, 237), "Sin salida"),
                        (var e, "SALIDA_ANTICIPADA") when e is "PUNTUAL" or "A_TIEMPO"
                            => (Color.FromArgb(255, 251, 235), Color.FromArgb(217, 119, 6), "S.Anticip."),
                        ("PUNTUAL", _) => (Color.FromArgb(240, 255, 244), Color.FromArgb(22, 163, 74), "Puntual"),
                        ("A_TIEMPO", _) => (Color.FromArgb(240, 255, 244), Color.FromArgb(22, 163, 74), "A tiempo"),
                        ("TARDANZA", _) => (Color.FromArgb(255, 247, 237), Color.FromArgb(234, 88, 12), "Tardanza"),
                        ("FALTA", _) => (Color.FromArgb(255, 245, 245), C_ROJO, "Falta"),
                        ("SIN_HORARIO", _) => (Color.FromArgb(239, 246, 255), Color.FromArgb(37, 99, 235), "Sin horario"),
                        _ => (esH ? Color.FromArgb(240, 255, 240) : C_KEY_BG,
                              esH ? C_VERDE : C_SUBTEXTO,
                              esH ? "Hoy" : "Pendiente")
                    };

                    panelesDias[i].BackColor = bg;
                    lblDiaHora[i].ForeColor = fg;
                    lblDiaEstado[i].Text = txt;
                    lblDiaEstado[i].ForeColor = fg;
                    if (eE == "FALTA") { lblDiaHora[i].Text = "✕"; lblDiaHora[i].ForeColor = C_ROJO; }
                }
            }
            catch { }
        }

        // ══════════════════════════════════════════════════════════
        // CONFIRMAR
        // ══════════════════════════════════════════════════════════
        private async Task ConfirmarMarcado()
        {
            if (_trabajadorEncontrado == null) return;
            if (_btnConfirmarManual != null && !_btnConfirmarManual.IsDisposed)
            {
                _btnConfirmarManual.Enabled = false;
                _btnConfirmarManual.Text = "Registrando...";
            }

            try
            {
                var datos = new { dni = _dni, tipo = _tipoMarcado };
                var content = new StringContent(JsonConvert.SerializeObject(datos), Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync($"{API_URL}/asistencias/marcar", content);
                var resJson = await resp.Content.ReadAsStringAsync();
                dynamic r = JsonConvert.DeserializeObject(resJson)!;

                string nombre = lblNombre.Text;
                string cargoArea = lblCargoArea.Text;

                if ((bool)r.exito)
                {
                    LimpiarTodo();
                    FormOverlay.Mostrar(this, (string)r.estado, (string)r.hora, nombre, cargoArea);
                }
                else
                {
                    LimpiarTodo();
                    FormOverlay.Mostrar(this, "ERROR", "ERROR", "", "", (string)r.mensaje, 3500);
                }
            }
            catch
            {
                LimpiarTodo();
                FormOverlay.Mostrar(this, "ERROR", "SIN CONEXIÓN", "", "", "Verifique que el servidor esté activo.", 3500);
            }
        }

        // ══════════════════════════════════════════════════════════
        // LIMPIAR
        // ══════════════════════════════════════════════════════════
        private void LimpiarTodo()
        {
            DetenerTimersFacial();
            _dni = "";
            _trabajadorEncontrado = null;
            _encodingGenerado = null;
            lblDniDisplay.Text = "";
            lblBuscando.Text = "";
            _btnConfirmarManual = null;
            _btnNoSoyYo = null;
            MostrarEspera();
        }

        // ══════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════
        private static Label MkLabel(string txt, Font font, Color color, ContentAlignment align) =>
            new Label { Text = txt, Font = font, ForeColor = color, AutoSize = false, TextAlign = align, BackColor = Color.Transparent };

        private void IniciarReloj()
        {
            _reloj = new System.Windows.Forms.Timer { Interval = 1000 };
            _reloj.Tick += (s, e) =>
            {
                if (lblReloj != null) lblReloj.Text = DateTime.Now.ToString("hh:mm:ss tt").ToUpper();
                if (lblFecha != null) lblFecha.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
            };
            _reloj.Start();
            lblReloj.Text = DateTime.Now.ToString("hh:mm:ss tt").ToUpper();
            lblFecha.Text = DateTime.Now.ToString("dddd, dd MMMM yyyy");
        }
    }
}