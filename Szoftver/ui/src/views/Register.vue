<template>
<div class="register-container">
    <h2>Registration</h2>
    <form @submit.prevent="registerUser">
        <label for="username">Username:</label>
        <input type="text" id="username" v-model="user.username" required />

        <label for="email">Email:</label>
        <input type="email" id="email" v-model="user.email" required />

        <label for="password">Password:</label>
        <input type="password" id="password" v-model="user.password" required />

        <label for="passwordAgain">Confirm Password:</label>
        <input type="password" id="passwordAgain" v-model="user.passwordAgain" required />

        <button type="submit">Register</button>
    </form>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <p class="login-link">
        Already have an account?
        <router-link to="/login">Sign in here!</router-link>
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
                username: '',
                password: '',
                passwordAgain: '',
            },
            errorMessage: '',
        };
    },
    methods: {
        async registerUser() {
            try {
                if (this.user.password !== this.user.passwordAgain) {
                    this.errorMessage = 'Passwords do not match!';
                    return;
                }

                const userResponse = await axios.post(`${API_BASE_URL}/api/Users`, {
                    email: this.user.email,
                    username: this.user.username,
                    password: this.user.password,
                    defaultCalendarId: 0,
                });
                this.$router.push("/login");
            } catch (error) {
                console.error('Error registering user:', error);
                this.errorMessage = 'Registration failed. Please try again!';
            }
        }

    },
};
</script>

<style scoped>
.register-container {
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
    background-color: #007bff;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
}

button:hover {
    background-color: #0056b3;
}

.error {
    color: red;
    margin-top: 10px;
}

.login-link {
    margin-top: 15px;
}
</style>
