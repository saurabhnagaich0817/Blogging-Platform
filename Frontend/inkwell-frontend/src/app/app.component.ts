import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: `
    <app-navbar></app-navbar>
    <main class="main-content">
      <router-outlet></router-outlet>
    </main>
    <app-footer></app-footer>
  `,
  styles: [`
    .main-content {
      min-height: calc(100vh - 80px); /* Adjust based on navbar height */
      padding-top: 80px; /* Spacer for fixed navbar */
    }
  `]
})
export class AppComponent {}
