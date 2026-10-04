import { Component } from '@angular/core';
import { CommentList } from './features/comments/comment-list/comment-list';
import { CommentSearch } from './features/comments/comment-search/comment-search';

@Component({
  selector: 'app-root',
  imports: [CommentSearch, CommentList],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {}