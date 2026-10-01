import { DatePipe, DecimalPipe } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';

import { CommentDto } from '../../../core/models';
import { CommentComposer } from '../comment-composer/comment-composer';

@Component({
  selector: 'app-reply-tree',
  imports: [
    DatePipe,
    DecimalPipe,
    CommentComposer,
  ],
  templateUrl: './reply-tree.html',
  styleUrl: './reply-tree.scss',
})
export class ReplyTree {
  @Input() replies: CommentDto[] = [];
  @Input() depth = 1;
  @Input() replyingTo: string | null = null;

  @Output() reply = new EventEmitter<string>();
  @Output() created = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  readonly maxIndentDepth = 5;

  avatarFor(comment: CommentDto): string {
    return comment.userName.trim().charAt(0).toUpperCase();
  }

  onReply(commentId: string): void {
    this.reply.emit(commentId);
  }

  onCreated(): void {
    this.created.emit();
  }

  onCancelled(): void {
    this.cancelled.emit();
  }
}