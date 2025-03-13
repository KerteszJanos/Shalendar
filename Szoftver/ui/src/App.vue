<template>
  <div>
    <nav v-if="isLoggedIn" class="navbar">
      <div class="nav-left">
        <img src="@/assets/images/Shalendar_logo.png" alt="Shalendar Logo" class="logo" />
        <router-link to="/dashboard" class="nav-link">Dashboard</router-link>
      </div>

      <div class="nav-right">
        <router-link to="/profile" class="nav-link">Profil</router-link>
        <button @click="logout" class="logout-button">Kijelentkezés</button>
      </div>
    </nav>

    <router-view></router-view>
  </div>
</template>

<script>
import { ref, onMounted, onUnmounted, watchEffect } from "vue";
import { useLogout } from "@/utils/LogoutHandler";

export default {
  setup() {
    const { logout } = useLogout();
    const isLoggedIn = ref(!!localStorage.getItem("token"));

    const checkLoginStatus = () => {
      isLoggedIn.value = !!localStorage.getItem("token");
    };

    watchEffect(() => {
      checkLoginStatus();
    });

    const handleStorageChange = () => {
      checkLoginStatus();
    };

    onMounted(() => {
      checkLoginStatus();
      window.addEventListener("storage", handleStorageChange);
    });

    onUnmounted(() => {
      window.removeEventListener("storage", handleStorageChange);
    });

    return { isLoggedIn, logout };
  },
};
</script>

<style scoped>
.navbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background-color: #0B6477;
  padding: 10px 20px;
  border-radius: 0 0 10px 10px;
}


.nav-left {
  display: flex;
  align-items: center;
  gap: 15px;
}

.logo {
  height: 40px;
}

.nav-right {
  display: flex;
  align-items: center;
  gap: 15px;
}

.nav-link {
  color: white;
  text-decoration: none;
  font-size: 16px;
}

.nav-link:hover {
  text-decoration: underline;
}

.logout-button {
  background-color: #e74c3c;
  color: white;
  border: none;
  padding: 8px 12px;
  cursor: pointer;
  font-size: 16px;
  border-radius: 5px;
}

.logout-button:hover {
  background-color: #c0392b;
}
</style>