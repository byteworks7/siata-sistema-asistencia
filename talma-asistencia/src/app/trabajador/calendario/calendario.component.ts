import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule }    from '@angular/common';
import { HttpClient }      from '@angular/common/http';
import { AuthService }     from '../../core/services/auth.service';
import { environment }     from '../../../environments/environment';
import { ResumenDiaComponent } from '../resumen-dia/resumen-dia.component';

interface DiaApi {
  fecha:           string;
  diaSemana:       string;
  estado:          string;
  horaEntrada:     string | null;
  horaSalida:      string | null;
  horarioEntrada:  string | null;
  horarioSalida:   string | null;
  estadoSalida:    string | null;
  minutosTardanza: number;
  minutosSalidaAnticipada: number;
  corregidoPorAdmin: boolean;
  observacion:     string | null;
}

interface CalendarioApi {
  mes:              number;
  anio:             number;
  nombreTrabajador: string;
  cargo:            string;
  area:             string;
  dias:             DiaApi[];
  totalPuntuales:   number;
  totalATiempo:     number;
  totalTardanzas:   number;
  totalFaltas:      number;
  totalDescansos:   number;
}

interface DiaMes extends DiaApi {
  numero: number;
  esHoy:  boolean;
}

@Component({
  selector: 'app-calendario',
  standalone: true,
  imports: [CommonModule, ResumenDiaComponent],
  templateUrl: './calendario.component.html',
  styleUrls: ['./calendario.component.scss']
})
export class CalendarioComponent implements OnInit {

  readonly API = environment.apiUrl;

  mesActual  = signal(new Date().getMonth() + 1);
  anioActual = signal(new Date().getFullYear());
  dias       = signal<DiaMes[]>([]);
  cargando   = signal(true);
  diaSeleccionado = signal<DiaMes | null>(null);

  totalPuntuales   = signal(0);
  totalTardanzas   = signal(0);
  totalFaltas      = signal(0);
  totalDescansos   = signal(0);
  totalPermisos    = signal(0);
  totalSuspensiones = signal(0);

  readonly meses = ['Enero','Febrero','Marzo','Abril','Mayo','Junio',
                    'Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre'];
  readonly diasSemana = ['Dom','Lun','Mar','Mié','Jue','Vie','Sáb'];

  idTrabajador = computed(() => this.auth.usuario()!.id);
  mesNombre    = computed(() => this.meses[this.mesActual() - 1]);

  grilla = computed(() => {
    const dias = this.dias();
    if (!dias.length) return [];
    const primer = dias[0];
    const inicio = new Date(primer.fecha + 'T00:00:00').getDay();
    const vacios: null[] = Array(inicio).fill(null);
    return [...vacios, ...dias];
  });

  constructor(private http: HttpClient, private auth: AuthService) {}

  ngOnInit() { this.cargarCalendario(); }

  cargarCalendario() {
    this.cargando.set(true);
    const url = `${this.API}/calendario/${this.idTrabajador()}?mes=${this.mesActual()}&anio=${this.anioActual()}`;

    this.http.get<CalendarioApi>(url).subscribe({
      next: (data) => {
        const hoy = new Date().toISOString().slice(0, 10);
        const diasMapeados: DiaMes[] = data.dias.map(d => ({
          ...d,
          numero: parseInt(d.fecha.slice(8, 10)),
          esHoy:  d.fecha === hoy,
        }));
        this.dias.set(diasMapeados);
        this.totalPuntuales.set(data.totalPuntuales + (data.totalATiempo ?? 0));
        this.totalTardanzas.set(data.totalTardanzas);
        this.totalFaltas.set(data.totalFaltas);
        this.totalDescansos.set(data.totalDescansos);
        this.totalPermisos.set(
          diasMapeados.filter(d => d.estado === 'VACACIONES').length
        );
        this.totalSuspensiones.set(
          diasMapeados.filter(d => d.estado === 'SUSPENSION').length
        );
        this.cargando.set(false);
      },
      error: () => this.cargando.set(false)
    });
  }

  mesPrev() {
    if (this.mesActual() === 1) { this.mesActual.set(12); this.anioActual.update(a => a - 1); }
    else this.mesActual.update(m => m - 1);
    this.cargarCalendario();
  }

  mesSig() {
    if (this.mesActual() === 12) { this.mesActual.set(1); this.anioActual.update(a => a + 1); }
    else this.mesActual.update(m => m + 1);
    this.cargarCalendario();
  }

  seleccionarDia(dia: DiaMes) { this.diaSeleccionado.set(dia); }
  cerrarResumen()              { this.diaSeleccionado.set(null); }

  getClaseDia(dia: DiaMes): string {
    if (dia.esHoy) return 'dia-hoy';
    switch (dia.estado) {
      case 'PUNTUAL':
      case 'A_TIEMPO':   return 'dia-puntual';
      case 'TARDANZA':   return 'dia-tardanza';
      case 'FALTA':      return 'dia-falta';
      case 'DESCANSO':   return 'dia-descanso';
      case 'VACACIONES': return 'dia-permiso';
      case 'SUSPENSION': return 'dia-suspension';
      default:           return 'dia-pendiente';
    }
  }

  getIconoDia(dia: DiaMes): string {
    switch (dia.estado) {
      case 'PUNTUAL':
      case 'A_TIEMPO':   return '✓';
      case 'TARDANZA':   return '!';
      case 'FALTA':      return '✕';
      case 'DESCANSO':   return 'D';
      case 'VACACIONES': return 'V';
      case 'SUSPENSION': return '🚫';
      default:           return '';
    }
  }

  getLabelDia(dia: DiaMes): string {
    switch (dia.estado) {
      case 'DESCANSO':   return 'DESCANSO';
      case 'VACACIONES': return 'VACACIONES';
      case 'SUSPENSION': return 'SUSPENSIÓN';
      default:           return '';
    }
  }
}