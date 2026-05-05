import { Routes } from '@angular/router';
import { LayoutAdminComponent } from './layout/layout.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: LayoutAdminComponent,
    children: [
      { path: '', redirectTo: 'horarios', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'horarios',
        loadComponent: () =>
          import('./horarios/horarios.component').then(m => m.HorariosComponent)
      },
      {
        path: 'trabajadores',
        loadComponent: () =>
          import('./trabajadores/trabajadores.component').then(m => m.TrabajadoresComponent)
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'asistencias',
        loadComponent: () =>
          import('./asistencias/asistencias.component').then(m => m.AsistenciasComponent)
      },
      {
        path: 'amonestaciones',
        loadComponent: () =>
          import('./amonestaciones/amonestaciones.component').then(m => m.AmonestacionesComponent)
      },
    ]
  }
];