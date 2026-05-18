// ═══════════════════════════════════════════════════
// InkWell — Complete Data Models (aligned to backend)
// ═══════════════════════════════════════════════════

/** Standard API response wrapper from BaseResponse<T> */
export interface BaseResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
}

/** Auth response after login/register */
export interface AuthResponse {
  token: string;
  message: string;
  isSuccess: boolean;
  isNewUser?: boolean;
}

/** User model decoded from JWT + API */
export interface User {
  id: string;
  userId?: string; // Align with backend DTO
  email: string;
  role: 'Admin' | 'Author' | 'Reader' | string;
  username?: string;
  fullName?: string;
  displayName?: string;  
  profilePictureUrl?: string;
  phoneNumber?: string;
  linkedInUrl?: string;
  githubUrl?: string;
  bio?: string;
  createdAt?: string;
}

/** Full user profile from GET /api/users/profile/:username */
export interface UserProfile {
  id: string;
  userId?: string; // Align with backend DTO
  email: string;
  username: string;
  fullName: string;
  displayName?: string;
  role: string;
  profilePictureUrl?: string;
  phoneNumber?: string;
  linkedInUrl?: string;
  githubUrl?: string;
  bio?: string;
  createdAt?: string;
}

/** Post from backend PostResponseDTO */
export interface Post {
  postId: string;
  title: string;
  content: string;
  slug: string;
  authorId: string;
  authorName?: string;
  categoryId?: string;
  categoryName?: string;
  imageUrl?: string;
  likesCount: number;
  status?: string;
  createdAt: string;
  updatedAt?: string | null;
}

/** DTO sent to POST /api/posts */
export interface CreatePostRequest {
  title: string;
  content: string;
  imageUrl?: string;
  status?: string;
  categoryId?: string;
  authorName?: string;
}

/** DTO sent to PUT /api/posts/:id */
export interface UpdatePostRequest {
  title?: string;
  content?: string;
  imageUrl?: string;
  status?: string;
  categoryId?: string | null;
  categoryName?: string | null;
}

/** Comment from backend CommentResponseDTO */
export interface Comment {
  commentId: string;
  postId: string;
  content: string;
  authorId: string;
  authorName?: string;
  parentCommentId?: string | null;
  status: string;
  likesCount: number;
  createdAt: string;
  updatedAt?: string | null;
  replies?: Comment[];  // for client-side threaded display
}

/** DTO sent to POST /api/comments */
export interface CreateCommentRequest {
  postId: string;
  content: string;
  postAuthorId?: string;
  parentCommentId?: string | null;
}

/** Category from backend CategoryResponseDTO */
export interface Category {
  categoryId: string;
  name: string;
  slug: string;
  parentCategoryId?: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  parentCategoryId?: string | null;
}

/** Tag from backend TagResponseDTO */
export interface Tag {
  tagId: string;
  name: string;
  slug: string;
  postCount: number;
}

export interface CreateTagRequest {
  name: string;
}

/** Media file from backend MediaResponseDTO */
export interface MediaFile {
  mediaId: string;
  fileName: string;
  url: string;
  altText?: string;
  uploadedAt: string;
  uploaderId?: string;
}

/** Newsletter subscriber from backend SubscriberResponseDTO */
export interface Subscriber {
  subscriberId: string;
  email: string;
  fullName: string;
  status: 'Pending' | 'Active' | 'Rejected' | 'Unsubscribed' | string;
  subscribedAt?: string;
  confirmationToken?: string;
}

/** Newsletter subscribe request */
export interface SubscribeRequest {
  email: string;
  fullName?: string;
  userId?: string;
}

/** Notification from backend Notification model */
export interface Notification {
  notificationId: string;
  userId: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  relatedId?: string;
  createdAt: string;
}

/** Dashboard analytics from backend AnalyticsResponseDTO */
export interface AnalyticsResponse {
  totalStories: number;
  totalEngagement: number;
  storageUsagePercentage: number;
  trendingStories: TrendingStory[];
  sixthOccurrencePosition: number;
}

export interface TrendingStory {
  postId: string;
  title: string;
  likes: number;
  views: number;
}

/** Registration DTO — includes role selection */
export interface RegisterRequest {
  fullName: string;
  username: string;
  email: string;
  password: string;
  role: 'Reader' | 'Author';
}
