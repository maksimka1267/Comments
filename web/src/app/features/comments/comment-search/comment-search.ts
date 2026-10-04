import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

import {
  Component,
  OnDestroy,
  computed,
  inject,
  signal,
} from '@angular/core';

import { Subscription } from 'rxjs';

import { CommentsApi } from '../../../core/comments-api';

import {
  CommentDto,
  SearchHit,
} from '../../../core/models';

import { visiblePages } from '../comment-list/pagination';


@Component({
  selector: 'app-comment-search',

  imports: [DatePipe],

  templateUrl: './comment-search.html',
  styleUrl: './comment-search.scss',
})
export class CommentSearch implements OnDestroy {

  private readonly api = inject(CommentsApi);

  private request?: Subscription;

  private expandRequest?: Subscription;


  // Query that results are shown for (null: search is not active)
  protected readonly active =
    signal<string | null>(null);

  protected readonly hits =
    signal<SearchHit[]>([]);


  // Pagination
  protected readonly page =
    signal(1);

  protected readonly totalPages =
    signal(0);

  protected readonly totalCount =
    signal(0);


  // UI state
  protected readonly loading =
    signal(false);

  protected readonly error =
    signal<string | null>(null);


  // Hit that is expanded to the full comment
  protected readonly expandedId =
    signal<string | null>(null);

  protected readonly expanded =
    signal<CommentDto | null>(null);

  protected readonly expandLoading =
    signal(false);


  // Pagination buttons
  protected readonly pages =
    computed(() =>
      visiblePages(
        this.page(),
        this.totalPages(),
      ),
    );


  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.expandRequest?.unsubscribe();
  }


  /**
   * Run a new search (Enter or the search button).
   */
  protected submit(
    event: Event,
    value: string,
  ): void {

    event.preventDefault();

    const query = value.trim();

    if (!query) {
      this.clearResults();
      return;
    }

    this.active.set(query);
    this.page.set(1);

    this.load();
  }


  /**
   * Reset the input and close the results.
   */
  protected clear(
    input: HTMLInputElement,
  ): void {

    input.value = '';

    this.clearResults();
  }


  /**
   * Change page of results.
   */
  protected goTo(page: number): void {

    if (
      page < 1 ||
      page > this.totalPages() ||
      page === this.page()
    ) {
      return;
    }

    this.page.set(page);

    this.load();
  }


  /**
   * Show or hide the full comment of a hit.
   */
  protected toggle(id: string): void {

    if (this.expandedId() === id) {
      this.collapse();
      return;
    }

    this.collapse();

    this.expandedId.set(id);
    this.expandLoading.set(true);

    this.expandRequest = this.api
      .getById(id)

      .subscribe({

        next: (comment) => {
          this.expanded.set(comment);
          this.expandLoading.set(false);
        },

        error: () => {
          this.collapse();
          this.error.set('Не удалось загрузить комментарий.');
        },

      });
  }


  private collapse(): void {

    this.expandRequest?.unsubscribe();

    this.expandedId.set(null);
    this.expanded.set(null);
    this.expandLoading.set(false);
  }


  private clearResults(): void {

    this.request?.unsubscribe();

    this.collapse();

    this.active.set(null);
    this.hits.set([]);
    this.page.set(1);
    this.totalPages.set(0);
    this.totalCount.set(0);
    this.loading.set(false);
    this.error.set(null);
  }


  /**
   * Load a page of results from API.
   */
  private load(): void {

    const query = this.active();

    if (!query) {
      return;
    }

    this.request?.unsubscribe();

    this.collapse();

    this.loading.set(true);
    this.error.set(null);


    this.request = this.api
      .search(query, this.page())

      .subscribe({

        next: (result) => {

          this.hits.set(result.items);

          this.totalPages.set(result.totalPages);

          this.totalCount.set(result.totalCount);

          this.loading.set(false);
        },


        error: (response: HttpErrorResponse) => {

          this.hits.set([]);
          this.totalPages.set(0);
          this.totalCount.set(0);

          this.error.set(
            this.messageFor(response),
          );

          this.loading.set(false);
        },

      });
  }


  private messageFor(
    response: HttpErrorResponse,
  ): string {

    switch (response.status) {

      case 503:
        return 'Поиск временно недоступен. Попробуйте позже.';

      case 400:
        return 'Введите запрос длиной до 200 символов.';

      default:
        return 'Не удалось выполнить поиск.';
    }
  }
}