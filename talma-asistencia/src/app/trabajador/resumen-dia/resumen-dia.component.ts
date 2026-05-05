import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-resumen-dia',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './resumen-dia.component.html',
  styleUrls: ['./resumen-dia.component.scss']
})
export class ResumenDiaComponent {
  @Input() dia: any;
  @Output() cerrar = new EventEmitter<void>();

  get fechaFormateada(): string {
    if (!this.dia?.fecha) return '';
    const d = new Date(this.dia.fecha + 'T00:00:00');
    return d.toLocaleDateString('es-PE', {
      weekday: 'long', year: 'numeric',
      month: 'long', day: 'numeric'
    });
  }

  get estadoLabel(): string {
    switch (this.dia?.estado) {
      case 'PUNTUAL':      return 'Puntual';
      case 'A_TIEMPO':     return 'A tiempo';
      case 'TARDANZA':     return 'Tardanza';
      case 'FALTA':        return 'Falta';
      case 'DESCANSO':     return 'Día de descanso';
      case 'SIN_REGISTRO': return 'Sin registro';
      default:             return '—';
    }
  }

  get estadoClase(): string {
    switch (this.dia?.estado) {
      case 'PUNTUAL':
      case 'A_TIEMPO':  return 'verde';
      case 'TARDANZA':  return 'naranja';
      case 'FALTA':     return 'rojo';
      default:          return 'gris';
    }
  }

  get salidaLabel(): string {
    switch (this.dia?.estadoSalida) {
      case 'REGISTRADA':           return 'Registrada';
      case 'SALIDA_ANTICIPADA':    return 'Salida anticipada';
      case 'SALIDA_NO_REGISTRADA': return 'No registrada';
      case 'PENDIENTE':            return 'Pendiente';
      default:                     return '—';
    }
  }
}