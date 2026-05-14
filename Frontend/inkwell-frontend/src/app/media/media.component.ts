/* 
 UI TEMPLATE STRUCTURE:
 1. Page container
 2. Header (icon + title)
 3. Card/Grid Layout
 4. Actions (buttons)
 5. Glassmorphism Design
 6. Toast feedback
*/

import { Component, OnInit } from '@angular/core';
import { ToastrService } from 'ngx-toastr';
import { MediaNewsletterService } from '../core/services/media-newsletter.service';
import { AuthService } from '../core/services/auth.service';
import { MediaFile, BaseResponse } from '../core/models/models';

@Component({
  selector: 'app-media',
  template: `
    <div class="app-container py-60">
      <header class="media-header mb-40 animate-in">
        <div class="badge-premium">Creative Assets</div>
        <h1 class="heading-xl">Visual <span class="text-gradient">Library</span></h1>
        <p class="text-secondary subtitle-xl">Manage and deploy your media assets across the platform.</p>
      </header>

      <!-- Premium Upload Zone -->
      <div class="glass-card p-40 mb-60 animate-in">
        <div class="drop-zone-premium" (click)="fileInput.click()" [class.active]="isDragging"
          (dragover)="onDragOver($event)" (dragleave)="isDragging=false" (drop)="onDrop($event)">
          <div class="zone-icon"><mat-icon>cloud_upload</mat-icon></div>
          <div class="zone-text">
            <h3>Deploy new assets</h3>
            <p>Drag and drop or click to browse. Supports JPG, PNG, WebP up to 10MB.</p>
          </div>
        </div>
        <input #fileInput type="file" hidden accept="image/*" (change)="onFileSelected($event)">

        <div *ngIf="selectedFile" class="staging-area mt-32 animate-in">
          <div class="staging-card">
            <img [src]="previewUrl" class="staging-preview">
            <div class="staging-details">
              <span class="file-name">{{ selectedFile.name }}</span>
              <span class="file-meta">{{ (selectedFile.size / 1024).toFixed(1) }} KB</span>
            </div>
            <div class="staging-actions">
              <button class="btn-premium sm" (click)="uploadFile()" [disabled]="uploading">
                <mat-spinner *ngIf="uploading" diameter="16" color="accent"></mat-spinner>
                <span *ngIf="!uploading">Start Deployment</span>
              </button>
              <button class="icon-btn-premium sm" (click)="clearSelection()">
                <mat-icon>close</mat-icon>
              </button>
            </div>
          </div>
        </div>
      </div>

      <div class="gallery-controls mb-24 animate-in">
         <h2 class="heading-sm">Your Collection <span class="text-muted">({{ uploadedFiles.length }})</span></h2>
      </div>

      <div class="media-grid-premium">
        <div *ngFor="let file of uploadedFiles" class="media-card-premium animate-in">
          <div class="media-visual">
            <img [src]="file.url" (error)="handleImageError($event)" loading="lazy">
            <div class="media-mask">
              <div class="mask-actions">
                <a [href]="file.url" target="_blank" class="mask-btn"><mat-icon>visibility</mat-icon></a>
                <button class="mask-btn danger" (click)="deleteFile(file)"><mat-icon>delete</mat-icon></button>
              </div>
            </div>
          </div>
          <div class="media-meta-strip">
            <span class="name" title="{{ file.fileName }}">{{ file.fileName }}</span>
            <span class="date">{{ file.uploadedAt | date: 'MMM d, y' }}</span>
          </div>
        </div>
      </div>

      <div *ngIf="!loading && uploadedFiles.length === 0" class="empty-gallery mt-60 animate-in">
         <div class="empty-art"></div>
         <h3 class="heading-sm">Library is Silent</h3>
         <p class="text-muted">No media assets found in your collection.</p>
      </div>
    </div>
  `,
  styles: [`
    .py-60 { padding-top: 60px; padding-bottom: 60px; }
    .mb-60 { margin-bottom: 60px; }
    .mb-40 { margin-bottom: 40px; }
    .mb-24 { margin-bottom: 24px; }
    .mt-32 { margin-top: 32px; }
    .mt-60 { margin-top: 60px; }
    .p-40 { padding: 40px; }

    .media-header { text-align: center; border-bottom: 1px solid var(--glass-border); padding-bottom: 40px; }
    .badge-premium { display: inline-block; padding: 4px 12px; background: rgba(99, 102, 241, 0.1); color: var(--primary); border-radius: 20px; font-size: 0.8rem; font-weight: 700; text-transform: uppercase; margin-bottom: 12px; }
    .subtitle-xl { font-size: 1.25rem; margin-top: 12px; }

    .drop-zone-premium {
      border: 2px dashed var(--glass-border); border-radius: 24px; padding: 60px;
      text-align: center; cursor: pointer; transition: all 0.3s;
      background: rgba(255,255,255,0.01); display: flex; flex-direction: column; align-items: center; gap: 20px;
    }
    .drop-zone-premium:hover, .drop-zone-premium.active { border-color: var(--primary); background: rgba(99, 102, 241, 0.05); }
    .zone-icon { width: 64px; height: 64px; background: var(--grad-main); border-radius: 20px; display: flex; align-items: center; justify-content: center; color: white; box-shadow: 0 10px 20px -5px rgba(99, 102, 241, 0.4); }
    .zone-icon mat-icon { font-size: 32px; width: 32px; height: 32px; }
    .zone-text h3 { margin: 0; font-size: 1.5rem; font-weight: 800; }
    .zone-text p { margin: 8px 0 0; color: var(--text-muted); }

    .staging-area { border-top: 1px solid var(--glass-border); padding-top: 32px; }
    .staging-card { display: flex; align-items: center; gap: 20px; background: rgba(255,255,255,0.05); padding: 16px; border-radius: 16px; }
    .staging-preview { width: 80px; height: 80px; border-radius: 12px; object-fit: cover; }
    .staging-details { flex: 1; display: flex; flex-direction: column; }
    .staging-details .file-name { font-weight: 700; font-size: 1rem; color: white; }
    .staging-details .file-meta { font-size: 0.8rem; color: var(--text-muted); }
    .staging-actions { display: flex; gap: 12px; }

    .media-grid-premium { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 24px; }
    .media-card-premium { border-radius: 20px; overflow: hidden; background: rgba(255,255,255,0.03); border: 1px solid var(--glass-border); transition: all 0.3s; }
    .media-card-premium:hover { border-color: var(--primary); transform: translateY(-8px); }
    
    .media-visual { position: relative; aspect-ratio: 16/10; overflow: hidden; background: #0f172a; }
    .media-visual img { width: 100%; height: 100%; object-fit: cover; }
    .media-mask { position: absolute; inset: 0; background: rgba(15, 23, 42, 0.8); backdrop-filter: blur(4px); display: flex; align-items: center; justify-content: center; opacity: 0; transition: all 0.3s; }
    .media-card-premium:hover .media-mask { opacity: 1; }
    
    .mask-actions { display: flex; gap: 16px; transform: translateY(20px); transition: all 0.4s; }
    .media-card-premium:hover .mask-actions { transform: translateY(0); }
    .mask-btn { width: 44px; height: 44px; border-radius: 12px; background: white; color: #0f172a; display: flex; align-items: center; justify-content: center; border: none; cursor: pointer; transition: all 0.2s; }
    .mask-btn:hover { transform: scale(1.1); }
    .mask-btn.danger:hover { background: #ef4444; color: white; }

    .media-meta-strip { padding: 16px; display: flex; flex-direction: column; }
    .media-meta-strip .name { font-weight: 700; font-size: 0.9rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .media-meta-strip .date { font-size: 0.75rem; color: var(--text-muted); margin-top: 4px; }

    .empty-gallery { text-align: center; }
    .empty-art { width: 80px; height: 80px; background: rgba(255,255,255,0.05); border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center; }
  `]
})
export class MediaComponent implements OnInit {
  selectedFile: File | null = null;
  previewUrl: string | null = null;
  uploading = false;
  loading = false;
  isDragging = false;
  uploadedFiles: MediaFile[] = [];

