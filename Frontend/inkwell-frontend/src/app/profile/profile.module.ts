import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProfileComponent } from './profile.component';
import { SharedModule } from '../shared/shared.module';

const routes: Routes = [
  { path: ':username', component: ProfileComponent }
];

@NgModule({
  declarations: [ProfileComponent],
  imports: [
    RouterModule.forChild(routes),
    SharedModule
  ]
})
export class ProfileModule {}
