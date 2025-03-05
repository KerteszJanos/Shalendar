<template>
<div class="login-container">
    <h2>Login</h2>
    <form @submit.prevent="loginUser">
        <label for="email">Email:</label>
        <input type="email" id="email" v-model="email" required />

        <label for="password">Password:</label>
        <input type="password" id="password" v-model="password" required />

        <button type="submit">Login</button>
    </form>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <p class="register-link">
        Don't have an account?
        <router-link to="/register">Sign up here!</router-link>
    </p>
</div>
</template>

  
<script>
import {
    ref
} from "vue";
import {
    useRouter
} from "vue-router";
import axios from "axios";
import {
    API_BASE_URL
} from "@/utils/config/config";
import {
    setErrorMessage
} from "@/utils/errorHandler";

export default {
    setup() {
        const email = ref("");
        const password = ref("");
        const errorMessage = ref("");
        const router = useRouter();

        const loginUser = async () => {
            try {
                const response = await axios.post(`${API_BASE_URL}/api/Users/login`, {
                    email: email.value,
                    password: password.value,
                });

                if (response.data.token) {
                    localStorage.setItem("token", response.data.token);
                    localStorage.setItem("user", JSON.stringify(response.data.user));
                    axios.defaults.headers.common["Authorization"] = `Bearer ${response.data.token}`;
                    window.dispatchEvent(new Event("storage"));
                    router.push("/dashboard");
                }
            } catch (error) {
                setErrorMessage(errorMessage, error.response?.data || "Login failed");
                console.error("Login failed:", error);
            }
        };

        return {
            email,
            password,
            errorMessage,
            loginUser,
        };
    },
};
</script>

  
<style scoped>
.login-container {
    max-width: 400px;
    margin: auto;
    padding: 20px;
    border: 1px solid #ccc;
    border-radius: 10px;
    background: white;
    text-align: center;
}

label {
    display: block;
    margin-top: 10px;
}

input {
    width: 100%;
    padding: 8px;
    margin: 5px 0 15px;
    border: 1px solid #ccc;
    border-radius: 5px;
}

button {
    width: 100%;
    padding: 10px;
    background-color: #28a745;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
}

button:hover {
    background-color: #218838;
}

.error {
    color: red;
    margin-top: 10px;
}

.register-link {
    margin-top: 15px;
}
</style>
