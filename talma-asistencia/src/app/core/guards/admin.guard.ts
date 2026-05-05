import { inject }        from '@angular/core';
import { Router }        from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { AuthService }   from '../services/auth.service';

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);

  const usuario = auth.usuario();

  // Sin sesión o sin token → portal
  if (!usuario || !usuario.token) {
    router.navigate(['/auth/portal']);
    return false;
  }

  // Verificar que el token no esté expirado
  try {
    const payload = JSON.parse(atob(usuario.token.split('.')[1]));
    const ahora   = Math.floor(Date.now() / 1000);
    if (payload.exp && payload.exp < ahora) {
      auth.logout();
      return false;
    }
  } catch {
    auth.logout();
    return false;
  }

  // Verificar rol
  if (usuario.rol === 'ADMIN') return true;

  router.navigate(['/auth/portal']);
  return false;
};