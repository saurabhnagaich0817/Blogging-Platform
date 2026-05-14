import { Component, OnInit } from '@angular/core';
import { PostService } from '../../core/services/post.service';
import { CategoryService } from '../../core/services/category.service';
import { Post, Tag, BaseResponse, Category } from '../../core/models/models';
import { Router } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-explore',
  template: `
    <div class="app-container py-60 animate-in">
      <div class="text-center mb-40">
        <span class="badge-amber mb-16" style="display:inline-flex">Discover</span>
        <h1 class="heading-xl">Explore <span class="text-gradient">Stories.</span></h1>
        <p class="text-secondary subtitle-xl">Trending posts, fresh perspectives, and hidden gems from the community.</p>
      </div>

      <!-- Search Bar -->
      <div class="search-container mb-32 animate-in" style="animation-delay: 0.05s">
        <div class="premium-search-box">
          <mat-icon>search</mat-icon>
          <input 
            type="text" 
            [(ngModel)]="searchTerm" 
            placeholder="Search stories, authors, or topics..."
            (input)="onSearchChange()">
          <button *ngIf="searchTerm" class="clear-search" (click)="searchTerm = ''; onSearchChange()">
            <mat-icon>close</mat-icon>
          </button>
        </div>
      </div>

      <!-- Trending Tags -->
      <div class="tags-strip mb-40 animate-in" style="animation-delay: 0.1s" *ngIf="trendingTags.length > 0">
        <span class="tags-label">Trending:</span>
        <button
          *ngFor="let tag of trendingTags"
          class="tag-pill"
          [class.active]="selectedTag === tag.name"
          (click)="filterByTag(tag)">
          # {{ tag.name }}
          <span class="tag-count">{{ tag.postCount }}</span>
        </button>
        <button *ngIf="selectedTag" class="tag-pill clear" (click)="clearFilter()">
          <mat-icon>close</mat-icon> Clear
        </button>
      </div>

      <!-- Categories Filter -->
      <div class="tags-strip mb-40 animate-in" style="animation-delay: 0.15s" *ngIf="categories.length > 0">
        <span class="tags-label">Categories:</span>
        <button
          *ngFor="let cat of categories"
          class="tag-pill"
          [class.active]="selectedCategory === cat.categoryId"
          (click)="filterByCategory(cat)">
          <mat-icon>folder</mat-icon> {{ cat.name }}
        </button>
        <button *ngIf="selectedCategory" class="tag-pill clear" (click)="selectedCategory = ''">
          <mat-icon>close</mat-icon> Clear
        </button>
      </div>

      <!-- Loading Skeleton -->
      <div class="post-grid" *ngIf="loading">
        <div class="glass-card skeleton-card" *ngFor="let i of [1,2,3,4,5,6]" style="min-height: 440px;">
          <div class="skeleton-shimmer"></div>
        </div>
      </div>

      <!-- Error -->
      <div class="glass-card state-card mt-40 animate-in" *ngIf="!loading && errorMessage">
        <mat-icon>error_outline</mat-icon>
        <h2>Couldn't load stories</h2>
        <p>{{ errorMessage }}</p>
        <button class="btn-premium outline mt-24" (click)="loadPosts()">Try Again</button>
      </div>

      <!-- Posts Grid -->
      <div class="post-grid" *ngIf="!loading && !errorMessage && filteredPosts.length > 0">
        <article
          class="glass-card post-card animate-in"
          *ngFor="let post of filteredPosts; let i = index"
          [style.animation-delay]="(i * 0.08) + 's'"
          [routerLink]="['/posts', post.postId]">

          <div class="post-banner">
            <img [src]="post.imageUrl || defaultImage" (error)="handleImageError($event)" class="banner-img" [alt]="post.title">
            <div class="post-overlay"></div>
            <span class="badge-amber post-tag">{{ post.status || 'Published' }}</span>
            <span *ngIf="post.categoryName" class="badge-blue post-tag category-badge">{{ post.categoryName }}</span>
          </div>

          <div class="post-body">
            <div class="post-meta">
              <span class="meta-item"><mat-icon>event</mat-icon> {{ post.createdAt | date:'mediumDate' }}</span>
              <span class="meta-dot">•</span>
              <span class="meta-item"><mat-icon>schedule</mat-icon> {{ estimateReadTime(post.content) }} min read</span>
            </div>

            <h2 class="post-title">{{ post.title }}</h2>
            <p class="post-excerpt">{{ getExcerpt(post.content) }}</p>

            <div class="post-footer">
              <div class="author-pill">
                <div class="avatar-sm">{{ getInitial(post.authorName || 'U') }}</div>
                <span>{{ post.authorName || ('Author ' + (post.authorId | slice:0:6)) }}</span>
              </div>

              <div class="flex gap-8">
                <button class="like-btn" (click)="$event.stopPropagation(); toggleLike(post)">
                  <mat-icon>favorite_border</mat-icon>
                  <span>{{ post.likesCount || 0 }}</span>
                </button>
              </div>
            </div>
          </div>
        </article>
      </div>

      <!-- Empty State -->
      <div class="glass-card state-card mt-40 animate-in" *ngIf="!loading && !errorMessage && filteredPosts.length === 0">
        <mat-icon>explore_off</mat-icon>
        <h2>No stories found{{ selectedTag ? ' for #' + selectedTag : '' }}</h2>
        <p>{{ selectedTag ? 'Try a different tag or clear the filter.' : 'Be the first one to share a story!' }}</p>
        <div class="flex-center gap-16 mt-24">
          <button class="btn-premium outline" *ngIf="selectedTag" (click)="clearFilter()">Clear Filter</button>
          <button class="btn-premium" routerLink="/posts/create" *ngIf="isLoggedIn && canCreatePost">Write a Story</button>
          <button class="btn-premium outline" routerLink="/auth/login" *ngIf="!isLoggedIn">Sign In</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .mb-40 { margin-bottom: 40px; }
    .subtitle-xl { max-width: 620px; margin: 12px auto 0; font-size: 1.1rem; }

    .tags-strip {
      display: flex; align-items: center; flex-wrap: wrap; gap: 12px;
      padding: 24px; background: rgba(255,255,255,0.02);
      border: 1px solid var(--glass-border-subtle); border-radius: var(--radius-md);
    }
    .tags-label { font-size: 0.8rem; font-weight: 800; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.06em; }
    .tag-pill {
      display: inline-flex; align-items: center; gap: 6px;
      padding: 6px 14px; border-radius: 999px;
      background: rgba(255,255,255,0.04); border: 1px solid var(--glass-border-subtle);
      color: var(--text-secondary); font-size: 0.85rem; font-weight: 700; cursor: pointer;
      transition: all 0.2s ease;
    }
    .tag-pill:hover, .tag-pill.active { background: rgba(245,158,11,0.1); border-color: var(--primary); color: var(--primary); }
    .tag-pill.clear { color: #ef4444; border-color: transparent; }
    .tag-pill.clear:hover { background: rgba(239,68,68,0.1); }
    .tag-count { font-size: 0.72rem; background: rgba(0,0,0,0.2); padding: 1px 6px; border-radius: 999px; }
    .tag-pill mat-icon { font-size: 16px; width: 16px; height: 16px; }

    .premium-search-box {
      position: relative; max-width: 600px; margin: 0 auto;
      background: rgba(255,255,255,0.05); border: 1px solid var(--glass-border);
      border-radius: var(--radius-lg); padding: 8px 16px;
      display: flex; align-items: center; gap: 12px;
      transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
      box-shadow: 0 10px 30px rgba(0,0,0,0.1);
    }
    .premium-search-box:focus-within {
      background: rgba(255,255,255,0.08); border-color: var(--primary);
      box-shadow: 0 15px 40px rgba(245,158,11,0.15); transform: translateY(-2px);
    }
    .premium-search-box mat-icon { color: var(--text-muted); }
    .premium-search-box input {
      background: none; border: none; color: var(--text-primary);
      font-size: 1.1rem; width: 100%; outline: none;
    }
    .badge-blue { background: rgba(59,130,246,0.15); color: #93c5fd; border: 1px solid rgba(59,130,246,0.3); padding: 4px 10px; border-radius: 6px; font-size: 0.7rem; font-weight: 800; text-transform: uppercase; }
    .category-badge { position: absolute; top: 12px; left: 12px; }
    .clear-search {
      background: none; border: none; color: var(--text-muted);
      cursor: pointer; padding: 4px; border-radius: 50%;
      display: flex; align-items: center; justify-content: center;
      transition: all 0.2s;
    }
    .clear-search:hover { background: rgba(255,255,255,0.1); color: #ef4444; }
    .mb-32 { margin-bottom: 32px; }
  
  `]
})
export class ExploreComponent implements OnInit {
  readonly defaultImage = 'https://images.unsplash.com/photo-1499750310107-5fef28a66643?auto=format&fit=crop&q=80&w=800';

