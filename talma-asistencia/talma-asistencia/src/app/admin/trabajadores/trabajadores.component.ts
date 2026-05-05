import { Component, OnInit, OnDestroy, signal, ViewChild, ElementRef } from '@angular/core';
import { CommonModule }   from '@angular/common';
import { FormsModule }    from '@angular/forms';
import { HttpClient }     from '@angular/common/http';
import { environment }    from '../../../environments/environment';

interface Area  { id: number; nombre: string; }
interface Cargo { id: number; nombre: string; }

interface Trabajador {
  id:            number;
  dni:           string;
  nombres:       string;
  apellidos:     string;
  correo:        string | null;
  telefono:      string | null;
  idArea:        number;
  idCargo:       number;
  area:          string;
  cargo:         string;
  estado:        boolean;
  fotoUrl:       string | null;
}

@Component({
  selector: 'app-trabajadores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './trabajadores.component.html',
  styleUrls: ['./trabajadores.component.scss']
})
export class TrabajadoresComponent implements OnInit, OnDestroy {

  readonly API = environment.apiUrl;

  trabajadores  = signal<Trabajador[]>([]);
  areas         = signal<Area[]>([]);
  cargos        = signal<Cargo[]>([]);
  cargando      = signal(false);
  guardando     = signal(false);
  mensaje       = signal('');
  mensajeError  = signal(false);
  mostrarForm   = signal(false);
  editando      = signal<Trabajador | null>(null);
  busqueda      = '';

  // Modal facial
  mostrarFacial   = signal(false);
  trabajadorFacial = signal<Trabajador | null>(null);
  fotoCapturada   = signal<string | null>(null);
  guardandoFoto   = signal(false);
  streamCamara:   MediaStream | null = null;
  camaraActiva    = signal(false);
  mensajeFacial   = signal('');

  @ViewChild('videoEl') videoEl!: ElementRef<HTMLVideoElement>;
  @ViewChild('canvasEl') canvasEl!: ElementRef<HTMLCanvasElement>;

  form = {
    dni: '', nombres: '', apellidos: '',
    correo: '', telefono: '',
    idArea: 0, idCargo: 0, estado: true
  };

  constructor(private http: HttpClient) {}

  ngOnInit()    { this.cargarTodo(); }
  ngOnDestroy() { this.detenerCamara(); }

  get trabajadoresActivos() { return this.trabajadores().filter(t => t.estado).length; }

  get trabajadoresFiltrados(): Trabajador[] {
    if (!this.busqueda) return this.trabajadores();
    const b = this.busqueda.toLowerCase();
    return this.trabajadores().filter(t =>
      `${t.nombres} ${t.apellidos}`.toLowerCase().includes(b) || t.dni.includes(b)
    );
  }

