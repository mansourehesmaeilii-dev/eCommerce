import { Component, Input } from '@angular/core';

interface FeaturedCollection {
  title: string;
  imageUrl: string;
  imageAlt: string;
}

@Component({
  selector: 'app-featured-collections',
  templateUrl: './featured-collections.component.html',
  styleUrl: './featured-collections.component.css',
  standalone: false
})
export class FeaturedCollectionsComponent {
  @Input() collections: FeaturedCollection[] = [];
}
