import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BaseResponse, Notification } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private apiUrl = `${environment.apiUrl}/notifications`;

  constructor(private http: HttpClient) {}

  getNotifications(): Observable<BaseResponse<Notification[]>> {
    return this.http.get<BaseResponse<Notification[]>>(this.apiUrl);
  }

  markAsRead(id: string): Observable<BaseResponse<string>> {
    return this.http.patch<BaseResponse<string>>(`${this.apiUrl}/${id}/read`, {});
  }

  markAllAsRead(): Observable<BaseResponse<string>> {
    return this.http.patch<BaseResponse<string>>(`${this.apiUrl}/read-all`, {});
  }

  deleteNotification(id: string): Observable<BaseResponse<string>> {
    return this.http.delete<BaseResponse<string>>(`${this.apiUrl}/${id}`);
  }
}
