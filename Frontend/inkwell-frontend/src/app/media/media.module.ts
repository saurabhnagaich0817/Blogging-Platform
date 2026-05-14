import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { MediaComponent } from './media.component';

const routes: Routes = [{ path: '', component: MediaComponent }];

@NgModule({
  declarations: [MediaComponent],
  imports: [
    RouterModule.forChild(routes),
    SharedModule
  ]
})
export class MediaModule {}
