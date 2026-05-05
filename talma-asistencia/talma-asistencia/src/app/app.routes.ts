import { Routes }          from '@angular/router';
import { authGuard }       from './core/guards/auth.guard';
import { adminGuard }      from './core/guards/admin.guard';
import { trabajadorGuard } from './core/guards/trabajador.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/auth/portal', pathMatch: 'full' },

  {
    path: 'auth',
    loadChildren: () =>
      import('./auth/auth.routes').then(m => m.AUTH_ROUTES)
  },

  {
    path: 'trabajador',
    canActivate: [authGuard, trabajadorGuard],
    loadChildren: () =>
      import('./trabajador/trabajador.routes').then(m => m.TRABAJADOR_ROUTES)
  },

  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadChildren: () =>
      import('./admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },

  { path: '**', redirectTo: '/auth/portal' }
];