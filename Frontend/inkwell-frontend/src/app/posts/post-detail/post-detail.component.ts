import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../core/services/auth.service';
import { CommentService } from '../../core/services/comment.service';
import { BaseResponse, Comment, Post, User } from '../../core/models/models';
import { PostService } from '../../core/services/post.service';

@Component({
  selector: 'app-post-detail',
  template: `
    <div *ngIf="loading" class="story-loading">
      <div class="loading-art"></div>
      <h3>Loading story...</h3>
    </div>

    <div *ngIf="!loading && !post" class="app-container py-60">
      <div class="glass-card state-card animate-in">
        <mat-icon>article</mat-icon>
        <h2>Post not found</h2>
        <p>{{ errorMessage || 'This story may have been removed or the link is invalid.' }}</p>
        <button class="btn-premium outline mt-24" routerLink="/posts">Back to Stories</button>
      </div>
    </div>

    <div *ngIf="!loading && post" class="reader-view animate-in">
      <div class="story-hero">
        <div class="hero-bg-container">
          <img [src]="post.imageUrl || defaultImage" class="hero-image" [alt]="post.title" (error)="handleImageError($event)">
          <div class="hero-overlay-gradient"></div>
        </div>

        <div class="hero-meta-content app-container">
          <span class="badge-amber mb-20" style="display:inline-flex">{{ post.status || 'Published' }}</span>
          <h1 class="hero-title">{{ post.title }}</h1>

          <div class="hero-author-strip">
            <div class="author-avatar-group">
              <div class="avatar-glow"></div>
              <div class="main-avatar">{{ getInitial(post.authorName || post.title) }}</div>
            </div>

            <div class="author-details">
              <span class="name">{{ post.authorName || 'Anonymous' }}</span>
              <span class="meta">{{ post.createdAt | date:'longDate' }} • {{ estimateReadTime(post.content) }} min read</span>
            </div>

            <div class="flex gap-12" style="margin-left: auto;">
              <button *ngIf="canEditPost" class="icon-btn-premium outline" routerLink="/posts/edit/{{ post.postId }}" title="Edit Post">
                <mat-icon>edit</mat-icon>
              </button>
              <button *ngIf="canManagePost" class="icon-btn-premium danger-hover" (click)="deletePost()" title="Delete Post">
                <mat-icon>delete</mat-icon>
              </button>
            </div>
          </div>
        </div>
      </div>

      <div class="story-layout app-container mt-40 mb-80">
        <!-- Sidebar Actions -->
        <aside class="sidebar-interactions">
          <div class="interactions-glass">
            <div class="interact-item">
              <button class="interact-btn" [class.active]="post.likesCount > 0" (click)="toggleLike()">
                <mat-icon>{{ post.likesCount > 0 ? 'favorite' : 'favorite_border' }}</mat-icon>
              </button>
              <span>{{ post.likesCount || 0 }}</span>
            </div>
            <div class="interact-item">
              <button class="interact-btn" [class.active]="isSaved" (click)="toggleSave()">
                <mat-icon>{{ isSaved ? 'bookmark' : 'bookmark_border' }}</mat-icon>
              </button>
            </div>
            <div class="interact-item">
              <button class="interact-btn" (click)="sharePost()">
                <mat-icon>share</mat-icon>
              </button>
            </div>
            <div class="divider-amber my-16" style="width: 20px; opacity: 0.5;"></div>
            <div class="interact-item">
              <button class="interact-btn" (click)="scrollToComments()">
                <mat-icon>chat_bubble_outline</mat-icon>
              </button>
              <span>{{ comments.length }}</span>
            </div>
          </div>
        </aside>

        <!-- Main Content -->
        <main class="story-main">
          <div class="glass-card story-body-card no-hover">
            <div class="content-render" [innerHTML]="renderContent(post.content)"></div>
          </div>

          <!-- Discussion Section -->
          <section id="comments" class="discussion-section mt-80">
            <div class="flex-between mb-32 border-bottom pb-16">
              <h2 class="heading-md">Discussion ({{ comments.length }})</h2>
              <span class="text-secondary" style="font-size: 0.85rem;">Be respectful and kind.</span>
            </div>

            <!-- Comment Composer -->
            <div *ngIf="currentUser; else signInToComment" class="comment-composer mb-40">
              <div class="composer-avatar">{{ getInitial(currentUser.username || currentUser.fullName || 'U') }}</div>
              <div class="composer-input-area">
                <textarea
                  placeholder="What are your thoughts?"
                  [(ngModel)]="newCommentContent"
                  [disabled]="submittingComment"></textarea>
                <div class="composer-footer">
                  <button class="btn-premium sm" [disabled]="submittingComment || !newCommentContent.trim()" (click)="addComment()">
                    <mat-spinner *ngIf="submittingComment" diameter="16"></mat-spinner>
                    <span *ngIf="!submittingComment">Publish</span>
                  </button>
                </div>
              </div>
            </div>

            <ng-template #signInToComment>
              <div class="glass-card state-card compact mb-40">
                <mat-icon>login</mat-icon>
                <h3>Sign in to join the conversation</h3>
                <p>You need an account before you can like, save, or comment on stories.</p>
                <button class="btn-premium outline mt-20" routerLink="/auth/login">Sign In</button>
              </div>
            </ng-template>

            <!-- Comments List -->
            <div class="comments-stack" *ngIf="comments.length > 0; else emptyComments">
              <div *ngFor="let comment of comments" class="comment-node animate-in">
                <div class="comment-header">
                  <div class="avatar-sm">{{ getInitial(comment.authorName || 'U') }}</div>
                  <div class="comment-meta">
                    <span class="author-name">{{ comment.authorName || 'User' }}</span>
                    <span class="time">{{ comment.createdAt | date:'mediumDate' }}</span>
                    <span class="badge-green ml-8" *ngIf="comment.status === 'Approved'" style="font-size: 0.6rem; padding: 2px 6px;">Approved</span>
                    <span class="badge-amber ml-8" *ngIf="comment.status === 'Pending'" style="font-size: 0.6rem; padding: 2px 6px;">Pending</span>
                  </div>
                  
                  <!-- Admin/Author Moderation Controls -->
                  <div class="comment-actions" *ngIf="canModerateComments && comment.status === 'Pending'">
                    <button class="icon-btn-premium sm" (click)="approveComment(comment.commentId)" title="Approve"><mat-icon style="color: #22c55e">check</mat-icon></button>
                    <button class="icon-btn-premium sm danger" (click)="rejectComment(comment.commentId)" title="Reject"><mat-icon style="color: #ef4444">close</mat-icon></button>
                  </div>
                  
                  <!-- Delete Comment (Own or Admin) -->
                  <button *ngIf="canDeleteComment(comment)" class="text-btn danger ml-auto" (click)="deleteComment(comment.commentId)">
                    Delete
                  </button>
                </div>

                <div class="comment-body">{{ comment.content }}</div>

                <div class="comment-footer">
                  <button class="interact-sm" (click)="likeComment(comment)">
                    <mat-icon>thumb_up_off_alt</mat-icon>
                    <span>{{ comment.likesCount || 0 }}</span>
                  </button>
                </div>
              </div>
            </div>

            <ng-template #emptyComments>
              <div class="empty-state text-center py-60">
                <p class="text-secondary">Be the first to share your thoughts.</p>
              </div>
            </ng-template>
          </section>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .border-bottom { border-bottom: 1px solid var(--glass-border-subtle); }
    .pb-16 { padding-bottom: 16px; }
    .ml-8 { margin-left: 8px; }
    .ml-auto { margin-left: auto; }
    .my-16 { margin-top: 16px; margin-bottom: 16px; }

    .reader-view { position: relative; }
    .story-hero {
      min-height: 500px; display: flex; align-items: flex-end; position: relative; padding-bottom: 60px;
    }
    .hero-bg-container { position: absolute; inset: 0; }
    .hero-image { width: 100%; height: 100%; object-fit: cover; }
    .hero-overlay-gradient {
      position: absolute; inset: 0;
      background: linear-gradient(180deg, rgba(8,8,16,0.2) 0%, rgba(8,8,16,0.8) 50%, var(--bg-deep) 100%);
    }
    .hero-meta-content { position: relative; z-index: 2; width: 100%; max-width: 960px !important; }
    
    .hero-title {
      margin: 0 0 24px; font-family: 'Playfair Display', serif; font-size: clamp(2.5rem, 5vw, 4rem);
      line-height: 1.1; font-weight: 900; color: white; letter-spacing: -0.02em;
    }
    .hero-author-strip { display: flex; align-items: center; gap: 16px; }
    .author-avatar-group { position: relative; width: 56px; height: 56px; }
    .avatar-glow { position: absolute; inset: -2px; background: var(--grad-main); border-radius: 50%; opacity: 0.8; }
    .main-avatar {
      position: relative; width: 100%; height: 100%; border-radius: 50%; background: var(--bg-deep);
      display: flex; align-items: center; justify-content: center; color: white; font-weight: 800; font-size: 1.2rem;
      border: 3px solid var(--bg-deep);
    }
    .author-details .name { display: block; color: white; font-size: 1.05rem; font-weight: 700; }
    .author-details .meta { display: block; color: var(--text-secondary); font-size: 0.9rem; }

    .story-layout { display: grid; grid-template-columns: 70px minmax(0, 1fr); gap: 48px; }
    
    /* Sidebar */
    .sidebar-interactions { position: sticky; top: 120px; height: fit-content; z-index: 10; }
    .interactions-glass {
      border: 1px solid var(--glass-border-subtle); border-radius: 99px;
      background: rgba(15, 15, 25, 0.4); backdrop-filter: blur(16px);
      padding: 24px 12px; display: flex; flex-direction: column; align-items: center; gap: 20px;
      box-shadow: 0 8px 32px rgba(0,0,0,0.3); width: 64px;
    }
    .interact-item { display: flex; flex-direction: column; align-items: center; gap: 4px; }
    .interact-item span { font-size: 0.75rem; font-weight: 700; color: var(--text-muted); }
    .interact-btn {
      width: 40px; height: 40px; border-radius: 50%; border: none; background: transparent;
      color: var(--text-secondary); display: flex; align-items: center; justify-content: center;
      cursor: pointer; transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .interact-btn:hover { background: rgba(255,255,255,0.08); color: var(--primary); transform: translateY(-2px); }
    .interact-btn.active { color: var(--primary); background: rgba(245,158,11,0.08); }
    .interact-btn mat-icon { width: 24px; height: 24px; font-size: 24px; }

    /* Main Content */
    .story-main { max-width: 800px; margin: 0 auto; width: 100%; }
    .story-body-card { padding: 60px 80px; font-size: 1.15rem; line-height: 1.9; background: var(--bg-card); border-color: var(--glass-border-subtle); border-radius: 24px; }
    .content-render { color: var(--text-primary); font-family: 'Inter', sans-serif; }
    .content-render ::ng-deep p { margin-bottom: 1.5rem; color: #e2e8f0; }
    .content-render ::ng-deep h1, .content-render ::ng-deep h2, .content-render ::ng-deep h3 { 
      margin: 2rem 0 1rem; font-family: 'Playfair Display', serif; color: white;
    }
    .content-render ::ng-deep a { color: var(--primary); text-decoration: none; }
    .content-render ::ng-deep a:hover { text-decoration: underline; }
    .content-render ::ng-deep blockquote { 
      border-left: 4px solid var(--primary); margin: 2rem 0; padding: 1rem 1.5rem; 
      background: rgba(245,158,11,0.05); border-radius: 0 8px 8px 0; font-style: italic; color: var(--text-secondary);
    }
    .content-render ::ng-deep img { max-width: 100%; border-radius: 12px; margin: 2rem 0; box-shadow: 0 8px 24px rgba(0,0,0,0.4); }

    /* Comments Section */
    .comment-composer { display: flex; gap: 16px; }
    .composer-avatar { width: 40px; height: 40px; border-radius: 50%; background: var(--grad-main); display: flex; align-items: center; justify-content: center; color: #000; font-weight: 800; flex-shrink: 0; }
    .composer-input-area {
      flex: 1; background: var(--bg-card); border: 1px solid var(--glass-border-subtle); border-radius: var(--radius-md);
      overflow: hidden; transition: border-color 0.2s;
    }
    .composer-input-area:focus-within { border-color: var(--primary); }
    .composer-input-area textarea {
      width: 100%; min-height: 100px; border: none; resize: vertical; outline: none; background: transparent;
      color: var(--text-primary); font-size: 0.95rem; line-height: 1.5; padding: 16px; font-family: 'Inter', sans-serif;
    }
    .composer-footer { padding: 10px 16px; display: flex; justify-content: flex-end; background: rgba(255,255,255,0.02); border-top: 1px solid var(--glass-border-subtle); }

    .comments-stack { display: flex; flex-direction: column; gap: 24px; }
    .comment-node { padding-bottom: 24px; border-bottom: 1px solid var(--glass-border-subtle); }
    .comment-node:last-child { border-bottom: none; }
    
    .comment-header { display: flex; align-items: center; gap: 12px; margin-bottom: 12px; }
    .comment-meta { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .author-name { font-weight: 700; font-size: 0.95rem; color: var(--text-primary); }
    .comment-meta .time { font-size: 0.8rem; color: var(--text-muted); }
    .text-btn { background: none; border: none; font-size: 0.8rem; font-weight: 600; cursor: pointer; padding: 0; }
    .text-btn:hover { text-decoration: underline; }
    .text-btn.danger { color: var(--text-muted); }
    .text-btn.danger:hover { color: #ef4444; }
    
    .comment-body { color: var(--text-secondary); line-height: 1.6; font-size: 0.95rem; margin-bottom: 12px; }
    
    .comment-footer { display: flex; gap: 16px; }
    .interact-sm {
      background: none; border: none; padding: 4px 8px; border-radius: 6px;
      display: inline-flex; align-items: center; gap: 6px; color: var(--text-muted);
      font-size: 0.85rem; font-weight: 600; cursor: pointer; transition: all 0.2s;
    }
    .interact-sm:hover { background: rgba(255,255,255,0.05); color: var(--text-primary); }
    .interact-sm mat-icon { font-size: 16px; width: 16px; height: 16px; }

    .story-loading { min-height: 100vh; display: flex; flex-direction: column; justify-content: center; align-items: center; gap: 20px; }
    .loading-art { width: 50px; height: 50px; border: 3px solid var(--glass-border-subtle); border-top-color: var(--primary); border-radius: 50%; animation: spin 1s linear infinite; }

    .danger-hover:hover { border-color: #ef4444 !important; color: #ef4444 !important; background: rgba(239,68,68,0.1) !important; }

    @media (max-width: 1024px) {
      .story-layout { grid-template-columns: 1fr; }
      .sidebar-interactions { display: none; } /* Hide sticky sidebar on mobile */
      .story-body-card { padding: 32px 24px; }
      .story-hero { min-height: 400px; }
    }
  `]
})
export class PostDetailComponent implements OnInit, OnDestroy {
  readonly defaultImage = 'https://images.unsplash.com/photo-1499750310107-5fef28a66643?auto=format&fit=crop&q=80&w=1200';

