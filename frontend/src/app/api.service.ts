import { Injectable, inject, signal } from "@angular/core";
import { HttpClient, HttpInterceptorFn } from "@angular/common/http";
import { tap } from "rxjs";
import {
  Auth,
  MenuItem,
  Order,
  Page,
  Restaurant,
  Settlement,
  Summary,
} from "./models";
// Change this address only if the API uses a different port.
export const API_BASE = "http://localhost:5180/api";
@Injectable({ providedIn: "root" })
export class ApiService {
  private http = inject(HttpClient);
  user = signal<Auth | null>(this.restore());
  private restore(): Auth | null {
    try {
      return JSON.parse(sessionStorage.getItem("foodhub.auth") || "null");
    } catch {
      return null;
    }
  }
  login(email: string, password: string) {
    return this.http
      .post<Auth>(`${API_BASE}/auth/login`, { email, password })
      .pipe(tap((x) => this.setUser(x)));
  }
  register(name: string, email: string, password: string) {
    return this.http
      .post<Auth>(`${API_BASE}/auth/register`, { name, email, password })
      .pipe(tap((x) => this.setUser(x)));
  }
  private setUser(user: Auth) {
    sessionStorage.setItem("foodhub.auth", JSON.stringify(user));
    this.user.set(user);
  }
  logout() {
    sessionStorage.removeItem("foodhub.auth");
    sessionStorage.removeItem("foodhub.cart");
    this.user.set(null);
  }
  restaurants(search = "", page = 1) {
    return this.http.get<Page<Restaurant>>(`${API_BASE}/restaurants`, {
      params: { search, page, pageSize: 12 },
    });
  }
  nearby(
    latitude: number,
    longitude: number,
    radiusKm: number,
    search = "",
    page = 1,
  ) {
    return this.http.get<Page<Restaurant>>(`${API_BASE}/restaurants/nearby`, {
      params: { latitude, longitude, radiusKm, search, page, pageSize: 12 },
    });
  }
  createRestaurant(input: unknown) {
    return this.http.post<Restaurant>(`${API_BASE}/restaurants`, input);
  }
  settings(r: Restaurant) {
    return this.http.patch<Restaurant>(`${API_BASE}/restaurants/${r.id}`, {
      commissionPercent: r.commissionPercent,
      isOpen: r.isOpen,
      latitude: r.latitude,
      longitude: r.longitude,
      deliveryRadiusKm: r.deliveryRadiusKm,
    });
  }
  menu(id: string) {
    return this.http.get<MenuItem[]>(`${API_BASE}/restaurants/${id}/menu`);
  }
  saveMenu(id: string, itemId: string | null, input: unknown) {
    return itemId
      ? this.http.put<MenuItem>(
          `${API_BASE}/restaurants/${id}/menu/${itemId}`,
          input,
        )
      : this.http.post<MenuItem>(`${API_BASE}/restaurants/${id}/menu`, input);
  }
  orders(page = 1) {
    return this.http.get<Page<Order>>(`${API_BASE}/orders`, {
      params: { page, pageSize: 12 },
    });
  }
  place(input: unknown, key: string) {
    return this.http.post<Order>(`${API_BASE}/orders`, input, {
      headers: { "Idempotency-Key": key },
    });
  }
  status(o: Order, status: string) {
    return this.http.patch<Order>(`${API_BASE}/orders/${o.id}/status`, {
      status,
      version: o.version,
    });
  }
  summary() {
    return this.http.get<Summary>(`${API_BASE}/admin/summary`);
  }
  settlements() {
    return this.http.get<Settlement[]>(`${API_BASE}/settlements`);
  }
}
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const user = inject(ApiService).user();
  return next(
    user && request.url.startsWith(API_BASE + "/")
      ? request.clone({ setHeaders: { Authorization: `Bearer ${user.token}` } })
      : request,
  );
};
