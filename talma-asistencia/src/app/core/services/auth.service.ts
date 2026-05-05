import { Injectable, signal } from '@angular/core';
import { HttpClient }         from '@angular/common/http';
import { Router }             from '@angular/router';
import { tap }                from 'rxjs/operators';
import { environment }        from '../../../environments/environment';

export interface LoginResponse {
  token:  string;
  nombre: string;
  rol:    string;
  id:     number;
  cargo?: string;
  area?:  string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {

  private readonly API = environment.apiUrl;
  private readonly KEY = 'talma_session';

  usuario = signal<LoginResponse | null>(this.cargarSesion());

  esAdmin      = () => this.usuario()?.rol === 'ADMIN';
  esTrabajador = () => this.usuario()?.rol === 'TRABAJADOR';

  constructor(private http: HttpClient, private router: Router) {}

  loginAdmin(username: string, password: string) {
    return this.http.post<LoginResponse>(`${this.API}/auth/login-admin`,
      { username, password }).pipe(tap(res => this.guardarSesion(res)));
  }

  loginTrabajador(dni: string) {
    return this.http.post<LoginResponse>(`${this.API}/auth/login-trabajador`,
      { dni }).pipe(tap(res => this.guardarSesion(res)));
  }

  logout() {
    sessionStorage.removeItem(this.KEY);
    localStorage.removeItem(this.KEY);
    this.usuario.set(null);
    this.router.navigate(['/auth/portal']);
  }

  getToken(): string | null {
    return this.usuario()?.token ?? null;
  }

  isLoggedIn(): boolean {
    return this.usuario() !== null;
  }

  private guardarSesion(res: LoginResponse) {
    sessionStorage.setItem(this.KEY, JSON.stringify(res));
    this.usuario.set(res);
  }

  private cargarSesion(): LoginResponse | null {
    try {
      localStorage.removeItem(this.KEY); // limpiar viejo
      const data = sessionStorage.getItem(this.KEY);
      return data ? JSON.parse(data) : null;
    } catch { return null; }
  }
}