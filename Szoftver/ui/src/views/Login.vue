<template>
<div class="login-container">
    <h2>Login</h2>
    <form @submit.prevent="loginUser">
        <label for="email">Email:</label>
        <input type="email" id="email" v-model="user.email" required />

        <label for="password">Password:</label>
        <input type="password" id="password" v-model="user.password" required />

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
import axios from 'axios';
import {
    API_BASE_URL
} from '@/utils/config/config';

export default {
    data() {
        return {
            user: {
                email: '',
                password: '',
            },
            errorMessage: '',
        };
    },
    methods: {
        async loginUser() {
            try {
                const response = await axios.post(`${API_BASE_URL}/api/Users/login`, {
                    email: this.user.email,
                    password: this.user.password
                });

                if (response.data.token) {
                    localStorage.setItem("token", response.data.token);

                    localStorage.setItem("user", JSON.stringify(response.data.user));

                    axios.defaults.headers.common["Authorization"] = `Bearer ${response.data.token}`;

                    window.dispatchEvent(new Event("storage"));

                    this.$router.push("/dashboard");
                }
            } catch (error) {
                console.error("Login failed:", error);
                this.errorMessage = error.response.data;
            }
        }

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
