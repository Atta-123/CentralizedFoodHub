import { Component, DestroyRef, computed, inject, signal } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { HttpErrorResponse } from "@angular/common/http";
import {
  Observable,
  Subject,
  catchError,
  debounceTime,
  finalize,
  of,
  switchMap,
} from "rxjs";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { ApiService } from "./api.service";
import {
  CartLine,
  MenuItem,
  Order,
  Restaurant,
  Settlement,
  Summary,
} from "./models";
@Component({
  selector: "app-root",
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.scss",
})
export class AppComponent {
  api = inject(ApiService);
  private destroy = inject(DestroyRef);
  view = signal("restaurants");
  busy = signal(false);
  error = signal("");
  notice = signal("");
  restaurants = signal<Restaurant[]>([]);
  restaurantTotal = signal(0);
  restaurantPage = signal(1);
  selected = signal<Restaurant | null>(null);
  menu = signal<MenuItem[]>([]);
  orders = signal<Order[]>([]);
  orderTotal = signal(0);
  orderPage = signal(1);
  summary = signal<Summary | null>(null);
  settlements = signal<Settlement[]>([]);
  cart = signal<CartLine[]>(this.restoreCart());
  subtotal = computed(() =>
    this.cart().reduce((sum, l) => sum + l.item.price * l.quantity, 0),
  );
  location = signal<{
    latitude: number;
    longitude: number;
    radiusKm: number;
  } | null>(this.restoreLocation());
  locating = signal(false);
  latitude: number | null = this.location()?.latitude ?? null;
  longitude: number | null = this.location()?.longitude ?? null;
  radiusKm = this.location()?.radiusKm ?? 5;
  search = "";
  email = "";
  password = "";
  name = "";
  registerMode = false;
  address = "";
  phone = "";
  showRestaurantForm = false;
  restaurantForm = {
    name: "",
    cuisine: "",
    address: "",
    commissionPercent: 20,
    ownerEmail: "",
    ownerPassword: "",
    latitude: null as number | null,
    longitude: null as number | null,
    deliveryRadiusKm: 5,
  };
  menuForm = { name: "", description: "", price: 500, isAvailable: true };
  editItemId: string | null = null;
  showMenuForm = false;
  private searchChanges = new Subject<string>();
  private restaurantLoads = new Subject<{ search: string; page: number }>();
  private checkoutKey: string | null = null;
  private checkoutPayload = "";
  constructor() {
    this.searchChanges
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroy))
      .subscribe(() => this.loadRestaurants());
    this.restaurantLoads
      .pipe(
        switchMap((request) =>
          this.restaurantRequest(request.search, request.page).pipe(
            catchError((e) => {
              this.fail(e);
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe((p) => {
        if (!p) return;
        this.restaurants.set(p.items);
        this.restaurantTotal.set(p.totalCount);
        this.restaurantPage.set(p.pageNumber);
      });
    if (this.api.user()) this.loadRestaurants();
  }
  private restoreCart(): CartLine[] {
    try {
      return JSON.parse(sessionStorage.getItem("foodhub.cart") || "[]");
    } catch {
      return [];
    }
  }
  private persist() {
    sessionStorage.setItem("foodhub.cart", JSON.stringify(this.cart()));
  }
  private fail(e: HttpErrorResponse) {
    let text = e.error?.message;
    if (!text && e.error?.errors)
      text = Object.values(e.error.errors).flat().join(" ");
    this.error.set(
      text ||
        (e.status === 0
          ? "API is unreachable. Start the backend on port 5180."
          : e.status === 401
            ? "Session expired. Sign out and sign in again."
            : "Request failed. Please retry."),
    );
  }
  private run<T>(request: Observable<T>, done: (data: T) => void) {
    if (this.busy()) return;
    this.error.set("");
    this.notice.set("");
    this.busy.set(true);
    request
      .pipe(
        finalize(() => this.busy.set(false)),
        takeUntilDestroyed(this.destroy),
      )
      .subscribe({ next: done, error: (e) => this.fail(e) });
  }
  auth() {
    const request = this.registerMode
      ? this.api.register(this.name, this.email, this.password)
      : this.api.login(this.email, this.password);
    this.run(request, () => {
      this.password = "";
      this.cart.set([]);
      this.persist();
      this.loadRestaurants();
    });
  }
  demo(email: string) {
    this.email = email;
    this.password = "Demo@12345";
    this.registerMode = false;
  }
  logout() {
    this.api.logout();
    this.location.set(null);
    this.latitude = null;
    this.longitude = null;
    this.radiusKm = 5;
    sessionStorage.removeItem("foodhub.location");
    this.restaurantLoads.next({ search: "", page: 1 });
    this.cart.set([]);
    this.selected.set(null);
    this.orders.set([]);
    this.restaurants.set([]);
    this.summary.set(null);
    this.view.set("restaurants");
    this.error.set("");
    this.notice.set("");
    this.search = "";
    this.restaurantPage.set(1);
  }
  navigate(view: string) {
    this.view.set(view);
    this.error.set("");
    this.notice.set("");
    if (view === "restaurants") this.loadRestaurants();
    if (view === "orders") this.loadOrders();
    if (view === "dashboard")
      this.api.summary().subscribe({
        next: (x) => this.summary.set(x),
        error: (e) => this.fail(e),
      });
    if (view === "settlements")
      this.api.settlements().subscribe({
        next: (x) => this.settlements.set(x),
        error: (e) => this.fail(e),
      });
  }
  loadRestaurants(page = 1) {
    this.restaurantLoads.next({ search: this.search, page });
  }
  private restaurantRequest(search: string, page = 1) {
    const loc = this.location();
    return this.api.user()?.role === "Customer" && loc
      ? this.api.nearby(loc.latitude, loc.longitude, loc.radiusKm, search, page)
      : this.api.restaurants(search, page);
  }
  private restoreLocation(): {
    latitude: number;
    longitude: number;
    radiusKm: number;
  } | null {
    try {
      const value = JSON.parse(
        sessionStorage.getItem("foodhub.location") || "null",
      );
      return value &&
        Number.isFinite(value.latitude) &&
        Number.isFinite(value.longitude) &&
        Math.abs(value.latitude) <= 90 &&
        Math.abs(value.longitude) <= 180
        ? {
            ...value,
            radiusKm:
              Number.isFinite(value.radiusKm) &&
              value.radiusKm >= 0.1 &&
              value.radiusKm <= 50
                ? value.radiusKm
                : 5,
          }
        : null;
    } catch {
      return null;
    }
  }
  applyLocation() {
    if (
      this.latitude === null ||
      this.longitude === null ||
      !Number.isFinite(this.latitude) ||
      !Number.isFinite(this.longitude) ||
      Math.abs(this.latitude) > 90 ||
      Math.abs(this.longitude) > 180 ||
      !Number.isFinite(this.radiusKm) ||
      this.radiusKm < 0.1 ||
      this.radiusKm > 50
    ) {
      this.error.set(
        "Enter valid coordinates and a radius between 0.1 and 50 km.",
      );
      return;
    }
    this.location.set({
      latitude: this.latitude,
      longitude: this.longitude,
      radiusKm: this.radiusKm,
    });
    sessionStorage.setItem("foodhub.location", JSON.stringify(this.location()));
    this.error.set("");
    this.selected.set(null);
    this.loadRestaurants();
  }
  useCurrentLocation() {
    if (!navigator.geolocation) {
      this.error.set("Location is unavailable. Enter coordinates manually.");
      return;
    }
    const userId = this.api.user()?.userId;
    this.locating.set(true);
    navigator.geolocation.getCurrentPosition(
      (position) => {
        this.locating.set(false);
        if (this.api.user()?.userId !== userId) return;
        this.latitude = position.coords.latitude;
        this.longitude = position.coords.longitude;
        this.applyLocation();
      },
      (error) => {
        this.locating.set(false);
        if (this.api.user()?.userId !== userId) return;
        this.error.set(
          error.code === 1
            ? "Location permission denied. Enter delivery coordinates manually."
            : "Could not determine your location. Enter coordinates manually or retry.",
        );
      },
      { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 },
    );
  }
  onSearch() {
    this.searchChanges.next(this.search);
  }
  selectRestaurant(r: Restaurant) {
    if (this.cart().length && this.cart()[0].item.restaurantId !== r.id) {
      this.error.set(
        "Your cart belongs to another restaurant. Clear it before switching.",
      );
      return;
    }
    this.selected.set(r);
    this.showMenuForm = false;
    this.api
      .menu(r.id)
      .subscribe({ next: (x) => this.menu.set(x), error: (e) => this.fail(e) });
  }
  closeMenu() {
    this.selected.set(null);
  }
  createRestaurant() {
    this.run(this.api.createRestaurant(this.restaurantForm), () => {
      this.showRestaurantForm = false;
      this.notice.set("Restaurant and owner account created.");
      this.loadRestaurants();
      this.restaurantForm = {
        name: "",
        cuisine: "",
        address: "",
        commissionPercent: 20,
        ownerEmail: "",
        ownerPassword: "",
        latitude: null as number | null,
        longitude: null as number | null,
        deliveryRadiusKm: 5,
      };
    });
  }
  saveSettings(r: Restaurant) {
    this.run(this.api.settings(r), (updated) => {
      this.restaurants.update((rows) =>
        rows.map((x) => (x.id === updated.id ? updated : x)),
      );
      this.notice.set(
        "Settings saved. Existing orders keep their original commission.",
      );
    });
  }
  editMenu(item?: MenuItem) {
    this.editItemId = item?.id ?? null;
    this.menuForm = item
      ? {
          name: item.name,
          description: item.description,
          price: item.price,
          isAvailable: item.isAvailable,
        }
      : { name: "", description: "", price: 500, isAvailable: true };
    this.showMenuForm = true;
  }
  saveMenu() {
    const r = this.selected();
    if (!r) return;
    this.run(this.api.saveMenu(r.id, this.editItemId, this.menuForm), () => {
      this.showMenuForm = false;
      this.notice.set("Menu saved.");
      this.selectRestaurant(r);
    });
  }
  add(item: MenuItem) {
    if (
      this.cart().length &&
      this.cart()[0].item.restaurantId !== item.restaurantId
    ) {
      this.error.set("Clear the other restaurant cart first.");
      return;
    }
    const old = this.cart().find((x) => x.item.id === item.id);
    if (old && old.quantity >= 20) return;
    if (!old && this.cart().length >= 30) {
      this.error.set("Maximum 30 different items per order.");
      return;
    }
    this.cart.update((lines) =>
      old
        ? lines.map((l) =>
            l.item.id === item.id ? { ...l, quantity: l.quantity + 1 } : l,
          )
        : [...lines, { item, quantity: 1 }],
    );
    this.persist();
  }
  remove(id: string) {
    this.cart.update((lines) =>
      lines.flatMap((l) =>
        l.item.id !== id
          ? [l]
          : l.quantity > 1
            ? [{ ...l, quantity: l.quantity - 1 }]
            : [],
      ),
    );
    this.persist();
  }
  clearCart() {
    this.cart.set([]);
    this.persist();
    this.checkoutKey = null;
    this.checkoutPayload = "";
  }
  checkout() {
    if (!this.cart().length) return;
    if (!this.location()) {
      this.error.set("Select your delivery location before checkout.");
      return;
    }
    const payload = {
      restaurantId: this.cart()[0].item.restaurantId,
      address: this.address,
      deliveryLatitude: this.location()!.latitude,
      deliveryLongitude: this.location()!.longitude,
      phone: this.phone,
      items: this.cart().map((l) => ({
        menuItemId: l.item.id,
        quantity: l.quantity,
      })),
    };
    const encoded = JSON.stringify(payload);
    if (encoded !== this.checkoutPayload || !this.checkoutKey) {
      this.checkoutKey = crypto.randomUUID();
      this.checkoutPayload = encoded;
    }
    this.run(this.api.place(payload, this.checkoutKey), () => {
      this.clearCart();
      this.view.set("orders");
      this.loadOrders();
      this.notice.set("Order placed. Your restaurant will now see it.");
    });
  }
  loadOrders(page = this.orderPage()) {
    this.api.orders(page).subscribe({
      next: (p) => {
        this.orders.set(p.items);
        this.orderTotal.set(p.totalCount);
        this.orderPage.set(p.pageNumber);
      },
      error: (e) => this.fail(e),
    });
  }
  actions(o: Order): string[] {
    const role = this.api.user()?.role;
    if (role === "Customer")
      return o.status === "PendingAcceptance" ? ["Cancelled"] : [];
    if (o.status === "PendingAcceptance") return ["Accepted", "Rejected"];
    if (o.status === "Accepted") return ["Preparing"];
    if (o.status === "Preparing") return ["ReadyForPickup"];
    if (o.status === "ReadyForPickup" && role === "Admin") return ["Delivered"];
    return [];
  }
  changeStatus(o: Order, status: string) {
    this.run(this.api.status(o, status), (updated) => {
      this.orders.update((rows) =>
        rows.map((x) => (x.id === updated.id ? updated : x)),
      );
      this.notice.set("Order status updated.");
    });
  }
  money(value: number) {
    return "Rs. " + value.toLocaleString("en-PK", { maximumFractionDigits: 2 });
  }
  short(id: string) {
    return id.slice(0, 8).toUpperCase();
  }
  label(status: string) {
    return status.replace(/([a-z])([A-Z])/g, "$1 $2");
  }
}
