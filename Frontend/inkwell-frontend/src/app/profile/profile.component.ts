import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../core/services/auth.service';
import { PostService } from '../core/services/post.service';
import { User, Post, BaseResponse, MediaFile } from '../core/models/models';
import { MediaNewsletterService } from '../core/services/media-newsletter.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-profile',
  template: `
    <div *ngIf="loading" class="flex-center py-80">
      <mat-spinner diameter="40"></mat-spinner>
    </div>

    <div *ngIf="!loading && !profileUser" class="app-container py-60">
      <div class="glass-card state-card animate-in">
        <mat-icon>person_off</mat-icon>
        <h2>User not found</h2>
        <p>This profile doesn't exist or was removed.</p>
        <button class="btn-premium outline mt-24" routerLink="/explore">Explore Platform</button>
      </div>
    </div>

    <div *ngIf="!loading && profileUser" class="app-container py-60">
      <div class="profile-header glass-card animate-in">
        <div class="profile-bg"></div>
        <div class="profile-content">
          <div class="avatar-lg-wrap">
            <img *ngIf="profileUser.profilePictureUrl" [src]="profileUser.profilePictureUrl" class="avatar-lg-img" [alt]="profileUser.fullName">
            <div *ngIf="!profileUser.profilePictureUrl" class="avatar-lg">{{ getInitial(profileUser.fullName || profileUser.username) }}</div>
            <button class="edit-avatar-btn" *ngIf="isOwnProfile" (click)="fileInput.click()" [disabled]="updating">
              <mat-icon>{{ updating ? 'sync' : 'photo_camera' }}</mat-icon>
            </button>
            <input #fileInput type="file" (change)="onFileSelected($event)" accept="image/*" hidden>
          </div>
          
          <div class="profile-info">
            <div class="flex items-center gap-12 mb-8">
              <h1 class="heading-lg m-0" *ngIf="!editMode">{{ profileUser.fullName || profileUser.username }}</h1>
              <input *ngIf="editMode" [(ngModel)]="profileUser.fullName" class="premium-input-sm" placeholder="Full Name">
              <span class="badge-amber" *ngIf="profileUser.role === 'Admin'">Admin</span>
              <span class="badge-blue" *ngIf="profileUser.role === 'Author'">Author</span>
            </div>
            <p class="text-secondary" style="font-size: 1.1rem">&#64;{{ profileUser.username }}</p>
            <p class="text-muted mt-8" *ngIf="!editMode">{{ profileUser.bio || 'Writer at InkWell' }}</p>
            <textarea *ngIf="editMode" [(ngModel)]="profileUser.bio" class="premium-textarea-sm mt-8" placeholder="Tell us about yourself..."></textarea>
            
            <div class="social-links mt-16" *ngIf="!editMode">
              <a *ngIf="profileUser.linkedInUrl" [href]="profileUser.linkedInUrl" target="_blank" class="social-icon"><mat-icon>link</mat-icon> LinkedIn</a>
              <a *ngIf="profileUser.githubUrl" [href]="profileUser.githubUrl" target="_blank" class="social-icon"><mat-icon>code</mat-icon> GitHub</a>
              <span *ngIf="profileUser.phoneNumber" class="social-icon"><mat-icon>phone</mat-icon> {{ profileUser.phoneNumber }}</span>
            </div>

            <div class="edit-social-grid mt-16" *ngIf="editMode">
              <div class="input-mini">
                <mat-icon>link</mat-icon>
                <input [(ngModel)]="profileUser.linkedInUrl" placeholder="LinkedIn URL">
              </div>
              <div class="input-mini">
                <mat-icon>code</mat-icon>
                <input [(ngModel)]="profileUser.githubUrl" placeholder="GitHub URL">
              </div>
              <div class="input-mini">
                <mat-icon>phone</mat-icon>
                <input [(ngModel)]="profileUser.phoneNumber" placeholder="Phone Number">
              </div>
            </div>

            <div class="profile-actions mt-24">
              <button class="btn-premium sm" *ngIf="isOwnProfile && !editMode" (click)="editMode = true">
                <mat-icon>edit</mat-icon> Edit Profile
              </button>
              <div class="flex gap-12" *ngIf="isOwnProfile && editMode">
                <button class="btn-premium sm" (click)="saveProfile()" [disabled]="updating">
                  <mat-icon>{{ updating ? 'sync' : 'save' }}</mat-icon> Save
                </button>
                <button class="btn-premium sm outline" (click)="editMode = false">Cancel</button>
              </div>
              
              <div class="flex gap-12" *ngIf="!isOwnProfile">
                <button class="btn-premium sm outline" *ngIf="connectionStatus === 'None'" (click)="sendConnectionRequest()">
                  <mat-icon>person_add</mat-icon> Connect
                </button>
                <button class="btn-premium sm outline" *ngIf="connectionStatus === 'Requested'" disabled>
                  <mat-icon>schedule</mat-icon> Requested
                </button>
                <button class="btn-premium sm" *ngIf="connectionStatus === 'Connected'" disabled>
                  <mat-icon>check_circle</mat-icon> Connected
                </button>
                <button class="btn-premium sm" *ngIf="connectionStatus === 'Inbound'" (click)="acceptConnection()">
                  <mat-icon>handshake</mat-icon> Accept Connection
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Pending Connection Requests (Only on Own Profile) -->
      <div class="mt-40 animate-in" style="animation-delay: 0.05s" *ngIf="isOwnProfile && pendingRequests.length > 0">
        <div class="flex-between mb-20">
          <h2 class="heading-md m-0">Connection Requests <span class="badge-amber">{{ pendingRequests.length }}</span></h2>
        </div>
        <div class="glass-card p-24">
          <div class="requests-list">
            <div class="request-item" *ngFor="let req of pendingRequests">
              <div class="flex items-center gap-16">
                <div class="avatar-sm">{{ getInitial(req.fullName || req.username) }}</div>
                <div>
                  <h4 class="m-0 font-bold">{{ req.fullName || req.username }}</h4>
                  <p class="text-muted text-sm m-0">Sent you a connection request</p>
                </div>
              </div>
              <div class="flex gap-12">
                <button class="btn-premium sm" (click)="acceptPendingRequest(req)">Accept</button>
                <button class="btn-premium sm outline danger" (click)="rejectPendingRequest(req)">Ignore</button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="mt-40 animate-in" style="animation-delay: 0.1s" *ngIf="profileUser.role !== 'Reader'">
        <h2 class="heading-md mb-24">Published Stories <span class="text-secondary">({{ posts.length }})</span></h2>
        
        <div class="post-grid" *ngIf="posts.length > 0">
          <article class="glass-card post-card" *ngFor="let post of posts" [routerLink]="['/posts', post.postId]">
            <div class="post-banner">
              <img [src]="post.imageUrl || defaultImage" class="banner-img" [alt]="post.title">
              <div class="post-overlay"></div>
            </div>
            <div class="post-body">
              <div class="post-meta">
                <span class="meta-item"><mat-icon>event</mat-icon>{{ post.createdAt | date:'mediumDate' }}</span>
              </div>
              <h2 class="post-title">{{ post.title }}</h2>
              <div class="post-footer">
                <span class="text-secondary text-sm"><mat-icon style="font-size:14px; width:14px; height:14px; vertical-align:-2px">favorite</mat-icon> {{ post.likesCount || 0 }} likes</span>
              </div>
            </div>
          </article>
        </div>

        <div *ngIf="posts.length === 0" class="glass-card state-card compact mt-24">
          <mat-icon>edit_note</mat-icon>
          <h3>No stories yet</h3>
          <p>{{ profileUser.fullName || profileUser.username }} hasn't published anything yet.</p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .profile-header { position: relative; overflow: hidden; padding-bottom: 40px; border-radius: var(--radius-xl); }
    .profile-bg { height: 160px; background: linear-gradient(135deg, rgba(245,158,11,0.2), rgba(239,68,68,0.1)); border-bottom: 1px solid var(--glass-border-subtle); }
    .profile-content { padding: 0 40px; display: flex; gap: 32px; align-items: flex-end; margin-top: -60px; position: relative; z-index: 2; }
    
    .avatar-lg-wrap { position: relative; width: 120px; height: 120px; flex-shrink: 0; }
    .avatar-lg-img { width: 100%; height: 100%; border-radius: 50%; object-fit: cover; border: 4px solid var(--bg-card); box-shadow: 0 10px 30px rgba(0,0,0,0.5); }
    .avatar-lg {
      width: 100%; height: 100%; border-radius: 50%; background: var(--bg-deep); border: 4px solid var(--bg-card);
      display: flex; align-items: center; justify-content: center; font-size: 3rem; font-weight: 800; color: var(--primary);
      box-shadow: 0 10px 30px rgba(0,0,0,0.5);
    }
    .edit-avatar-btn {
      position: absolute; bottom: 0; right: 0; width: 36px; height: 36px; border-radius: 50%;
      background: var(--primary); color: #000; border: 3px solid var(--bg-card);
      display: flex; align-items: center; justify-content: center; cursor: pointer; transition: all 0.2s;
    }
    .edit-avatar-btn:hover { transform: scale(1.1); background: #fbbf24; }
    .edit-avatar-btn mat-icon { font-size: 18px; width: 18px; height: 18px; }
    .profile-info { padding-bottom: 10px; flex: 1; }
    .m-0 { margin: 0; }

    .social-links { display: flex; gap: 16px; flex-wrap: wrap; }
    .social-icon { display: flex; align-items: center; gap: 6px; font-size: 0.9rem; color: var(--text-secondary); text-decoration: none; transition: color 0.2s; }
    .social-icon:hover { color: var(--primary); }
    .social-icon mat-icon { font-size: 18px; width: 18px; height: 18px; }

    .premium-input-sm, .premium-textarea-sm {
      width: 100%; background: rgba(255,255,255,0.05); border: 1px solid var(--glass-border);
      border-radius: 8px; color: #fff; padding: 8px 12px; font-size: 1rem;
    }
    .premium-textarea-sm { min-height: 80px; resize: vertical; }

    .edit-social-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 12px; }
    .input-mini {
      display: flex; align-items: center; gap: 8px; background: rgba(0,0,0,0.2);
      border-radius: 8px; padding: 4px 12px; border: 1px solid var(--glass-border-subtle);
    }
    .input-mini mat-icon { font-size: 16px; width: 16px; height: 16px; color: var(--text-muted); }
    .input-mini input { background: transparent; border: none; color: #fff; font-size: 0.85rem; width: 100%; outline: none; }

    @media (max-width: 768px) {
      .profile-content { flex-direction: column; align-items: center; text-align: center; }
      .profile-info { padding-bottom: 0; margin-top: 16px; width: 100%; }
      .profile-info .flex { justify-content: center; flex-wrap: wrap; }
      .social-links { justify-content: center; }
      .edit-social-grid { grid-template-columns: 1fr; }
    }
    
    .requests-list { display: grid; gap: 16px; }
    .request-item { display: flex; align-items: center; justify-content: space-between; padding: 16px; background: rgba(255,255,255,0.03); border-radius: 12px; border: 1px solid var(--glass-border-subtle); }
    .btn-premium.outline.danger:hover { background: rgba(239,68,68,0.2); border-color: #ef4444; color: #ef4444; }
  `]
})
export class ProfileComponent implements OnInit {
  readonly defaultImage = 'https://images.unsplash.com/photo-1499750310107-5fef28a66643?auto=format&fit=crop&q=80&w=800';
  profileUser: any = null; // Changed from User to any to handle extra fields without TS strictness for now
  posts: Post[] = [];
  loading = true;
  updating = false;
  isOwnProfile = false;
  editMode = false;
  pendingRequests: User[] = [];

  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private postService: PostService,
    private mediaService: MediaNewsletterService,
    private toastr: ToastrService
  ) {}

  // Added MediaNewsletterService as any because it's already in the module usually, but need to check
  // Actually I'll just import it properly.

  ngOnInit(): void {
    const username = this.route.snapshot.paramMap.get('username');
    if (!username) {
      this.loading = false;
      return;
    }

    this.authService.getProfile(username).subscribe({
      next: (res: any) => {
        this.profileUser = res.data;
        const currentUser = this.authService.getCurrentUser();
        this.isOwnProfile = !!(currentUser && this.profileUser && currentUser.id === this.profileUser.id);
        
        if (this.isOwnProfile) {
          this.loadPendingRequests();
        }
        
        if (currentUser && this.profileUser && !this.isOwnProfile) {
          this.checkConnectionStatus();
        }

        if (this.profileUser && this.profileUser.role !== 'Reader') {
          this.loadUserPosts(this.profileUser.id);
        } else {
          this.loading = false;
        }
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  connectionStatus: string = 'None';

  checkConnectionStatus(): void {
    const targetId = this.profileUser.userId || this.profileUser.id;
    this.authService.getConnectionStatus(targetId).subscribe({
      next: (res: any) => this.connectionStatus = res.data || 'None'
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.updating = true;
    this.mediaService.uploadMedia(file).subscribe({
      next: (res: BaseResponse<MediaFile>) => {
        if (res.success && res.data?.url) {
          this.updateProfilePicture(res.data.url);
        } else {
          this.updating = false;
          this.toastr.error('Upload failed');
        }
      },
      error: () => {
        this.updating = false;
        this.toastr.error('Error uploading file');
      }
    });
  }

  private updateProfilePicture(url: string): void {
    this.authService.updateProfilePicture(url).subscribe({
      next: (res) => {
        this.updating = false;
        if (res.success) {
          this.toastr.success('Profile picture updated!');
          if (this.profileUser) {
            this.profileUser.profilePictureUrl = url;
          }
        }
      },
      error: () => {
        this.updating = false;
        this.toastr.error('Error updating profile');
      }
    });
  }

  loadUserPosts(userId: string): void {
    this.postService.getAllPosts().subscribe({
      next: (res: any) => {
        this.posts = (res.data || []).filter((p: any) => p.authorId === userId);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  saveProfile(): void {
    if (!this.profileUser) return;
    this.updating = true;
    this.authService.updateProfile(this.profileUser).subscribe({
      next: (res) => {
        this.updating = false;
        if (res.success) {
          this.toastr.success('Profile updated successfully!');
          this.editMode = false;
        }
      },
      error: () => {
        this.updating = false;
        this.toastr.error('Failed to update profile');
      }
    });
  }

  sendConnectionRequest(): void {
    if (!this.profileUser) return;
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) {
      this.toastr.warning('Please login to connect.');
      return;
    }

    const targetId = this.profileUser.userId || this.profileUser.id;
    this.authService.requestConnection(targetId).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.success(`Connection request sent to ${this.profileUser.fullName || this.profileUser.username}!`);
          this.connectionStatus = 'Requested';
        } else {
          this.toastr.error(res.message);
        }
      },
      error: (err) => this.toastr.error(err.error?.message || 'Connection failed.')
    });
  }

  acceptConnection(): void {
    if (!this.profileUser) return;
    const targetId = this.profileUser.userId || this.profileUser.id;
    this.authService.acceptConnection(targetId).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.success('Connection accepted!');
          this.connectionStatus = 'Connected';
        }
      }
    });
  }

  loadPendingRequests(): void {
    this.authService.getPendingConnections().subscribe({
      next: (res) => { this.pendingRequests = res.data || []; }
    });
  }

  acceptPendingRequest(user: User): void {
    this.authService.acceptConnection(user.id).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.success(`You are now connected with ${user.fullName || user.username}!`);
          this.pendingRequests = this.pendingRequests.filter(r => r.id !== user.id);
        }
      }
    });
  }

  rejectPendingRequest(user: User): void {
    this.authService.rejectConnection(user.id).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastr.info('Connection request ignored.');
          this.pendingRequests = this.pendingRequests.filter(r => r.id !== user.id);
        }
      }
    });
  }

  getInitial(value: string | undefined | null): string {
    return value ? value.charAt(0).toUpperCase() : 'U';
  }
}
