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

        <ul class="password-criteria">
            <li :class="{ valid: user.password.length >= 8 }">✔ At least 8 characters</li>
            <li :class="{ valid: /[A-Z]/.test(user.password) }">✔ At least one uppercase letter</li>
            <li :class="{ valid: /[0-9]/.test(user.password) }">✔ At least one number</li>
        </ul>

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
            this.errorMessage = '';

            if (!this.user.username.trim()) {
                this.errorMessage = 'Username cannot be empty or just whitespace!';
                return;
            }

            if (this.user.password !== this.user.passwordAgain) {
                this.errorMessage = 'Passwords do not match!';
                return;
            }

            try {
                const response = await axios.post(`${API_BASE_URL}/api/Users`, {
                    email: this.user.email,
                    username: this.user.username.trim(),
                    password: this.user.password,
                    defaultCalendarId: 0,
                });

                this.$router.push("/login");
            } catch (error) {
                console.error('Error registering user:', error);

                if (error.response && error.response.status === 400) {
                    this.errorMessage = error.response.data;
                } else {
                    this.errorMessage = 'Registration failed. Please try again!';
                }
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

.password-criteria {
    list-style: none;
    padding: 0;
    font-size: 0.9em;
    margin-top: -5px;
    text-align: left;
    color: red;
}

.password-criteria .valid {
    color: green;
}

.login-link {
    margin-top: 15px;
}
</style>
