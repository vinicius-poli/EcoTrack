import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.component.html',
})
export class HomeComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly estaAutenticado = this.authService.estaAutenticado;
  protected readonly saindo = signal(false);

  protected sair(): void {
    if (this.saindo()) return;

    this.saindo.set(true);

    this.authService.logout().subscribe({
      complete: () => {
        this.saindo.set(false);
        void this.router.navigateByUrl('/login');
      },
    });
  }
}