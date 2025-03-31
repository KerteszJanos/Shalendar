<!--
  - Collects username, email, and password input.
  - Validates password strength and match.
  - Submits data to the API and redirects to login on success.
-->

<template>
<div class="register-container">
    <h2>Registration</h2>
    <form @submit.prevent="registerUser">
        <label for="username">Username:</label>
        <input type="text" id="username" v-model="username" required />

        <label for="email">Email:</label>
        <input type="email" id="email" v-model="email" required />

        <label for="password">Password:</label>
        <input type="password" id="password" v-model="password" required @input="validatePassword" />

        <ul class="password-criteria">
            <li :class="{ valid: password.length >= 8 }">✔ At least 8 characters</li>
            <li :class="{ valid: /[A-Z]/.test(password) }">✔ At least one uppercase letter</li>
            <li :class="{ valid: /[0-9]/.test(password) }">✔ At least one number</li>
        </ul>

        <label for="passwordAgain">Confirm Password:</label>
        <input type="password" id="passwordAgain" v-model="passwordAgain" required />

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
        // ---------------------------------
        // Constants	        		   |
        // --------------------------------- 
        const router = useRouter();

        // ---------------------------------
        // Reactive state		           |
        // ---------------------------------
        const username = ref("");
        const email = ref("");
        const password = ref("");
        const passwordAgain = ref("");
        const errorMessage = ref("");

        // ---------------------------------
        // Methods		            	   |
        // ---------------------------------
        // --------------
        // Core actions	|
        // --------------
        const registerUser = async () => {
            errorMessage.value = "";

            if (!username.value.trim()) {
                setErrorMessage(errorMessage, "Username cannot be empty or just whitespace!");
                return;
            }

            if (password.value !== passwordAgain.value) {
                setErrorMessage(errorMessage, "Passwords do not match!");
                return;
            }

            try {
                await axios.post(`${API_BASE_URL}/api/Users`, {
                    email: email.value,
                    username: username.value.trim(),
                    password: password.value,
                    defaultCalendarId: 0,
                });

                alert("Registration successful! You can now log in.");
                router.push("/login");
            } catch (error) {
                if (error.response && error.response.status === 400) {
                    setErrorMessage(errorMessage, `Error registering user: ${error.response.data}`);
                    console.error("Error registering user:", error);
                } else {
                    setErrorMessage(errorMessage, `Error registering user.`);
                    console.error("Error registering user:", error);
                }
            }
        };

        // --------------
        // Helpers	    |
        // --------------
        const validatePassword = () => {
            return (
                password.value.length >= 8 &&
                /[A-Z]/.test(password.value) &&
                /[0-9]/.test(password.value)
            );
        };

        return {
            username,
            email,
            password,
            passwordAgain,
            errorMessage,
            validatePassword,
            registerUser,
        };
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
