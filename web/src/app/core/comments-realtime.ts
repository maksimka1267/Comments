import { Injectable, OnDestroy } from '@angular/core';

import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

import { Observable, Subject } from 'rxjs';

/** То, что сервер присылает при создании комментария (только идентификаторы). */
export interface CommentCreatedNotification {
  commentId: string;
  parentId: string | null;
}

const MAX_DELAY_MS = 30_000;

/** Задержка перед повтором: 1, 2, 4, ... секунд, но не больше 30. */
const backoff = (attempt: number): number =>
  Math.min(MAX_DELAY_MS, 1_000 * 2 ** attempt);

@Injectable({
  providedIn: 'root',
})
export class CommentsRealtime implements OnDestroy {
  private readonly created = new Subject<CommentCreatedNotification>();
  private readonly resync = new Subject<void>();

  private connection?: HubConnection;
  private retryTimer?: ReturnType<typeof setTimeout>;
  private stopped = false;

  /** Новый комментарий или ответ, созданный кем угодно. */
  readonly commentCreated$: Observable<CommentCreatedNotification> =
    this.created.asObservable();

  /** Связь восстановилась: за время обрыва события могли потеряться. */
  readonly resync$: Observable<void> = this.resync.asObservable();

  start(): void {
    if (this.connection) {
      return;
    }

    this.stopped = false;

    this.connection = new HubConnectionBuilder()
      .withUrl('/hubs/comments')
      // повторяем бесконечно: сервер может перезапускаться
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (context) =>
          backoff(context.previousRetryCount),
      })
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on(
      'CommentCreated',
      (notification: CommentCreatedNotification) =>
        this.created.next(notification),
    );

    this.connection.onreconnected(() => this.resync.next());

    void this.connect(0);
  }

  stop(): void {
    this.stopped = true;
    clearTimeout(this.retryTimer);

    void this.connection?.stop();
    this.connection = undefined;
  }

  ngOnDestroy(): void {
    this.stop();
  }

  /** Автопереподключение не покрывает неудачный первый запуск, поэтому повторяем сами. */
  private async connect(attempt: number): Promise<void> {
    if (this.stopped || !this.connection) {
      return;
    }

    try {
      await this.connection.start();

      if (attempt > 0) {
        this.resync.next();
      }
    } catch {
      if (this.stopped) {
        return;
      }

      this.retryTimer = setTimeout(
        () => void this.connect(attempt + 1),
        backoff(attempt),
      );
    }
  }
}