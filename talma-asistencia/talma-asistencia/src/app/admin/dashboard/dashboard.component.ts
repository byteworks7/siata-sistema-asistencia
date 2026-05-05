import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient }             from '@angular/common/http';
import { environment }            from '../../../environments/environment';

interface ResumenHoy {
  totalTrabajadores:  number;
  presentes:          number;
  tardanzas:          number;
  faltas:             number;
  sinRegistro:        number;
  porcentajeAsistencia: number;
}

interface AsistenciaHoy {
  id:              number;
  idTrabajador:    number;
  nombreTrabajador: string;
  cargo:           string;
  area:            string;
  horaEntrada:     string | null;
  horaSalida:      string | null;
  estadoEntrada:   string;
  estadoSalida:    string;
  minutosTardanza: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {

  readonly API = environment.apiUrl;

  cargando     = signal(true);
  asistencias  = signal<AsistenciaHoy[]>([]);
  trabajadores = signal<any[]>([]);
  today        = new Date();

  // Stats calculadas
  get totalTrab()    { return this.trabajadores().filter(t => t.estado).length; }
  get presentes()    { return this.asistencias().filter(a => a.estadoEntrada === 'PUNTUAL' || a.estadoEntrada === 'A_TIEMPO' || a.estadoEntrada === 'TARDANZA').length; }
  get puntuales()    { return this.asistencias().filter(a => a.estadoEntrada === 'PUNTUAL' || a.estadoEntrada === 'A_TIEMPO').length; }
  get tardanzas()    { return this.asistencias().filter(a => a.estadoEntrada === 'TARDANZA').length; }
  get faltas()       { return this.asistencias().filter(a => a.estadoEntrada === 'FALTA').length; }
  get sinRegistro()  { return Math.max(0, this.totalTrab - this.presentes - this.faltas); }
  get porcentaje()   {
    if (!this.totalTrab) return 0;
    return Math.round((this.presentes / this.totalTrab) * 100);
  }

  constructor(private http: HttpClient) {}

  ngOnInit() { this.cargarDatos(); }

  cargarDatos() {
    this.cargando.set(true);
    const hoy = new Date();
    const mes = hoy.getMonth() + 1;
    const anio = hoy.getFullYear();

    this.http.get<any[]>(`${this.API}/trabajadores`).subscribe(t => {
      this.trabajadores.set(t);
    });

    this.http.get<AsistenciaHoy[]>(`${this.API}/asistencias/hoy`).subscribe({
      next: a => { this.asistencias.set(a); this.cargando.set(false); },
      error: () => this.cargando.set(false)
    });
  }

  getClaseEstado(estado: string): string {
    switch (estado) {
      case 'PUNTUAL':
      case 'A_TIEMPO':  return 'estado-puntual';
      case 'TARDANZA':  return 'estado-tardanza';
      case 'FALTA':     return 'estado-falta';
      default:          return 'estado-pendiente';
    }
  }

  getEtiquetaEstado(estado: string): string {
    switch (estado) {
      case 'PUNTUAL':   return 'Puntual';
      case 'A_TIEMPO':  return 'A tiempo';
      case 'TARDANZA':  return 'Tardanza';
      case 'FALTA':     return 'Falta';
      default:          return 'Pendiente';
    }
  }

  recargar() { this.cargarDatos(); }
}