  post: Post | null = null;
  comments: Comment[] = [];
  newCommentContent = '';
  loading = true;
  submittingComment = false;
  errorMessage = '';
  isSaved = false;
  currentUser: User | null = null;

  private authSubscription?: Subscription;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private postService: PostService,
    private commentService: CommentService,
    private authService: AuthService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.authSubscription = this.authService.currentUser$.subscribe((user) => {
      this.currentUser = user;
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading = false;
      this.errorMessage = 'A post ID was not provided.';
      return;
    }

    this.loadPost(id);
    this.loadComments(id);
  }

  get canManagePost(): boolean {
    if (!this.currentUser || !this.post) return false;
    return this.currentUser.role === 'Admin' || this.currentUser.id.toLowerCase() === this.post.authorId.toLowerCase();
  }

  get canEditPost(): boolean {
    if (!this.currentUser || !this.post) return false;
    return this.currentUser.role === 'Admin' || this.currentUser.id.toLowerCase() === this.post.authorId.toLowerCase();
  }

  get canModerateComments(): boolean {
    if (!this.currentUser || !this.post) return false;
    return this.currentUser.role === 'Admin' || this.currentUser.id.toLowerCase() === this.post.authorId.toLowerCase();
  }

  loadPost(id: string): void {
    this.loading = true;
    this.postService.getPostById(id).subscribe({
      next: (response: BaseResponse<Post>) => {
        this.post = response.data || null;
        this.loading = false;
        if (!this.post) this.errorMessage = 'The requested story does not exist.';
      },
      error: (error) => {
        this.loading = false;
        this.post = null;
        this.errorMessage = error.error?.message || 'The story could not be loaded.';
      }
    });
  }