  constructor(
    private mediaService: MediaNewsletterService, 
    private authService: AuthService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    this.loadMedia();
  }

  loadMedia(): void {
    const user = this.authService.getCurrentUser();
    if (!user) return;

    this.loading = true;
    this.mediaService.getMediaByUser(user.id).subscribe({
      next: (res: BaseResponse<MediaFile[]>) => {
        this.loading = false;
        this.uploadedFiles = res.data || [];
      },
      error: () => {
        this.loading = false;
        this.toastr.error('Unable to sync your media library', 'Sync Error');
      }
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) this.preparePreview(input.files[0]);
  }

  onDragOver(event: DragEvent): void { event.preventDefault(); this.isDragging = true; }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    const file = event.dataTransfer?.files[0];
    if (file) this.preparePreview(file);
  }

  preparePreview(file: File): void {
    this.selectedFile = file;
    const reader = new FileReader();
    reader.onload = () => { this.previewUrl = reader.result as string; };
    reader.readAsDataURL(file);
  }

  uploadFile(): void {
    if (!this.selectedFile) return;
    this.uploading = true;
    this.mediaService.uploadMedia(this.selectedFile).subscribe({
      next: (res: BaseResponse<MediaFile>) => {
        this.uploading = false;
        if (res.success && res.data) {
          this.uploadedFiles.unshift(res.data);
          this.clearSelection();
          this.toastr.success('Your file is now in the library!', 'Upload Complete 🎨');
        }
      },
      error: () => { this.uploading = false; this.toastr.error('We could not upload your file.', 'Upload Error'); }
    });
  }

  deleteFile(file: MediaFile): void {
    if (!confirm('This will permanently delete this file. Continue?')) return;
    this.mediaService.deleteMedia(file.mediaId).subscribe({
      next: () => {
        this.uploadedFiles = this.uploadedFiles.filter(f => f.mediaId !== file.mediaId);
        this.toastr.info('File removed from library.', 'Deleted');
      },
      error: () => this.toastr.error('File could not be deleted.', 'Error')
    });
  }

  handleImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = 'https://via.placeholder.com/400x300?text=Image+Unavailable';
  }

  clearSelection(): void { this.selectedFile = null; this.previewUrl = null; }
}
