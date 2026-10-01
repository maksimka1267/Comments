import { Component } from '@angular/core';
import { CommentList } from './features/comments/comment-list/comment-list';

@Component({
  selector: 'app-root',
  imports: [CommentList],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}