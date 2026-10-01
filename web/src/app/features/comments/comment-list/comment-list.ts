import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { CommentsApi } from '../../../core/comments-api';
import { CommentDto, SortDir, SortField } from '../../../core/models';

@Component({
  selector: 'app-comment-list',
  imports: [DatePipe],
  templateUrl: './comment-list.html',
})
export class CommentList implements OnInit, OnDestroy {
  private readonly api = inject(CommentsApi);
  private request?: Subscription;

  protected readonly comments = signal<CommentDto[]>([]);
  protected readonly sortBy = signal<SortField>('date');
  protected readonly sortDir = signal<SortDir>('desc'); // по умолчанию LIFO: новые сверху
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }

  protected sort(field: SortField): void {
    if (this.sortBy() === field) {
      this.sortDir.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortBy.set(field);
      this.sortDir.set(field === 'date' ? 'desc' : 'asc');
    }
    this.load();
  }

  protected indicator(field: SortField): string {
    if (this.sortBy() !== field) {
      return '';
    }
    return this.sortDir() === 'asc' ? '▲' : '▼';
  }

  private load(): void {
    this.request?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);

    this.request = this.api.list(this.sortBy(), this.sortDir(), 1).subscribe({
      next: (result) => {
        this.comments.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Не удалось загрузить комментарии.');
        this.loading.set(false);
      },
    });
  }
}