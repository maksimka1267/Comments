import { DecimalPipe } from '@angular/common';
import {
  Component,
  HostListener,
  Input,
} from '@angular/core';

@Component({
  selector: 'app-image-lightbox',
  imports: [DecimalPipe],
  templateUrl: './image-lightbox.html',
  styleUrl: './image-lightbox.scss',
})
export class ImageLightbox {
  @Input() src = '';
  @Input() alt = '';

  protected opened = false;
  protected zoom = 1;

  protected open(): void {
    this.opened = true;
    this.zoom = 1;
    document.body.style.overflow = 'hidden';
  }

  protected close(): void {
    this.opened = false;
    this.zoom = 1;
    document.body.style.overflow = '';
  }

  protected zoomIn(): void {
    this.zoom = Math.min(this.zoom + 0.25, 3);
  }

  protected zoomOut(): void {
    this.zoom = Math.max(this.zoom - 0.25, 0.5);
  }

  protected resetZoom(): void {
    this.zoom = 1;
  }

  protected onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    if (this.opened) {
      this.close();
    }
  }
}