  posts: Post[] = [];
  trendingTags: Tag[] = [];
  categories: Category[] = [];
  selectedTag = '';
  selectedCategory = '';
  searchTerm = '';
  loading = false;
  errorMessage = '';
  isLoggedIn = false;
  userRole = '';

  constructor(
    private postService: PostService,
    private categoryService: CategoryService,
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
    this.loadTrendingTags();
    this.loadCategories();
  }

  loadCategories(): void {
    this.categoryService.getAllCategories().subscribe({
      next: (res) => { this.categories = res.data || []; },
      error: () => { this.categories = []; }
    });
  }

  loadPosts(): void {
    this.loading = true;
    this.errorMessage = '';

    this.postService.getAllPosts().subscribe({
      next: (res: BaseResponse<Post[]>) => {
        this.loading = false;
        this.posts = res.data || [];
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err.error?.message || 'The backend is unavailable. Please try again.';
      }
    });
  }

  loadTrendingTags(): void {
    this.categoryService.getTrendingTags(8).subscribe({
      next: (res: BaseResponse<Tag[]>) => { this.trendingTags = res.data || []; },
      error: () => { this.trendingTags = []; }
    });
  }

  filterByTag(tag: Tag): void {
    this.selectedTag = tag.name;
  }

  clearFilter(): void {
    this.selectedTag = '';
    this.selectedCategory = '';
  }

