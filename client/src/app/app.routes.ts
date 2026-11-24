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
  {
    path: 'empreendimentos/:id/editar',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./empreendimentos/editar/editar-empreendimento.component').then(
        (m) => m.EditarEmpreendimentoComponent,
      ),
  },
  {
    path: 'empreendimentos/:id/excluir',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./empreendimentos/excluir/excluir-empreendimento.component').then(
        (m) => m.ExcluirEmpreendimentoComponent,
      ),
  },
  {
    path: 'empreendimentos/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./empreendimentos/detalhes/detalhes-empreendimento.component').then(
        (m) => m.DetalhesEmpreendimentoComponent,
      ),
  },
  {
    path: 'licencas',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./licencas/listar/listar-licencas.component').then(
        (m) => m.ListarLicencasComponent,
      ),
  },
  {
    path: 'licencas/cadastrar',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./licencas/cadastrar/cadastrar-licenca.component').then(
        (m) => m.CadastrarLicencaComponent,
      ),
  },
  {
    path: 'licencas/:id/editar',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./licencas/editar/editar-licenca.component').then(
        (m) => m.EditarLicencaComponent,
      ),
  },
  {
    path: 'licencas/:id/excluir',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./licencas/excluir/excluir-licenca.component').then(
        (m) => m.ExcluirLicencaComponent,
      ),
  },
  {
    path: 'licencas/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./licencas/detalhes/detalhes-licenca.component').then(
        (m) => m.DetalhesLicencaComponent,
      ),
  },
  // { path: 'condicionantes', canActivate: [authGuard], ... },
  { path: '**', redirectTo: 'home' },
];