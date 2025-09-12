import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from './shared/header/header.component';
import { FooterComponent } from './shared/footer/footer.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, HeaderComponent, FooterComponent],
  // Usa el nombre REAL de tu template. Si tu archivo es "app.html", usa './app.html'.
  templateUrl: './app.component.html',
  styleUrls: ['./app.scss'],
})
export class AppComponent {}