  loadComments(postId: string): void {
    this.commentService.getCommentsByPost(postId).subscribe({
      next: (response: BaseResponse<Comment[]>) => {
        this.comments = response.data || [];
      },
      error: () => { this.comments = []; }
    });
  }

  scrollToComments(): void {
    document.getElementById('comments')?.scrollIntoView({ behavior: 'smooth' });
  }

  toggleLike(): void {
    if (!this.post || !this.ensureLoggedIn('like stories')) return;

    this.postService.toggleLike(this.post.postId).subscribe({
      next: (response: BaseResponse<number>) => {
        this.post!.likesCount = response.data || 0;
      },
      error: (error) => {
        this.toastr.error('Could not update like.', 'Error');
      }
    });
  }

  toggleSave(): void {
    if (!this.post || !this.ensureLoggedIn('save stories')) return;

    this.postService.toggleSave(this.post.postId).subscribe({
      next: (response: BaseResponse<boolean>) => {
        this.isSaved = !!response.data;
        this.toastr.success(this.isSaved ? 'Saved to Reading List.' : 'Removed from Reading List.');
      },
      error: (error) => {
        this.toastr.error('Could not save post.', 'Error');
      }
    });
  }

  sharePost(): void {
    const url = window.location.href;
    navigator.clipboard.writeText(url).then(() => {
      this.toastr.success('Link copied to clipboard.');
    });
  }

