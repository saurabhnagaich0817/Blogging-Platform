import { Component, OnInit } from '@angular/core';
import { PostService } from '../../core/services/post.service';
import { Post, BaseResponse } from '../../core/models/models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-saved-posts',
  template: `
    <div class="app-container py-60">
      <header class="section-header flex-between mb-40 animate-in">
        <div>
          <h1 class="heading-lg">Reading List</h1>
          <p class="text-secondary">Stories you've saved for later.</p>
        </div>
        <button class="btn-premium outline sm" routerLink="/explore">
          <mat-icon>explore</mat-icon> Find More
        </button>
      </header>

      <div *ngIf="loading" class="post-grid mt-40">
        <div class="skeleton-card glass-card" *ngFor="let i of [1,2,3]" style="min-height: 440px;">
          <div class="skeleton-shimmer"></div>
        </div>
      </div>

      <div class="post-grid mt-40" *ngIf="!loading && posts.length > 0">
        <article class="glass-card post-card animate-in" *ngFor="let post of posts; let i = index" [style.animation-delay]="(i * 0.08) + 's'" [routerLink]="['/posts', post.postId]">
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
              <div class="author-pill">
                <div class="avatar-sm">{{ getInitial(post.authorName || 'U') }}</div>
                <span>{{ post.authorName || 'Anonymous' }}</span>
              </div>
              <button class="icon-btn-premium sm" (click)="$event.stopPropagation(); removeSave(post)" title="Remove from list">
                <mat-icon style="color: var(--primary)">bookmark</mat-icon>
              </button>
            </div>
          </div>
        </article>
      </div>

      <div *ngIf="!loading && posts.length === 0" class="glass-card state-card compact mt-40 animate-in">
        <mat-icon>bookmarks</mat-icon>
        <h2>Your reading list is empty.</h2>
        <p>Save stories to read them later when you have time.</p>
        <button class="btn-premium mt-24" routerLink="/explore">Explore Stories</button>
      </div>
    </div>
  `
})
export class SavedPostsComponent implements OnInit {
  readonly defaultImage = 'https://images.unsplash.com/photo-1499750310107-5fef28a66643?auto=format&fit=crop&q=80&w=800';
  posts: Post[] = [];
  loading = true;

  constructor(
    private postService: PostService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.postService.getSavedPosts().subscribe({
      next: (res: BaseResponse<Post[]>) => {
        this.posts = res.data || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load reading list.');
      }
    });
  }

  removeSave(post: Post): void {
    this.postService.toggleSave(post.postId).subscribe({
      next: () => {
        this.posts = this.posts.filter(p => p.postId !== post.postId);
        this.toastr.success('Story removed from reading list.');
      }
    });
  }

  getInitial(value: string): string {
    return value.charAt(0).toUpperCase();
  }
}
