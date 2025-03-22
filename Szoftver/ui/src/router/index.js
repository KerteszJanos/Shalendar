import { createRouter, createWebHistory } from 'vue-router';
import Login from '../views/Login.vue';
import Register from '../views/Register.vue';
import Dashboard from '../views/Dashboard.vue';
import Profile from '../views/Profile.vue';
import CalendarView from '../components/organisms/CalendarView.vue';
import DayView from '../views/DayView.vue';
import Calendars from '../views/Calendars.vue';
import { jwtDecode } from "jwt-decode";

// Vue Router setup with route definitions and authentication guard logic

// Define route paths and assign authentication requirements via meta fields
const routes = [
  { path: '/', redirect: '/login' },
  { path: '/login', component: Login, meta: { guest: true } },
  { path: '/register', component: Register, meta: { guest: true } },
  { path: '/dashboard', component: Dashboard, meta: { requiresAuth: true } },
  { path: '/profile', component: Profile, meta: { requiresAuth: true } },
  { path: '/calendar', component: CalendarView, meta: { requiresAuth: true } },
  { path: '/day/:date', component: DayView, props: true, meta: { requiresAuth: true } },
  { path: '/calendars', component: Calendars, props: true, meta: { requiresAuth: true } },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

// Global navigation guard to protect routes based on authentication
router.beforeEach((to, from, next) => {
  const token = localStorage.getItem("token");
  let isAuthenticated = false;

  if (token) {
    try {
      const decoded = jwtDecode(token);
      const now = Date.now() / 1000;

      // If token is expired, remove auth data and redirect to login
      if (decoded.exp && decoded.exp < now) {
        console.warn("Token expired, logging out...");
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        return next('/login');
      }

      isAuthenticated = true;
    } catch (error) {
      // If token cannot be parsed, treat as unauthenticated
      console.error("Error when decrypting token:", error);
      localStorage.removeItem("token");
      localStorage.removeItem("user");
    }
  }

  // Prevent authenticated users from accessing guest-only pages
  if (to.meta.guest && isAuthenticated) {
    return next('/dashboard');
  }

  // Prevent unauthenticated users from accessing protected routes
  if (to.meta.requiresAuth && !isAuthenticated) {
    return next('/login');
  }

  // Continue navigation if all guard checks passed
  next();
});

export default router;