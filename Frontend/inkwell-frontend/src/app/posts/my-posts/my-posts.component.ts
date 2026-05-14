import { Component, OnInit } from '@angular/core';
import { PostService } from '../../core/services/post.service';
import { Post, BaseResponse } from '../../core/models/models';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-my-posts',
  template: `
    <div class="app-container py-60">
      <header class="section-header flex-between mb-40 animate-in">
        <div>
          <h1 class="heading-lg">My Stories</h1>
          <p class="text-secondary">Manage your published drafts and stories.</p>
        </div>
        <button class="btn-premium sm" routerLink="/posts/create">
          <mat-icon>edit_square</mat-icon> New Story
        </button>
      </header>

      <div *ngIf="loading" class="flex-center py-80">
        <mat-spinner diameter="40"></mat-spinner>
      </div>

      <div class="glass-card table-card animate-in" *ngIf="!loading && posts.length > 0">
        <table class="premium-table full-width">
          <thead>
            <tr>
              <th>Title</th>
              <th>Status</th>
              <th>Published</th>
              <th>Likes</th>
              <th class="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let post of posts">
              <td class="td-title">
                <a [routerLink]="['/posts', post.postId]" class="title-link">{{ post.title }}</a>
              </td>
              <td><span class="badge-amber">{{ post.status || 'Published' }}</span></td>
              <td class="text-secondary">{{ post.createdAt | date:'mediumDate' }}</td>
              <td class="text-secondary"><mat-icon class="valign-icon">favorite</mat-icon> {{ post.likesCount || 0 }}</td>
              <td class="text-right">
                <div class="flex gap-8" style="justify-content: flex-end">
                  <button class="icon-btn-premium sm" [routerLink]="['/posts/edit', post.postId]" title="Edit"><mat-icon>edit</mat-icon></button>
                  <button class="icon-btn-premium sm danger" (click)="deletePost(post)" title="Delete"><mat-icon>delete</mat-icon></button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div *ngIf="!loading && posts.length === 0" class="glass-card state-card compact mt-40 animate-in">
        <mat-icon>library_books</mat-icon>
        <h2>You haven't published anything yet.</h2>
        <p>Start your journey by writing your first story.</p>
        <button class="btn-premium outline mt-24" routerLink="/posts/create">Write a Story</button>
      </div>
    </div>
  `,
  styles: [`
    .table-card { padding: 0; overflow: hidden; }
    .premium-table { border-collapse: collapse; text-align: left; }
    .premium-table th { padding: 20px 24px; background: rgba(0,0,0,0.2); font-size: 0.8rem; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.05em; border-bottom: 1px solid var(--glass-border-subtle); }
    .premium-table td { padding: 20px 24px; border-bottom: 1px solid var(--glass-border-subtle); vertical-align: middle; }
    .premium-table tr:last-child td { border-bottom: none; }
    .premium-table tbody tr:hover { background: rgba(255,255,255,0.02); }
    .title-link { color: var(--text-primary); text-decoration: none; font-weight: 700; transition: color 0.2s; }
    .title-link:hover { color: var(--primary); text-decoration: underline; }
    .valign-icon { font-size: 14px; width: 14px; height: 14px; vertical-align: -2px; color: var(--accent); }
    .text-right { text-align: right; }
    .danger { color: #ef4444 !important; }
    .danger:hover { background: rgba(239,68,68,0.1) !important; border-color: #ef4444 !important; }
  `]
})
export class MyPostsComponent implements OnInit {
  posts: Post[] = [];
  loading = true;

  constructor(
    private postService: PostService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.postService.getMyPosts().subscribe({
      next: (res: BaseResponse<Post[]>) => {
        this.posts = res.data || [];
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Failed to load your stories.');
      }
    });
  }

  deletePost(post: Post): void {
    if (!confirm(`Are you sure you want to delete "${post.title}"?`)) return;

    this.postService.deletePost(post.postId).subscribe({
      next: () => {
        this.posts = this.posts.filter(p => p.postId !== post.postId);
        this.toastr.success('Story deleted.');
      },
      error: () => this.toastr.error('Failed to delete story.')
    });
  }
}
