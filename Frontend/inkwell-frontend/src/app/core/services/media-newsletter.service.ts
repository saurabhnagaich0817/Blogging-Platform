import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BaseResponse, MediaFile, Subscriber } from '../models/models';

@Injectable({ providedIn: 'root' })
export class MediaNewsletterService {
  private mediaUrl = `${environment.apiUrl}/media`;
  private newsletterUrl = `${environment.apiUrl}/newsletter`;

  constructor(private http: HttpClient) {}

  // Media Methods
  uploadMedia(file: File, postId?: string): Observable<BaseResponse<MediaFile>> {
    const formData = new FormData();
    formData.append('File', file); // Capitalized to match DTO
    if (postId) formData.append('LinkedPostId', postId); // Match DTO property name
    return this.http.post<BaseResponse<MediaFile>>(`${this.mediaUrl}/upload`, formData);
  }

  getMediaById(id: string): Observable<BaseResponse<MediaFile>> {
    return this.http.get<BaseResponse<MediaFile>>(`${this.mediaUrl}/${id}`);
  }

  getMediaByPost(postId: string): Observable<BaseResponse<MediaFile[]>> {
    return this.http.get<BaseResponse<MediaFile[]>>(`${this.mediaUrl}/post/${postId}`);
  }
  
  getMediaByUser(userId: string): Observable<BaseResponse<MediaFile[]>> {
    return this.http.get<BaseResponse<MediaFile[]>>(`${this.mediaUrl}/user/${userId}`);
  }

  deleteMedia(id: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${this.mediaUrl}/${id}`);
  }

  getAllMedia(): Observable<BaseResponse<MediaFile[]>> {
    return this.http.get<BaseResponse<MediaFile[]>>(this.mediaUrl);
  }

  // Newsletter Methods
  subscribe(dto: any): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${this.newsletterUrl}/subscribe`, dto);
  }

  getAllSubscribers(): Observable<BaseResponse<Subscriber[]>> {
    return this.http.get<BaseResponse<Subscriber[]>>(this.newsletterUrl);
  }

  approve(id: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${this.newsletterUrl}/${id}/approve`, {});
  }

  reject(id: string): Observable<BaseResponse<string>> {
    return this.http.post<BaseResponse<string>>(`${this.newsletterUrl}/${id}/reject`, {});
  }
}
