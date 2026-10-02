import {
  Component,
  ElementRef,
  EventEmitter,
  OnInit,
  Output,
  ViewChild,
  inject,
  input,
  signal,
} from '@angular/core';

import { FormsModule } from '@angular/forms';

import { CommentsApi } from '../../../core/comments-api';

@Component({
  selector: 'app-comment-composer',
  imports: [FormsModule],
  templateUrl: './comment-composer.html',
  styleUrl: './comment-composer.scss',
})
export class CommentComposer implements OnInit {

  private readonly api = inject(CommentsApi);

  readonly parentId =
    input<string | null>(null);


  @Output()
  readonly created =
    new EventEmitter<void>();

  @Output()
  readonly cancelled =
    new EventEmitter<void>();


  @ViewChild('textArea')
  private textArea?: ElementRef<HTMLTextAreaElement>;


  protected userName = '';
  protected email = '';
  protected homePage = '';
  protected text = '';
  protected captchaAnswer = '';


  protected file: File | null = null;


  protected captchaId =
    signal<string | null>(null);

  protected captchaImage =
    signal<string | null>(null);


  protected loadingCaptcha =
    signal(false);

  protected submitting =
    signal(false);

  protected error =
    signal<string | null>(null);


  protected preview =
    signal(false);


  ngOnInit(): void {
    this.loadCaptcha();
  }


  protected loadCaptcha(): void {

    this.loadingCaptcha.set(true);

    this.api.getCaptcha().subscribe({

      next: (captcha) => {

        this.captchaId.set(captcha.id);
        this.captchaImage.set(captcha.image);

        this.captchaAnswer = '';

        this.loadingCaptcha.set(false);
      },

      error: () => {

        this.error.set(
          'Не удалось загрузить CAPTCHA.',
        );

        this.loadingCaptcha.set(false);
      },

    });
  }


  protected togglePreview(): void {
    this.preview.update(
      value => !value,
    );
  }


  protected insertTag(
    tag: 'i' | 'strong' | 'code',
  ): void {

    const textarea =
      this.textArea?.nativeElement;

    if (!textarea) {
      return;
    }


    const start =
      textarea.selectionStart;

    const end =
      textarea.selectionEnd;


    const selected =
      this.text.substring(start, end);


    const openTag =
      `<${tag}>`;

    const closeTag =
      `</${tag}>`;


    const replacement =
      selected
        ? `${openTag}${selected}${closeTag}`
        : `${openTag}${closeTag}`;


    this.text =
      this.text.substring(0, start) +
      replacement +
      this.text.substring(end);


    requestAnimationFrame(() => {

      textarea.focus();

      const cursorPosition =
        selected
          ? start + replacement.length
          : start + openTag.length;

      textarea.setSelectionRange(
        cursorPosition,
        cursorPosition,
      );

    });
  }


  protected insertLink(): void {

    const textarea =
      this.textArea?.nativeElement;

    if (!textarea) {
      return;
    }


    const start =
      textarea.selectionStart;

    const end =
      textarea.selectionEnd;


    const selected =
      this.text.substring(start, end);


    const url =
      window.prompt(
        'Введите URL:',
        'https://',
      );


    if (!url) {
      return;
    }


    const label =
      selected || 'ссылка';


    const replacement =
      `<a href="${url}" target="_blank" rel="noopener noreferrer">${label}</a>`;


    this.text =
      this.text.substring(0, start) +
      replacement +
      this.text.substring(end);


    requestAnimationFrame(() => {

      textarea.focus();

      const cursorPosition =
        start + replacement.length;

      textarea.setSelectionRange(
        cursorPosition,
        cursorPosition,
      );

    });
  }


  protected onFileSelected(
    event: Event,
  ): void {

    const input =
      event.target as HTMLInputElement;

    this.file =
      input.files?.[0] ?? null;
  }


  protected submit(): void {

    this.error.set(null);


    if (!this.userName.trim()) {
      this.error.set('Введите имя.');
      return;
    }


    if (!this.email.trim()) {
      this.error.set('Введите E-mail.');
      return;
    }


    if (!this.text.trim()) {
      this.error.set(
        'Введите текст комментария.',
      );

      return;
    }


    const captchaId =
      this.captchaId();


    if (!captchaId) {
      this.error.set(
        'CAPTCHA ещё не загружена.',
      );

      return;
    }


    if (!this.captchaAnswer.trim()) {
      this.error.set(
        'Введите CAPTCHA.',
      );

      return;
    }


    this.submitting.set(true);


    this.api.create(

      this.userName.trim(),

      this.email.trim(),

      this.homePage.trim() || null,

      this.text.trim(),

      this.parentId(),

      captchaId,

      this.captchaAnswer.trim(),

      this.file,

    ).subscribe({

      next: () => {

        this.submitting.set(false);

        this.reset();

        this.created.emit();
      },


      error: (error) => {

        this.submitting.set(false);


        const message =
          error?.error?.errors
            ?.CaptchaAnswer?.[0] ??

          error?.error?.errors
            ?.Text?.[0] ??

          error?.error?.title ??

          'Не удалось отправить комментарий.';


        this.error.set(message);

        this.loadCaptcha();
      },

    });
  }


  protected cancel(): void {
    this.cancelled.emit();
  }


  private reset(): void {

    this.userName = '';
    this.email = '';
    this.homePage = '';
    this.text = '';
    this.captchaAnswer = '';

    this.file = null;

    this.preview.set(false);
  }
}