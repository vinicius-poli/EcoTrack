import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { HomeComponent } from './home/home.component';
import { CadastrarEmpreendimentoComponent } from './empreendimentos/cadastrar/cadastrar-empreendimento.component';
import { ListarEmpreendimentosComponent } from './empreendimentos/listar/listar-empreendimentos.component';
import { Login } from './auth/login/login';
import { Cadastro } from './auth/cadastro/cadastro';


export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'login', component: Login },
  { path: 'cadastro', component: Cadastro },
  {
  path: 'equipe/novo-membro',
  canActivate: [authGuard],
  loadComponent: () =>
    import('./auth/cadastro/cadastrar-membro.component').then(
      (m) => m.CadastrarMembroComponent,
    ),
  },
  {
    path: 'empreendimentos',
    component: ListarEmpreendimentosComponent,
    canActivate: [authGuard],
  },
  {
    path: 'empreendimentos/cadastrar',
    component: CadastrarEmpreendimentoComponent,
    canActivate: [authGuard],
  },
  // { path: 'condicionantes', canActivate: [authGuard], ... },
  { path: '**', redirectTo: 'home' }
];