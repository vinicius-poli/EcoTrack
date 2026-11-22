import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { CadastrarEmpreendimentoComponent } from './empreendimentos/cadastrar/cadastrar-empreendimento.component';
import { ListarEmpreendimentosComponent } from './empreendimentos/listar/listar-empreendimentos.component';
import { Login } from './auth/login/login';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'home', component: HomeComponent },
  { path: 'login', component: Login },
  { path: 'empreendimentos', component: ListarEmpreendimentosComponent },
  { path: 'empreendimentos/cadastrar', component: CadastrarEmpreendimentoComponent },
  // { path: 'condicionantes', ... },
  { path: '**', redirectTo: 'home' }
];