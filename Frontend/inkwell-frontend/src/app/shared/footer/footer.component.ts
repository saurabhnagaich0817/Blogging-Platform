import { Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  template: `
    <footer class="footer">
      <div class="footer-container app-container">
        <div class="footer-grid">
          <!-- Brand -->
          <div class="footer-brand">
            <div class="logo-area">
              <mat-icon class="logo-icon">auto_stories</mat-icon>
              <span class="logo-text text-gradient">InkWell</span>
            </div>
            <p class="brand-pitch text-secondary">
              InkWell is a premium platform for writers and thinkers to share their stories with the world. Join our global community today.
            </p>
            <div class="social-links">
              <button class="icon-btn-premium sm" title="Facebook">
                <mat-icon>facebook</mat-icon>
              </button>
              <button class="icon-btn-premium sm" title="Twitter / X">
                <mat-icon>chat</mat-icon>
              </button>
              <button class="icon-btn-premium sm" title="Instagram">
                <mat-icon>photo_camera</mat-icon>
              </button>
            </div>
          </div>

          <!-- Explore Links -->
          <div class="footer-links">
            <h3>Explore</h3>
            <ul>
              <li><a routerLink="/posts">Stories</a></li>
              <li><a routerLink="/explore">Trending</a></li>
              <li><a routerLink="/digest">Newsletter</a></li>
              <li><a routerLink="/community">Community</a></li>
            </ul>
          </div>

          <!-- Platform Links -->
          <div class="footer-links">
            <h3>Platform</h3>
            <ul>
              <li><a routerLink="/dashboard">Dashboard</a></li>
              <li><a routerLink="/posts/create">Write a Post</a></li>
              <li><a routerLink="/media">Media Library</a></li>
              <li><a routerLink="/newsletter">Newsletter Admin</a></li>
            </ul>
          </div>
        </div>

        <div class="footer-divider"></div>

        <div class="footer-bottom">
          <p>&copy; 2026 InkWell Platform. Crafted with ❤️ for the community.</p>
          <div class="status-indicator">
            <span class="dot"></span>
            Systems Operational
          </div>
        </div>
      </div>
    </footer>
  `,
  styles: [`
    .footer {
      background: var(--glass-bg);
      backdrop-filter: blur(10px);
      -webkit-backdrop-filter: blur(10px);
      border-top: 1px solid var(--glass-border);
      padding: 80px 0 40px;
      margin-top: 80px;
    }

    .footer-grid {
      display: grid;
      grid-template-columns: 2fr 1fr 1fr;
      gap: 60px;
      margin-bottom: 60px;
    }

    .logo-area { display: flex; align-items: center; gap: 10px; margin-bottom: 20px; }
    .logo-icon { font-size: 2.2rem; width: 2.2rem; height: 2.2rem; color: var(--primary); }
    .logo-text { font-size: 1.8rem; font-weight: 850; letter-spacing: -1px; }

    .brand-pitch { line-height: 1.8; font-size: 1rem; max-width: 360px; margin-bottom: 24px; }

    .social-links { display: flex; gap: 10px; }

    .footer-links h3 {
      font-size: 0.85rem; font-weight: 800; color: var(--text-muted);
      text-transform: uppercase; letter-spacing: 0.1em; margin-bottom: 24px;
    }
    .footer-links ul { list-style: none; padding: 0; margin: 0; }
    .footer-links li { margin-bottom: 14px; }
    .footer-links a {
      text-decoration: none; color: var(--text-secondary); transition: color 0.2s; font-size: 0.95rem; font-weight: 500;
    }
    .footer-links a:hover { color: var(--primary); }

    .footer-divider { height: 1px; background: var(--glass-border); margin-bottom: 40px; }

    .footer-bottom {
      display: flex; justify-content: space-between; align-items: center;
      color: var(--text-muted); font-size: 0.9rem;
    }
    .footer-bottom p { margin: 0; }

    .status-indicator { display: flex; align-items: center; gap: 8px; font-weight: 600; color: #22c55e; }
    .dot {
      width: 8px; height: 8px; background: #22c55e; border-radius: 50%; display: inline-block;
      box-shadow: 0 0 10px #22c55e;
      animation: pulse 2s infinite;
    }
    @keyframes pulse { 0%, 100% { opacity: 1; } 50% { opacity: 0.5; } }

    @media (max-width: 800px) {
      .footer-grid { grid-template-columns: 1fr; gap: 40px; }
      .footer-bottom { flex-direction: column; gap: 20px; text-align: center; }
    }
  `]
})
export class FooterComponent {}
