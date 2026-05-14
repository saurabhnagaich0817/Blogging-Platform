import { Component, OnInit } from '@angular/core';
import { AuthService } from '../core/services/auth.service';
import { PostService } from '../core/services/post.service';
import { User, BaseResponse, AnalyticsResponse } from '../core/models/models';
import { catchError } from 'rxjs/operators';
import { of } from 'rxjs';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-dashboard',
  template: `
    <div class="app-container py-60">
      <header class="dashboard-hero mb-40 animate-in">
        <div class="user-welcome">
           <div class="avatar-glow-ring">
              <div class="avatar-main">{{ (getUserDisplayName().charAt(0) || 'U').toUpperCase() }}</div>
           </div>
           <div class="welcome-text">
              <h1 class="heading-xl">Welcome, <span class="text-gradient">{{ getUserDisplayName() | titlecase }}</span></h1>
              <p class="text-secondary">Here's your personal InkWell command center.</p>
           </div>
        </div>
      </header>

      <!-- Stats Grid (Author/Admin Only) -->
      <div class="premium-stats-grid mb-60 animate-in" style="animation-delay: 0.1s" *ngIf="canViewAnalytics">
        <div class="stat-card-premium">
           <div class="stat-icon-bg amber"><mat-icon>article</mat-icon></div>
           <div class="stat-meta">
              <span class="value">{{ stats.totalStories }}</span>
              <span class="label">Stories Published</span>
           </div>
           <div class="stat-trend positive">+{{ stats.totalStories > 0 ? 2 : 0 }} this week</div>
        </div>

        <div class="stat-card-premium">
           <div class="stat-icon-bg red"><mat-icon>favorite</mat-icon></div>
           <div class="stat-meta">
              <span class="value">{{ stats.totalEngagement }}</span>
              <span class="label">Total Engagement</span>
           </div>
           <div class="stat-trend positive">Trending up</div>
        </div>

         <div class="stat-card-premium">
            <div class="stat-icon-bg gray"><mat-icon>cloud_queue</mat-icon></div>
            <div class="stat-meta">
               <span class="value">{{ stats.storageUsagePercentage }}%</span>
               <span class="label">Storage Used</span>
            </div>
            <div class="stat-trend">Capacity: 5GB</div>
         </div>
      </div>

      <!-- Reader Welcome Banner (Reader Only) -->
      <div class="glass-card reader-banner mb-60 animate-in" *ngIf="isReader" style="animation-delay: 0.1s">
        <div class="banner-content">
          <h2>Upgrade Your Role</h2>
          <p>Request an upgrade to become an Author or Admin and unlock more capabilities.</p>
        </div>
        <div style="display: flex; gap: 12px;">
          <button *ngIf="isReader" class="btn-premium outline" (click)="requestUpgrade('Author')">Become an Author</button>
          <button class="btn-premium outline" (click)="requestUpgrade('Admin')">Request Admin Access</button>
        </div>
      </div>

      <div class="dashboard-layout animate-in" style="animation-delay: 0.2s">
        <main class="dashboard-main">
          <div class="section-top mb-24">
             <h2 class="heading-sm">Quick Orchestration</h2>
             <p class="text-muted">Common tasks and rapid actions.</p>
          </div>

          <div class="quick-grid">
            <div class="action-tile glass-card" routerLink="/posts/create" *ngIf="canCreatePost">
               <div class="tile-icon"><mat-icon>edit_square</mat-icon></div>
               <div class="tile-body">
                  <h3>Compose Post</h3>
                  <p>Write and publish a new story to the community.</p>
               </div>
               <mat-icon class="arrow">chevron_right</mat-icon>
            </div>

            <div class="action-tile glass-card" routerLink="/posts/my" *ngIf="canCreatePost">
               <div class="tile-icon"><mat-icon>library_books</mat-icon></div>
               <div class="tile-body">
                  <h3>My Stories</h3>
                  <p>Manage, edit, or delete your published and draft posts.</p>
               </div>
               <mat-icon class="arrow">chevron_right</mat-icon>
            </div>

            <div class="action-tile glass-card" routerLink="/posts/saved">
               <div class="tile-icon"><mat-icon>bookmarks</mat-icon></div>
               <div class="tile-body">
                  <h3>Reading List</h3>
                  <p>Access stories you've saved for later reading.</p>
               </div>
               <mat-icon class="arrow">chevron_right</mat-icon>
            </div>

            <div class="action-tile glass-card" routerLink="/explore">
               <div class="tile-icon"><mat-icon>explore</mat-icon></div>
               <div class="tile-body">
                  <h3>Explore Feed</h3>
                  <p>Discover trending topics and new writers.</p>
               </div>
               <mat-icon class="arrow">chevron_right</mat-icon>
            </div>

            <div class="action-tile glass-card" routerLink="/media" *ngIf="canCreatePost">
               <div class="tile-icon"><mat-icon>photo_library</mat-icon></div>
               <div class="tile-body">
                  <h3>Asset Library</h3>
                  <p>Manage your images and creative files.</p>
               </div>
               <mat-icon class="arrow">chevron_right</mat-icon>
            </div>
            
            <div class="action-tile glass-card admin-tile" routerLink="/admin" *ngIf="isAdmin">
               <div class="tile-icon"><mat-icon>admin_panel_settings</mat-icon></div>
               <div class="tile-body">
                  <h3>Admin Panel</h3>
                  <p>Manage platform categories, tags, and users.</p>
               </div>
               <mat-icon class="arrow">chevron_right</mat-icon>
            </div>
          </div>
        </main>

        <aside class="dashboard-side">
           <div class="glass-card p-32">
              <h3 class="heading-xs mb-24">Account Identity</h3>
              
              <div class="identity-stack">
                 <div class="id-item">
                    <span class="id-label">Role Privilege</span>
                    <span class="badge-amber" *ngIf="user?.role === 'Admin'"><mat-icon style="font-size: 14px; width:14px; height:14px">shield</mat-icon> {{ user?.role }}</span>
                    <span class="badge-blue" *ngIf="user?.role === 'Author'"><mat-icon style="font-size: 14px; width:14px; height:14px">edit</mat-icon> {{ user?.role }}</span>
                    <span class="badge-green" *ngIf="user?.role === 'Reader'"><mat-icon style="font-size: 14px; width:14px; height:14px">menu_book</mat-icon> {{ user?.role }}</span>
                 </div>
                 <div class="id-item">
                    <span class="id-label">Verified Email</span>
                    <span class="id-value" style="font-size: 0.85rem; color: var(--text-secondary)">{{ user?.email }}</span>
                 </div>
                 <div class="id-item">
                    <span class="id-label">Security Status</span>
                    <span class="id-value" style="color: #22c55e; font-size: 0.85rem;"><mat-icon style="font-size: 14px; vertical-align: middle">lock</mat-icon> Protected</span>
                 </div>
              </div>

              <div class="divider-amber my-24"></div>
              
              <button class="btn-premium full-width outline mt-16" routerLink="/profile/{{ user?.username }}">
                 <mat-icon>person</mat-icon> View Public Profile
              </button>
           </div>
        </aside>
      </div>
    </div>
  `,
  styles: [`
    .my-24 { margin: 24px 0; }

    .dashboard-hero { border-bottom: 1px solid var(--glass-border-subtle); padding-bottom: 40px; display: flex; justify-content: space-between; align-items: flex-end; }
    .user-welcome { display: flex; align-items: center; gap: 32px; }
    
    .avatar-glow-ring {
      position: relative; width: 90px; height: 90px;
      padding: 4px; border-radius: 50%; background: var(--grad-main);
      box-shadow: var(--shadow-glow);
    }
    .avatar-main {
      width: 100%; height: 100%; border-radius: 50%; background: var(--bg-deep);
      display: flex; align-items: center; justify-content: center;
      font-size: 2rem; font-weight: 800; color: var(--text-primary); border: 4px solid var(--bg-deep);
    }

    .premium-stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(280px, 1fr)); gap: 24px; }
    .stat-card-premium {
      background: var(--bg-card); border: 1px solid var(--glass-border-subtle);
      border-radius: var(--radius-lg); padding: 28px; display: flex; align-items: center; gap: 24px;
      position: relative; overflow: hidden;
      box-shadow: 0 10px 30px rgba(0,0,0,0.3);
    }
    .stat-card-premium::before {
      content: ''; position: absolute; inset: 0; background: var(--grad-surface); pointer-events: none;
    }
    .stat-icon-bg {
      width: 56px; height: 56px; border-radius: 16px; display: flex; align-items: center; justify-content: center;
      position: relative; z-index: 2;
    }
    .stat-icon-bg.amber { background: rgba(245, 158, 11, 0.1); color: var(--primary); }
    .stat-icon-bg.red { background: rgba(239, 68, 68, 0.1); color: #ef4444; }
    .stat-icon-bg.gray { background: rgba(255, 255, 255, 0.05); color: var(--text-secondary); }
    .stat-icon-bg mat-icon { font-size: 28px; width: 28px; height: 28px; }

    .stat-meta { position: relative; z-index: 2; }
    .stat-meta .value { display: block; font-size: 2.2rem; font-weight: 800; font-family: 'Playfair Display', serif; line-height: 1.1; margin-bottom: 4px; }
    .stat-meta .label { font-size: 0.8rem; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.05em; }
    .stat-trend { position: absolute; top: 20px; right: 24px; font-size: 0.75rem; font-weight: 800; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.05em; z-index: 2; }
    .stat-trend.positive { color: #22c55e; }

    .reader-banner {
      display: flex; justify-content: space-between; align-items: center;
      padding: 32px 40px; background: linear-gradient(90deg, rgba(245,158,11,0.05), transparent);
      border-left: 4px solid var(--primary);
    }
    .reader-banner h2 { font-family: 'Inter', sans-serif; font-size: 1.2rem; margin: 0 0 8px; color: var(--text-primary); }
    .reader-banner p { margin: 0; color: var(--text-secondary); font-size: 0.95rem; }

    .dashboard-layout { display: grid; grid-template-columns: 1fr 380px; gap: 40px; align-items: start; }
    .dashboard-side { margin-top: 80px; }
    .quick-grid { display: flex; flex-direction: column; gap: 16px; }
    
    .action-tile {
      display: flex; align-items: center; gap: 24px; padding: 24px; cursor: pointer;
    }
    .action-tile:hover .tile-icon { background: var(--primary); color: #000; }
    .action-tile:hover .arrow { transform: translateX(4px); color: var(--primary); opacity: 1; }
    .tile-icon { width: 48px; height: 48px; background: rgba(255,255,255,0.05); border-radius: 12px; display: flex; align-items: center; justify-content: center; color: var(--text-secondary); transition: all 0.3s; }
    .tile-body h3 { margin: 0; font-size: 1.05rem; font-weight: 700; font-family: 'Inter', sans-serif; }
    .tile-body p { margin: 4px 0 0; color: var(--text-secondary); font-size: 0.88rem; }
    .action-tile .arrow { margin-left: auto; color: var(--text-muted); opacity: 0.5; transition: all 0.3s; }

    .admin-tile { border: 1px dashed rgba(239,68,68,0.3); background: rgba(239,68,68,0.02); }
    .admin-tile .tile-icon { color: #ef4444; }

    .identity-stack { display: flex; flex-direction: column; gap: 20px; }
    .id-item { display: flex; justify-content: space-between; align-items: center; }
    .id-label { font-size: 0.8rem; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.05em; }

    @media (max-width: 1100px) {
      .dashboard-layout { grid-template-columns: 1fr; }
      .dashboard-hero { flex-direction: column; align-items: flex-start; gap: 24px; }
      .reader-banner { flex-direction: column; text-align: center; gap: 24px; }
    }
  `]
})
export class DashboardComponent implements OnInit {
  user: User | null = null;
  stats: AnalyticsResponse = {
    totalStories: 0,
    totalEngagement: 0,
    storageUsagePercentage: 0,
    trendingStories: [],
    sixthOccurrencePosition: 0
  };

