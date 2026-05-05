import { Component, signal } from '@angular/core';
import { CommonModule }      from '@angular/common';
import { FormsModule }       from '@angular/forms';
import { Router }            from '@angular/router';
import { AuthService }       from '../../core/services/auth.service';

@Component({
  selector: 'app-login-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login-admin.component.html',
  styleUrls: ['./login-admin.component.scss']
})
export class LoginAdminComponent {

  username     = '';
  password     = '';
  showPass     = false;
  error        = signal('');
  cargando     = signal(false);

  constructor(private auth: AuthService, private router: Router) {}

  login() {
    this.error.set('');
    if (!this.username || !this.password) { this.error.set('Complete todos los campos'); return; }

    this.cargando.set(true);
    this.auth.loginAdmin(this.username, this.password).subscribe({
      next: () => { this.cargando.set(false); this.router.navigate(['/admin/dashboard']); },
      error: (e) => {
        this.cargando.set(false);
        this.error.set(e.status === 401 ? 'Credenciales incorrectas' : 'Error de conexión');
      }
    });
  }

  volver() { this.router.navigate(['/auth/portal']); }
}