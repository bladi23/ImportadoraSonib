import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CartService } from '../../../core/cart.service';

@Component({
  standalone: true,
  selector: 'app-checkout-success',
  imports: [CommonModule, RouterLink],
  templateUrl: './checkout-success.component.html',
  styleUrls: ['./checkout-success.component.scss']
})
export class CheckoutSuccessComponent {
  private cart = inject(CartService);
  private route = inject(ActivatedRoute);

  orderId?: number;

  ngOnInit() {
    this.orderId = Number(this.route.snapshot.queryParamMap.get('orderId'));
    // sincroniza el contador del header (el backend ya vació el carrito)
    this.cart.refresh();
  }
}