  filterByCategory(cat: Category): void {
    this.selectedCategory = cat.categoryId;
  }

  onSearchChange(): void {
    // Client-side filtering is fast enough for now
  }

  get filteredPosts(): Post[] {
    let results = this.posts;

    // Filter by Tag
    if (this.selectedTag) {
      results = results.filter(p =>
        p.title.toLowerCase().includes(this.selectedTag.toLowerCase()) ||
        p.content.toLowerCase().includes(this.selectedTag.toLowerCase())
      );
    }

    // Filter by Category
    if (this.selectedCategory) {
      results = results.filter(p => p.categoryId === this.selectedCategory);
    }

    // Filter by Search Term
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      results = results.filter(p =>
        p.title.toLowerCase().includes(term) ||
        p.content.toLowerCase().includes(term) ||
        p.authorName?.toLowerCase().includes(term) ||
        p.categoryName?.toLowerCase().includes(term)
      );
    }

    return results;
  }

  toggleLike(post: Post): void {
    if (!this.isLoggedIn) {
      this.toastr.info('Sign in to like posts.');
      this.router.navigate(['/auth/login']);
      return;
    }

    this.postService.toggleLike(post.postId).subscribe({
      next: (res: BaseResponse<number>) => { post.likesCount = res.data || 0; },
      error: () => { this.toastr.error('Could not update like.'); }
    });
  }

  getExcerpt(content: string): string {
    const plain = content.replace(/<[^>]+>/g, ' ').replace(/\s+/g, ' ').trim();
    return plain.length > 150 ? `${plain.slice(0, 147)}...` : (plain || 'Open this post to read the full story.');
  }

  estimateReadTime(content: string): number {
    const plain = content.replace(/<[^>]+>/g, ' ').trim();
    return Math.max(1, Math.ceil((plain ? plain.split(/\s+/).length : 0) / 200));
  }

  getInitial(value: string): string {
    return (value || 'U').trim().charAt(0).toUpperCase();
  }

  handleImageError(event: Event): void {
    (event.target as HTMLImageElement).src = this.defaultImage;
  }
}
