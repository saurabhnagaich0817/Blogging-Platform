# 🎨 InkWell - Frontend (Angular 17)

The professional, modern, and highly interactive frontend for the **InkWell Blogging Platform**. Built with **Angular 17**, it provides a seamless user experience for writers and readers alike.

---

## ✨ Features
- **Modern Dashboard**: Role-based views for Admins and Readers.
- **Dynamic Content**: Real-time post feed with advanced filtering by categories.
- **Authentication**: Secure Login/Signup with JWT and Google OAuth integration.
- **Rich Interaction**: Like, Comment, and Save features.
- **Media Management**: Integrated image uploading and gallery view.
- **Responsive Design**: Fully optimized for Mobile, Tablet, and Desktop.

---

## 🛠️ Tech Stack
- **Framework**: Angular 17
- **Styling**: Vanilla CSS (Premium Custom Design)
- **State Management**: Reactive services with RxJS
- **Authentication**: JWT Interceptors
- **Icons**: Lucide Angular & FontAwesome
- **Animations**: Angular Animations & CSS Transitions

---

## 🚀 Getting Started

### Prerequisites
- Node.js (v18+)
- Angular CLI (`npm install -g @angular/cli`)

### Setup
1. **Clone the Repo**:
   ```bash
   git clone <your-frontend-repo-url>
   ```
2. **Install Dependencies**:
   ```bash
   npm install
   ```
3. **Configure Environment**:
   Check `src/environments/environment.ts` and ensure `apiUrl` points to your Gateway (default: `http://localhost:5000/api`).
4. **Run Development Server**:
   ```bash
   npm start
   ```
   Navigate to `http://localhost:4200`.

---

## 🏗️ Folder Structure
- `src/app/core`: Singleton services (Auth, API, Interceptors).
- `src/app/shared`: Reusable UI components and modules.
- `src/app/pages`: Main page layouts (Home, PostDetails, Profile).
- `src/app/admin`: Specialized dashboard for platform management.

---

## 👔 Architecture Highlights
- **Interceptors**: Automatically attach JWT tokens to all outgoing API requests.
- **Guards**: Protect routes (Admin Dashboard) from unauthorized access.
- **Lazy Loading**: Optimized bundle size for faster initial load.

---

Developed with ❤️ by **Saurabh Nagayach**