  cargarTodo() {
    this.cargando.set(true);
    this.http.get<Area[]>(`${this.API}/areas`).subscribe(a => this.areas.set(a));
    this.http.get<Cargo[]>(`${this.API}/cargos`).subscribe(c => this.cargos.set(c));
    this.http.get<Trabajador[]>(`${this.API}/trabajadores`).subscribe({
      next: t => { this.trabajadores.set(t); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  abrirFormNuevo() {
    this.editando.set(null);
    this.form = { dni:'', nombres:'', apellidos:'', correo:'', telefono:'', idArea:0, idCargo:0, estado:true };
    this.mostrarForm.set(true);
  }

  abrirFormEditar(t: Trabajador) {
    this.editando.set(t);
    this.form = {
      dni: t.dni, nombres: t.nombres, apellidos: t.apellidos,
      correo: t.correo ?? '', telefono: t.telefono ?? '',
      idArea: t.idArea, idCargo: t.idCargo, estado: t.estado
    };
    this.mostrarForm.set(true);
  }

  cerrarForm() { this.mostrarForm.set(false); }

  guardar() {
    if (!this.form.dni || this.form.dni.length !== 8) { this.mostrarMensaje('El DNI debe tener 8 dígitos', true); return; }
    if (!this.form.nombres || !this.form.apellidos)   { this.mostrarMensaje('Completa nombres y apellidos', true); return; }
    if (!this.form.idArea || !this.form.idCargo)      { this.mostrarMensaje('Selecciona área y cargo', true); return; }

    this.guardando.set(true);
    const payload = { ...this.form, correo: this.form.correo||null, telefono: this.form.telefono||null };

    const req = this.editando()
      ? this.http.put(`${this.API}/trabajadores/${this.editando()!.id}`, payload)
      : this.http.post(`${this.API}/trabajadores`, payload);

    req.subscribe({
      next: () => { this.guardando.set(false); this.mostrarForm.set(false); this.mostrarMensaje('Guardado ✓', false); this.cargarTodo(); },
      error: (e) => { this.guardando.set(false); this.mostrarMensaje(e.error?.mensaje ?? 'Error', true); }
    });
  }

  toggleEstado(t: Trabajador) {
    const payload = { dni:t.dni, nombres:t.nombres, apellidos:t.apellidos, correo:t.correo, telefono:t.telefono, idArea:t.idArea, idCargo:t.idCargo, estado:!t.estado };
    this.http.put(`${this.API}/trabajadores/${t.id}`, payload).subscribe({
      next: () => { this.mostrarMensaje(`Trabajador ${!t.estado?'activado':'desactivado'}`, false); this.cargarTodo(); },
      error: () => this.mostrarMensaje('Error al cambiar estado', true)
    });
  }

  // ── FACIAL ───────────────────────────────────────────────────
  abrirFacial(t: Trabajador) {
    this.trabajadorFacial.set(t);
    this.fotoCapturada.set(t.fotoUrl);
    this.mensajeFacial.set('');
    this.mostrarFacial.set(true);
    // Iniciar cámara después de que el DOM se renderice
    setTimeout(() => this.iniciarCamara(), 300);
  }

  cerrarFacial() {
    this.detenerCamara();
    this.mostrarFacial.set(false);
    this.fotoCapturada.set(null);
    this.trabajadorFacial.set(null);
  }

  async iniciarCamara() {
    try {
      this.streamCamara = await navigator.mediaDevices.getUserMedia({ video: { width:640, height:480, facingMode:'user' } });
      const video = this.videoEl?.nativeElement;
      if (video) { video.srcObject = this.streamCamara; video.play(); this.camaraActiva.set(true); }
    } catch {
      this.mensajeFacial.set('No se pudo acceder a la cámara. Verifica los permisos.');
    }
  }

  detenerCamara() {
    this.streamCamara?.getTracks().forEach(t => t.stop());
    this.streamCamara = null;
    this.camaraActiva.set(false);
  }

  capturarFoto() {
    const video  = this.videoEl?.nativeElement;
    const canvas = this.canvasEl?.nativeElement;
    if (!video || !canvas) return;

    canvas.width  = video.videoWidth  || 640;
    canvas.height = video.videoHeight || 480;
    const ctx = canvas.getContext('2d')!;
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

    const base64 = canvas.toDataURL('image/jpeg', 0.85);
    this.fotoCapturada.set(base64);
    this.detenerCamara();
  }

  repetirFoto() {
    this.fotoCapturada.set(null);
    setTimeout(() => this.iniciarCamara(), 200);
  }

  guardarFoto() {
    const t = this.trabajadorFacial();
    const foto = this.fotoCapturada();
    if (!t || !foto) return;

    this.guardandoFoto.set(true);
    this.http.post(`${this.API}/trabajadores/${t.id}/foto`, { fotoBase64: foto }).subscribe({
      next: () => {
        this.guardandoFoto.set(false);
        this.mensajeFacial.set('✅ Foto guardada correctamente');
        this.mostrarMensaje('Foto facial guardada ✓', false);
        this.cargarTodo();
        setTimeout(() => this.cerrarFacial(), 1500);
      },
      error: () => { this.guardandoFoto.set(false); this.mensajeFacial.set('❌ Error al guardar la foto'); }
    });
  }

  private mostrarMensaje(msg: string, esError: boolean) {
    this.mensaje.set(msg);
    this.mensajeError.set(esError);
    setTimeout(() => this.mensaje.set(''), 3500);
  }
}