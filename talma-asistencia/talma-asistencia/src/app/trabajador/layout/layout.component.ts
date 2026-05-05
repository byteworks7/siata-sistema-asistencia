import { Component, computed } from '@angular/core';
import { CommonModule }        from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService }         from '../../core/services/auth.service';

@Component({
  selector: 'app-layout-trabajador',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './layout.component.html',
  styleUrls: ['./layout.component.scss']
})
export class LayoutComponent {

  usuario = computed(() => this.auth.usuario());

  constructor(private auth: AuthService) {}

  logout() { this.auth.logout(); }
}