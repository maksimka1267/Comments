import {
  DatePipe,
  DecimalPipe,
} from '@angular/common';
import { ImageLightbox } from '../image-lightbox/image-lightbox';
import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';

import { Subscription } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  CommentCreatedNotification,
  CommentsRealtime,
} from '../../../core/comments-realtime';
import { CommentsApi } from '../../../core/comments-api';

import {
  CommentDto,
  SortDir,
  SortField,
} from '../../../core/models';

import { visiblePages } from './pagination';

import { ReplyTree } from '../reply-tree/reply-tree';
import { CommentComposer } from '../comment-composer/comment-composer';


@Component({
  selector: 'app-comment-list',

  imports: [
  DatePipe,
  DecimalPipe,
  CommentComposer,
  ReplyTree,
  ImageLightbox,
],

  templateUrl: './comment-list.html',
  styleUrl: './comment-list.scss',
})
export class CommentList implements OnInit, OnDestroy {

  private readonly api = inject(CommentsApi);
  private readonly realtime = inject(CommentsRealtime);
  private request?: Subscription;


  // Comments
  protected readonly comments =
    signal<CommentDto[]>([]);


  // ID comment/reply that we are replying to
  protected readonly replyingTo =
    signal<string | null>(null);


  // Sorting
  protected readonly sortBy =
    signal<SortField>('date');

  protected readonly sortDir =
    signal<SortDir>('desc');


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

  // Real-time updates
  protected readonly newCommentIds =
    signal<ReadonlySet<string>>(new Set());

  protected readonly missedUpdates =
    signal(false);

  protected readonly hasUpdates =
    computed(() =>
      this.newCommentIds().size > 0 ||
      this.missedUpdates(),
    );

  // ID всех комментариев и ответов, которые сейчас на экране
  private readonly visibleIds =
    computed(() => {
      const ids = new Set<string>();

      const walk = (items: CommentDto[]): void => {
        for (const item of items) {
          ids.add(item.id);
          walk(item.replies);
        }
      };

      walk(this.comments());

      return ids;
    });
  // Pagination buttons
  protected readonly pages =
    computed(() =>
      visiblePages(
        this.page(),
        this.totalPages(),
      ),
    );


    constructor() {
    this.realtime.commentCreated$
      .pipe(takeUntilDestroyed())
      .subscribe((notification) =>
        this.onRemoteComment(notification),
      );

    this.realtime.resync$
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.missedUpdates.set(true));
  }


  ngOnInit(): void {
    this.load();
    this.realtime.start();
  }


  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.realtime.stop();
  }


  /**
   * Start replying to a comment/reply.
   */
  protected startReply(id: string): void {
    this.replyingTo.set(id);
  }


  /**
   * Cancel current reply.
   */
  protected cancelReply(): void {
    this.replyingTo.set(null);
  }


  /**
   * Comment/reply was successfully created.
   */
  protected commentCreated(): void {
    this.replyingTo.set(null);
    this.load();
  }
  /**
   * Reload the current page after the user clicked the "new comments" banner.
   */
  protected refresh(): void {
    this.load();
  }


  /**
   * Someone created a comment: remember it, but do not touch the list.
   */
  private onRemoteComment(
    notification: CommentCreatedNotification,
  ): void {

    const known = this.visibleIds();

    // уже на экране (например, свой комментарий)
    if (known.has(notification.commentId)) {
      return;
    }

    // ответ интересен, только если его родитель сейчас виден
    if (
      notification.parentId &&
      !known.has(notification.parentId)
    ) {
      return;
    }

    this.newCommentIds.update((ids) =>
      new Set(ids).add(notification.commentId),
    );
  }

  /**
   * Change sorting.
   */
  protected sort(field: SortField): void {

    if (this.sortBy() === field) {

      this.sortDir.update((dir) =>
        dir === 'asc'
          ? 'desc'
          : 'asc',
      );

    } else {

      this.sortBy.set(field);

      this.sortDir.set(
        field === 'date'
          ? 'desc'
          : 'asc',
      );
    }

    this.page.set(1);

    this.load();
  }


  /**
   * Change page.
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
   * Sorting indicator.
   */
  protected indicator(
    field: SortField,
  ): string {

    if (this.sortBy() !== field) {
      return '';
    }

    return this.sortDir() === 'asc'
      ? '▲'
      : '▼';
  }


  /**
   * Generate avatar.
   */
  protected avatarFor(
    comment: CommentDto,
  ): string {

    const avatars = [
      '👨🏻',
      '👩🏻',
      '👨🏼',
      '👩🏼',
      '👨🏽',
      '👩🏽',
    ];

    const value = String(comment.id);

    let hash = 0;

    for (
      let i = 0;
      i < value.length;
      i++
    ) {

      hash =
        value.charCodeAt(i) +
        ((hash << 5) - hash);
    }

    return avatars[
      Math.abs(hash) % avatars.length
    ];
  }


  /**
   * Load comments from API.
   */
  private load(): void {

    this.request?.unsubscribe();

    this.loading.set(true);
    this.error.set(null);
    this.newCommentIds.set(new Set());
    this.missedUpdates.set(false);

    this.request = this.api
      .list(
        this.sortBy(),
        this.sortDir(),
        this.page(),
      )

      .subscribe({

        next: (result) => {

          this.comments.set(
            result.items,
          );

          this.totalPages.set(
            result.totalPages,
          );

          this.totalCount.set(
            result.totalCount,
          );
          const known = this.visibleIds();

          this.newCommentIds.update((ids) =>
            new Set([...ids].filter((id) => !known.has(id))),
          );
          this.loading.set(false);
        },


        error: () => {

          this.error.set(
            'Не удалось загрузить комментарии.',
          );

          this.loading.set(false);
        },

      });
  }
}