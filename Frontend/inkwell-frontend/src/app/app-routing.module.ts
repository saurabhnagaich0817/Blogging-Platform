/**
 * Main Routing Module for InkWell Frontend.
 * Implements Lazy Loading for feature modules to optimize initial load time.
 * Protected routes are guarded by AuthGuard for Role-Based Access Control.
 */
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { ExploreComponent } from './pages/explore/explore.component';
import { DigestComponent } from './pages/digest/digest.component';
import { CommunityComponent } from './pages/community/community.component';

const routes: Routes = [
  { path: '', redirectTo: 'posts', pathMatch: 'full' },
  {
    path: 'auth',
    loadChildren: () => import('./auth/auth.module').then(m => m.AuthModule)
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./dashboard/dashboard.module').then(m => m.DashboardModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'admin',
    loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'profile',
    loadChildren: () => import('./profile/profile.module').then(m => m.ProfileModule)
  },
  {
    path: 'posts',
    loadChildren: () => import('./posts/posts.module').then(m => m.PostsModule)
  },
  {
    path: 'compose',
    redirectTo: 'posts/create',
    pathMatch: 'full'
  },
  {
    path: 'explore',
    component: ExploreComponent
  },
  {
    path: 'digest',
    component: DigestComponent
  },
  {
    path: 'community',
    component: CommunityComponent
  },
  {
    path: 'media',
    loadChildren: () => import('./media/media.module').then(m => m.MediaModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'newsletter',
    loadChildren: () => import('./newsletter/newsletter.module').then(m => m.NewsletterModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'access-denied',
    loadChildren: () => import('./shared/access-denied/access-denied.module').then(m => m.AccessDeniedModule)
  },
  { path: '**', redirectTo: 'posts' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {}
