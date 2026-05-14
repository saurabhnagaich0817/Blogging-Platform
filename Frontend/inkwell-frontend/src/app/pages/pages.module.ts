import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SharedModule } from '../shared/shared.module';
import { ExploreComponent } from './explore/explore.component';
import { DigestComponent } from './digest/digest.component';
import { CommunityComponent } from './community/community.component';

@NgModule({
  declarations: [
    ExploreComponent,
    DigestComponent,
    CommunityComponent
  ],
  imports: [
    CommonModule,
    SharedModule
  ],
  exports: [
    ExploreComponent,
    DigestComponent,
    CommunityComponent
  ]
})
export class PagesModule { }
