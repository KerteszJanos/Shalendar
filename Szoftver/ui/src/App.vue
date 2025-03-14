<template>
<div class="app-container">
    <nav v-if="isLoggedIn" class="navbar">
        <div class="nav-left">
            <router-link to="/dashboard" class="dashboard-logo">
                <img src="@/assets/images/Shalendar_logo.png" alt="Shalendar Logo" class="logo" />
            </router-link>
        </div>

        <div class="nav-right">
            <router-link to="/profile" class="profile-button">
                <LucideUser size="20" />
            </router-link>
            <button @click="logout" class="logout-button">
                <LucideLogOut size="20" />
            </button>
        </div>
    </nav>

    <div class="content-container">
      <router-view></router-view>
    </div>
</div>
</template>

<script>
import {
    ref,
    onMounted,
    onUnmounted,
    watchEffect
} from "vue";
import {
    useLogout
} from "@/utils/LogoutHandler";
import {
    LucideUser,
    LucideLogOut
} from "lucide-vue-next";

export default {
    components: {
        LucideUser,
        LucideLogOut,
    },
    setup() {
        const {
            logout
        } = useLogout();
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

        return {
            isLoggedIn,
            logout
        };
    },
};
</script>

<style scoped>
.content-container {
    flex: 1;
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 100%;
    overflow: auto;
}

.app-container {
    display: flex;
    flex-direction: column;
    height: 100%;
    width: 100%;
}

.navbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background-color: #0B6477;
    padding: 5px 20px;
    border-radius: 0 0 10px 10px;
    height: 60px;
}

.nav-left {
    display: flex;
    align-items: center;
}

.dashboard-logo {
    display: flex;
    align-items: center;
    justify-content: center;
    transition: all 0.3s ease-in-out;
}

.dashboard-logo img {
    height: 50px;
    transition: all 0.3s ease-in-out;
}

.dashboard-logo:hover img {
    transform: scale(1.1);
    filter: brightness(1.2);
}

.nav-right {
    display: flex;
    align-items: center;
    gap: 15px;
}

.profile-button {
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: #1976d2;
    color: white;
    border-radius: 50%;
    width: 40px;
    height: 40px;
    transition: all 0.3s ease-in-out;
}

.profile-button:hover {
    background-color: #1565c0;
    transform: scale(1.1);
    box-shadow: 0 4px 10px rgba(0, 0, 0, 0.2);
}

.logout-button {
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: #e74c3c;
    color: white;
    border: none;
    padding: 8px 12px;
    cursor: pointer;
    font-size: 16px;
    border-radius: 5px;
    gap: 5px;
    transition: all 0.3s ease-in-out;
}

.logout-button:hover {
    background-color: #c0392b;
    transform: scale(1.1);
}
</style>
