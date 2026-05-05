import { Component, signal } from '@angular/core';
import { CommonModule }      from '@angular/common';
import { Router }            from '@angular/router';
import { AuthService }       from '../../core/services/auth.service';

@Component({
  selector: 'app-login-trabajador',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './login-trabajador.component.html',
  styleUrls: ['./login-trabajador.component.scss']
})
export class LoginTrabajadorComponent {

  dni      = signal('');
  error    = signal('');
  cargando = signal(false);

  readonly teclas = ['1','2','3','4','5','6','7','8','9','0','⌫','✕'];

  constructor(private auth: AuthService, private router: Router) {}

  tecla(t: string) {
    if (this.cargando()) return;
    this.error.set('');

    if (t === '⌫') {
      this.dni.update(v => v.slice(0, -1));
    } else if (t === '✕') {
      this.dni.set('');
      this.error.set('');
    } else if (this.dni().length < 8) {
      this.dni.update(v => v + t);
      if (this.dni().length === 8) this.consultar();
    }
  }

  consultar() {
    if (this.dni().length !== 8) {
      this.error.set('Ingresa los 8 dígitos de tu DNI');
      return;
    }
    this.cargando.set(true);
    this.auth.loginTrabajador(this.dni()).subscribe({
      next: () => {
        this.cargando.set(false);
        this.router.navigate(['/trabajador/calendario']);
      },
      error: () => {
        this.cargando.set(false);
        this.error.set('DNI no encontrado. Verifique e intente nuevamente.');
        this.dni.set('');
      }
    });
  }

  volver() { this.router.navigate(['/auth/portal']); }
}