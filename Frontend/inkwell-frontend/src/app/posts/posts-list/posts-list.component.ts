import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/services/auth.service';
import { BaseResponse, Post } from '../../core/models/models';
import { PostService } from '../../core/services/post.service';

@Component({
  selector: 'app-posts-list',
  template: `
    <div class="app-container py-60">
      <header class="section-header animate-in">
        <div class="flex-between flex-wrap gap-24">
          <div>
            <span class="section-kicker">Fresh from the community</span>
            <h1 class="heading-xl">The <span class="text-gradient">InkWell</span> Journal</h1>
            <p class="text-secondary" style="font-size: 1.1rem; max-width: 600px;">
              Read the latest posts, save your favorites, and publish your own work when you're ready.
            </p>
          </div>

          <div class="header-cta">
            <button *ngIf="isLoggedIn && canCreatePost" class="btn-premium" routerLink="/posts/create">
              <mat-icon>edit_square</mat-icon>
              Write a Post
            </button>
            <button *ngIf="!isLoggedIn" class="btn-premium outline" routerLink="/auth/login">
              <mat-icon>login</mat-icon>
              Sign In to Publish
            </button>
          </div>
        </div>
      </header>

      <div *ngIf="loading" class="post-grid mt-40">
        <div class="skeleton-card glass-card" *ngFor="let i of [1,2,3,4,5,6]" style="min-height: 440px;">
          <div class="skeleton-shimmer"></div>
          <div class="skel-block" style="height: 220px; border-radius: 0;"></div>
          <div style="padding: 26px;">
            <div class="skel-block" style="height: 14px; width: 40%; margin-bottom: 16px;"></div>
            <div class="skel-block" style="height: 24px; margin-bottom: 12px;"></div>
            <div class="skel-block" style="height: 24px; width: 80%; margin-bottom: 20px;"></div>
            <div class="skel-block" style="height: 14px;"></div>
            <div class="skel-block" style="height: 14px; margin-top: 8px; width: 60%;"></div>
          </div>
        </div>
      </div>

      <div *ngIf="!loading && errorMessage" class="glass-card state-card mt-40 animate-in">
        <mat-icon>error_outline</mat-icon>
        <h2>We couldn't load posts right now.</h2>
        <p>{{ errorMessage }}</p>
        <button class="btn-premium outline mt-24" (click)="loadPosts()">Try Again</button>
      </div>

      <div class="post-grid mt-40" *ngIf="!loading && !errorMessage && posts.length > 0">
        <article
          class="glass-card post-card animate-in"
          *ngFor="let post of posts; let i = index"
          [style.animation-delay]="(i * 0.08) + 's'"
          [routerLink]="['/posts', post.postId]">
          
          <div class="post-banner">
            <img
              [src]="post.imageUrl || defaultImage"
              (error)="handleImageError($event)"
              class="banner-img"
              [alt]="post.title">
            <div class="post-overlay"></div>
            <span class="badge-amber post-tag">{{ post.status || 'Published' }}</span>
          </div>

          <div class="post-body">
            <div class="post-meta">
              <span class="meta-item">
                <mat-icon>event</mat-icon>
                {{ post.createdAt | date:'mediumDate' }}
              </span>
              <span class="meta-dot">•</span>
              <span class="meta-item">
                <mat-icon>schedule</mat-icon>
                {{ estimateReadTime(post.content) }} min read
              </span>
            </div>

            <h2 class="post-title">{{ post.title }}</h2>
            <p class="post-excerpt">{{ getExcerpt(post.content) }}</p>

            <div class="post-footer">
              <div class="author-pill">
                <div class="avatar-sm">{{ getInitial(post.authorName || post.title) }}</div>
                <span>{{ post.authorName || 'Anonymous' }}</span>
              </div>

              <div class="flex gap-8">
                <button class="icon-btn-premium sm" (click)="$event.stopPropagation(); toggleSave(post)" title="Save for later">
                  <mat-icon>bookmark_border</mat-icon>
                </button>
                <button class="like-btn" (click)="$event.stopPropagation(); toggleLike(post)">
                  <mat-icon>favorite_border</mat-icon>
                  <span>{{ post.likesCount || 0 }}</span>
                </button>
              </div>
            </div>
          </div>
        </article>
      </div>

      <div *ngIf="!loading && !errorMessage && posts.length === 0" class="glass-card state-card mt-40 animate-in">
        <mat-icon>auto_stories</mat-icon>
        <h2>No posts yet.</h2>
        <p>Once someone publishes a post, it will show up here immediately.</p>
        <button
          class="btn-premium outline mt-24"
          [routerLink]="isLoggedIn ? '/posts/create' : '/auth/login'">
          {{ isLoggedIn ? 'Create the First Post' : 'Sign In to Get Started' }}
        </button>
      </div>
    </div>
  `
})
export class PostsListComponent implements OnInit {
  readonly defaultImage = 'https://images.unsplash.com/photo-1499750310107-5fef28a66643?auto=format&fit=crop&q=80&w=800';

