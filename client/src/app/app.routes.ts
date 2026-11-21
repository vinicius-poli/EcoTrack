import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  // Depois você adiciona aqui:
  // { path: 'empreendimentos', ... },
  // { path: 'condicionantes', ... },
  { path: '**', redirectTo: 'home' }
];