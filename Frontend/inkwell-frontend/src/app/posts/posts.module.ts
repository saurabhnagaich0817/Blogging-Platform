import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { PostsListComponent } from './posts-list/posts-list.component';
import { PostDetailComponent } from './post-detail/post-detail.component';
import { PostCreateComponent } from './post-create/post-create.component';
import { PostEditComponent } from './post-edit/post-edit.component';
import { MyPostsComponent } from './my-posts/my-posts.component';
import { SavedPostsComponent } from './saved-posts/saved-posts.component';
import { AuthGuard } from '../core/guards/auth.guard';

const routes: Routes = [
  { path: '', component: MyPostsComponent, canActivate: [AuthGuard] },
  { path: 'explore', component: PostsListComponent },
  { path: 'create', component: PostCreateComponent, canActivate: [AuthGuard] },
  { path: 'my', redirectTo: '', pathMatch: 'full' },
  { path: 'saved', component: SavedPostsComponent, canActivate: [AuthGuard] },
  { path: 'edit/:id', component: PostEditComponent, canActivate: [AuthGuard] },
  { path: ':id', component: PostDetailComponent }
];

@NgModule({
  declarations: [
    PostsListComponent,
    PostDetailComponent,
    PostCreateComponent,
    PostEditComponent,
    MyPostsComponent,
    SavedPostsComponent
  ],
  imports: [
    RouterModule.forChild(routes),
    SharedModule
  ]
})
export class PostsModule {}