  addComment(): void {
    if (!this.post || !this.ensureLoggedIn('comment')) return;

    const content = this.newCommentContent.trim();
    if (!content) return;

    this.submittingComment = true;
    this.commentService.addComment({
      postId: this.post.postId,
      content,
      postAuthorId: this.post.authorId
    }).subscribe({
      next: (response: BaseResponse<Comment>) => {
        this.submittingComment = false;
        if (response.data) {
          this.comments.unshift(response.data);
          this.newCommentContent = '';
          this.toastr.success('Comment posted successfully.');
        }
      },
      error: (error) => {
        this.submittingComment = false;
        this.toastr.error('Could not publish comment.');
      }
    });
  }

  likeComment(comment: Comment): void {
    if (!this.ensureLoggedIn('like comments')) return;
    this.commentService.likeComment(comment.commentId).subscribe({
      next: () => {
        comment.likesCount++;
      }
    });
  }

  canDeleteComment(comment: Comment): boolean {
    if (!this.currentUser) return false;
    return this.currentUser.role === 'Admin' || this.currentUser.id.toLowerCase() === comment.authorId.toLowerCase();
  }

  deleteComment(commentId: string): void {
    if (!confirm('Delete this comment permanently?')) return;
    
    this.commentService.deleteComment(commentId).subscribe({
      next: () => {
        this.comments = this.comments.filter(c => c.commentId !== commentId);
        this.toastr.success('Comment deleted.');
      }
    });
  }

