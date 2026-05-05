import { Routes } from '@angular/router';

export const AUTH_ROUTES: Routes = [
  { path: '', redirectTo: 'portal', pathMatch: 'full' },

  {
    path: 'portal',
    loadComponent: () =>
      import('./portal/portal.component').then(m => m.PortalComponent)
  },

  {
    path: 'trabajador',
    loadComponent: () =>
      import('./login-trabajador/login-trabajador.component')
        .then(m => m.LoginTrabajadorComponent)
  },

  {
    path: 'admin',
    loadComponent: () =>
      import('./login-admin/login-admin.component')
        .then(m => m.LoginAdminComponent)
  },
];