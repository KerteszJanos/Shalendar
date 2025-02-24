import { createRouter, createWebHistory } from 'vue-router';
import Login from '../views/Login.vue';
import Register from '../views/Register.vue';
import Dashboard from '../views/Dashboard.vue';
import Profile from '../views/Profile.vue';
import CalendarView from '../components/organisms/CalendarView.vue';
import DayView from '../views/DayView.vue';
import { jwtDecode } from "jwt-decode";

const routes = [
  { path: '/', redirect: '/login' },
  { path: '/login', component: Login, meta: { guest: true } },
  { path: '/register', component: Register, meta: { guest: true } },
  { path: '/dashboard', component: Dashboard, meta: { requiresAuth: true } },
  { path: '/profile', component: Profile, meta: { requiresAuth: true } },
  { path: '/calendar', component: CalendarView, meta: { requiresAuth: true } },
  { path: '/day/:date', component: DayView, props: true, meta: { requiresAuth: true } },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem("token");
  let isAuthenticated = false;

  if (token) {
    try {
      const decoded = jwtDecode(token);
      const now = Date.now() / 1000;

      if (decoded.exp && decoded.exp < now) {
        console.warn("Token expired, logging out...");
        localStorage.removeItem("token");
        localStorage.removeItem("user");
        return next('/login');
      }

      isAuthenticated = true;
    } catch (error) {
      console.error("Error when decrypting token:", error);
      localStorage.removeItem("token");
      localStorage.removeItem("user");
    }
  }

  if (to.meta.guest && isAuthenticated) {
    return next('/dashboard');
  }

  if (to.meta.requiresAuth && !isAuthenticated) {
    return next('/login');
  }

  next();
});

export default router;
