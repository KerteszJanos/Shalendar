<template>
  <div>
    <nav v-if="!isLoggedIn">
      <router-link to="/login">Bejelentkezés</router-link> |
      <router-link to="/register">Regisztráció</router-link>
    </nav>

    <nav v-else>
      <router-link to="/dashboard">Dashboard</router-link> |
      <router-link to="/profile">Profil</router-link> |
      <button @click="logout">Kijelentkezés</button>
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
    const isLoggedIn = ref(!!localStorage.getItem("token")); // 🔥 Reaktív változó

    const checkLoginStatus = () => {
      isLoggedIn.value = !!localStorage.getItem("token"); // 🔥 Ha a token törlődik, azonnal frissít
    };

    // 🔥 FIGYELJÜK a token változásait ÉS kényszerítjük a fejléc frissítését
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
