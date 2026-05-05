import { Routes } from '@angular/router';
import { LayoutComponent } from './layout/layout.component';

export const TRABAJADOR_ROUTES: Routes = [
  {
    path: '',
    component: LayoutComponent,
    children: [
      { path: '', redirectTo: 'calendario', pathMatch: 'full' },
      {
        path: 'calendario',
        loadComponent: () =>
          import('./calendario/calendario.component')
            .then(m => m.CalendarioComponent)
      },
    ]
  }
];