  posts: Post[] = [];
  loading = false;
  errorMessage = '';
  isLoggedIn = false;
  userRole = '';

  constructor(
    private postService: PostService,
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {}

  get canCreatePost(): boolean {
    return this.userRole === 'Author' || this.userRole === 'Admin';
  }

  ngOnInit(): void {
    this.isLoggedIn = this.authService.isLoggedIn();
    this.userRole = this.authService.getUserRole();
    this.loadPosts();
  }

  loadPosts(): void {
    this.loading = true;
    this.errorMessage = '';

    this.postService.getAllPosts().subscribe({
      next: (response: BaseResponse<Post[]>) => {
        this.posts = response.data || [];
        this.loading = false;
      },
      error: (error) => {
        this.loading = false;
        this.errorMessage = error.error?.message || 'The backend is unavailable or returned an invalid response.';
      }
    });
  }

  toggleLike(post: Post): void {
    if (!this.isLoggedIn) {
      this.toastr.info('Sign in to like posts.', 'Authentication Required');
      this.router.navigate(['/auth/login']);
      return;
    }

    this.postService.toggleLike(post.postId).subscribe({
      next: (response: BaseResponse<number>) => {
        post.likesCount = response.data || 0;
        this.toastr.success('Your reaction has been saved.', 'Post Updated');
      },
      error: (error) => {
        this.toastr.error(error.error?.message || 'Unable to update likes right now.', 'Request Failed');
      }
    });
  }

  toggleSave(post: Post): void {
    if (!this.isLoggedIn) {
      this.toastr.info('Sign in to save posts.', 'Authentication Required');
      this.router.navigate(['/auth/login']);
      return;
    }

    this.postService.toggleSave(post.postId).subscribe({
      next: (response: BaseResponse<boolean>) => {
        const isSaved = response.data;
        this.toastr.success(isSaved ? 'Saved to reading list.' : 'Removed from reading list.', 'List Updated');
      },
      error: (error) => {
        this.toastr.error(error.error?.message || 'Unable to save post right now.', 'Request Failed');
      }
    });
  }

  getExcerpt(content: string): string {
    const plainText = content.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    if (!plainText) {
      return 'Open this post to read the full story.';
    }

    return plainText.length > 150 ? `${plainText.slice(0, 147)}...` : plainText;
  }

  estimateReadTime(content: string): number {
    const plainText = content.replace(/<[^>]+>/g, ' ').trim();
    const words = plainText ? plainText.split(/\s+/).length : 0;
    return Math.max(1, Math.ceil(words / 200));
  }

  getInitial(value: string): string {
    return (value || 'I').trim().charAt(0).toUpperCase() || 'I';
  }

  handleImageError(event: Event): void {
    const image = event.target as HTMLImageElement;
    image.src = 'https://images.unsplash.com/photo-1499750310107-5fef28a66643?auto=format&fit=crop&q=80&w=800';
  }
}
