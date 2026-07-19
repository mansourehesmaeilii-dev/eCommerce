import { Component } from '@angular/core';

interface FeaturedCollection
{
    title: string;
    imageUrl: string;
    imageAlt: string;
}

@Component({
    selector: 'app-ecommerce-product-componet',
    templateUrl: './ecommerce-product-componet.component.html',
    styleUrl: './ecommerce-product-componet.component.css',
    standalone: false
})
export class ECommerceProductComponetComponent
{
    public readonly featuredCollections: FeaturedCollection[] = [
        {
            title: 'New Arrivals',
            imageUrl:
                'https://images.unsplash.com/photo-1601924994987-69e26d50dc26?auto=format&fit=crop&w=1000&q=80',
            imageAlt: 'Folded neutral knitwear on a clean background'
        },
        {
            title: 'Best Sellers',
            imageUrl:
                'https://images.unsplash.com/photo-1524592094714-0f0654e20314?auto=format&fit=crop&w=1000&q=80',
            imageAlt: 'Minimal watch with leather band on fabric'
        },
        {
            title: 'Summer Essentials',
            imageUrl:
                'https://images.unsplash.com/photo-1511499767150-a48a237f0083?auto=format&fit=crop&w=1000&q=80',
            imageAlt: 'Round sunglasses resting on light fabric'
        }
    ];
}
