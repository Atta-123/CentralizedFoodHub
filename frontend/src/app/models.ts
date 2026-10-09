export interface Auth {
  token: string;
  userId: string;
  name: string;
  role: "Admin" | "Customer" | "Restaurant";
}
export interface Restaurant {
  latitude: number | null;
  longitude: number | null;
  deliveryRadiusKm: number;
  distanceKm: number | null;
  id: string;
  name: string;
  cuisine: string;
  address: string;
  commissionPercent: number;
  isOpen: boolean;
  ownerEmail: string | null;
}
export interface MenuItem {
  id: string;
  restaurantId: string;
  name: string;
  description: string;
  price: number;
  isAvailable: boolean;
}
export interface CartLine {
  item: MenuItem;
  quantity: number;
}
export interface Order {
  id: string;
  restaurantId: string;
  restaurantName: string;
  customerName: string;
  address: string;
  phone: string;
  subtotal: number;
  commissionPercent: number;
  commission: number;
  restaurantPayable: number;
  status: string;
  createdAt: string;
  version: string;
  items: { name: string; unitPrice: number; quantity: number }[];
  history: { status: string; changedAt: string }[];
}
export interface Page<T> {
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  items: T[];
}
export interface Summary {
  restaurants: number;
  orders: number;
  deliveredOrders: number;
  deliveredSales: number;
  commissionRevenue: number;
  restaurantPayable: number;
}
export interface Settlement {
  restaurantId: string;
  restaurantName: string;
  deliveredOrders: number;
  sales: number;
  commission: number;
  payable: number;
}
