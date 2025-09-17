import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService, Category, ProductListItem } from '../../../core/api.service';

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './products.component.html',
  styleUrls: ['./products.component.scss']
})
export class ProductsComponent implements OnInit {
  categories: Category[] = [];
  selected = '';
  items: ProductListItem[] = [];
  total = 0; page = 1; pageSize = 12; search = '';
  loading = false;

  constructor(private api: ApiService) {}

  ngOnInit() {
    this.api.getCategories().subscribe(c => this.categories = c);
    this.load();
  }

  load() {
    this.loading = true;
    this.api.getProducts({
      category: this.selected || undefined,
      page: this.page, pageSize: this.pageSize,
      search: this.search || undefined
    }).subscribe(res => { this.items = res.items; this.total = res.total; this.loading = false; });
  }
}