  constructor(
    private authService: AuthService,
    private postService: PostService,
    private toastr: ToastrService
  ) {}

  get canCreatePost(): boolean {
    return this.user?.role === 'Author' || this.user?.role === 'Admin';
  }

  get isAdmin(): boolean {
    return this.user?.role === 'Admin';
  }

  get isReader(): boolean {
    return this.user?.role === 'Reader';
  }

  get canViewAnalytics(): boolean {
    return this.canCreatePost; // Only authors and admins see analytics
  }

  ngOnInit(): void {
    this.user = this.authService.getCurrentUser();
    if (this.canViewAnalytics) {
      this.loadStats();
    }
  }

  loadStats(): void {
    this.postService.getAnalytics().pipe(
      catchError(() => of({ success: false, data: null, message: '' }))
    ).subscribe({
      next: (res: any) => {
        if (res.success && res.data) {
          this.stats = res.data;
        }
      }
    });
  }

  getUserDisplayName(): string {
    if (this.user?.fullName && this.user.fullName.length > 0) return this.user.fullName;
    if (this.user?.username && this.user.username.length > 0) return this.user.username;
    return this.user?.email?.split('@')[0] || 'User';
  }

  requestUpgrade(role: string): void {
    this.authService.requestUpgrade(role).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.success(`Upgrade request to ${role} sent successfully.`);
        } else {
          this.toastr.error(res.message || 'Upgrade request failed.');
        }
      },
      error: (err) => {
        this.toastr.error(err.error?.message || 'Could not send upgrade request.');
      }
    });
  }
}
