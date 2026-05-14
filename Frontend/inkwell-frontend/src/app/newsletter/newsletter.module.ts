import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { NewsletterComponent } from './newsletter.component';

const routes: Routes = [{ path: '', component: NewsletterComponent }];

@NgModule({
  declarations: [NewsletterComponent],
  imports: [
    RouterModule.forChild(routes),
    SharedModule
  ]
})
export class NewsletterModule {}
