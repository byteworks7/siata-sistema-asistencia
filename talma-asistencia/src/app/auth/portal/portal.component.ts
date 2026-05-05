import { Component } from '@angular/core';
import { Router }    from '@angular/router';

@Component({
  selector: 'app-portal',
  standalone: true,
  templateUrl: './portal.component.html',
  styleUrls: ['./portal.component.scss']
})
export class PortalComponent {
  constructor(private router: Router) {}

  irTrabajador() { this.router.navigate(['/auth/trabajador']); }
  irAdmin()      { this.router.navigate(['/auth/admin']); }
}