  approveComment(commentId: string): void {
    this.commentService.approveComment(commentId).subscribe({
      next: () => {
        const c = this.comments.find(x => x.commentId === commentId);
        if (c) c.status = 'Approved';
        this.toastr.success('Comment approved.');
      }
    });
  }

  rejectComment(commentId: string): void {
    this.commentService.rejectComment(commentId).subscribe({
      next: () => {
        const c = this.comments.find(x => x.commentId === commentId);
        if (c) c.status = 'Rejected';
        this.toastr.success('Comment rejected.');
      }
    });
  }

  deletePost(): void {
    if (!this.post || !confirm('Delete this story permanently? This cannot be undone.')) return;

    this.postService.deletePost(this.post.postId).subscribe({
      next: () => {
        this.toastr.success('Story deleted successfully.');
        this.router.navigate(['/posts']);
      },
      error: () => this.toastr.error('Could not delete story.')
    });
  }

  renderContent(content: string): string {
    if (/<[a-z][\s\S]*>/i.test(content)) return content;
    return content.split('\n').filter(line => !!line.trim()).map(line => `<p>${this.escapeHtml(line)}</p>`).join('');
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
    image.src = this.defaultImage;
  }

  ngOnDestroy(): void {
    this.authSubscription?.unsubscribe();
  }

  private ensureLoggedIn(action: string): boolean {
    if (this.currentUser) return true;
    this.toastr.info(`Please sign in to ${action}.`);
    this.router.navigate(['/auth/login']);
    return false;
  }

  private escapeHtml(value: string): string {
    return value.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
  }
}
