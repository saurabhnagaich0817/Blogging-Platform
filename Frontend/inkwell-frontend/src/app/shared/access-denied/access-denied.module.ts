import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../shared.module';
import { Component } from '@angular/core';

@Component({
  selector: 'app-access-denied',
  template: `
    <div class="denied-container">
      <mat-icon class="denied-icon">block</mat-icon>
      <h1>Access Denied</h1>
      <p>You don't have permission to view this page.</p>
      <button mat-raised-button color="primary" routerLink="/posts">
        <mat-icon>home</mat-icon> Go Home
      </button>
    </div>
  `,
  styles: [`
    .denied-container {
      min-height: calc(100vh - 64px);
      display: flex; flex-direction: column;
      align-items: center; justify-content: center;
      text-align: center; padding: 40px;
    }
    .denied-icon { font-size: 6rem; width: 6rem; height: 6rem; color: #e53935; }
    h1 { font-size: 2.5rem; color: #333; margin: 16px 0 8px; }
    p { color: #888; font-size: 1.1rem; margin-bottom: 24px; }
  `]
})
export class AccessDeniedComponent {}

@NgModule({
  declarations: [AccessDeniedComponent],
  imports: [
    RouterModule.forChild([{ path: '', component: AccessDeniedComponent }]),
    SharedModule
  ]
})
export class AccessDeniedModule {}
