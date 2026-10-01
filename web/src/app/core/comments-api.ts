import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CommentDto, PagedResult, SortDir, SortField } from './models';

@Injectable({ providedIn: 'root' })
export class CommentsApi {
  private readonly http = inject(HttpClient);

  list(sortBy: SortField, sortDir: SortDir, page: number): Observable<PagedResult<CommentDto>> {
    const params = new HttpParams()
      .set('sortBy', sortBy)
      .set('sortDir', sortDir)
      .set('page', page);

    return this.http.get<PagedResult<CommentDto>>('/api/comments', { params });
  }
}