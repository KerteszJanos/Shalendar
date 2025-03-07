<template>
<div class="profile-container">
    <h1>Profile</h1>

    <div v-if="user">
        <p><strong>Username:</strong> {{ user.username }}</p>
        <p><strong>Email:</strong> {{ user.email }}</p>
    </div>

    <h2>Change Password</h2>
    <form @submit.prevent="changePassword">
        <label for="oldPassword">Old Password:</label>
        <input type="password" id="oldPassword" v-model="oldPassword" required />

        <label for="newPassword">New Password:</label>
        <input type="password" id="newPassword" v-model="newPassword" required @input="validatePassword" />

        <ul class="password-criteria">
            <li :class="{ valid: newPassword.length >= 8 }">✔ At least 8 characters</li>
            <li :class="{ valid: /[A-Z]/.test(newPassword) }">✔ At least one uppercase letter</li>
            <li :class="{ valid: /[0-9]/.test(newPassword) }">✔ At least one number</li>
        </ul>

        <label for="confirmPassword">Confirm New Password:</label>
        <input type="password" id="confirmPassword" v-model="confirmPassword" required />

        <button type="submit" :disabled="!isPasswordValid">Change Password</button>
    </form>

    <h2>Delete Account</h2>
    <button @click="deleteAccount" class="delete-btn">Delete Account</button>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-if="successMessage" class="success">{{ successMessage }}</p>
</div>
</template>

<script>
import {
    ref,
    onMounted
} from "vue";
import {
    useRouter
} from "vue-router";
import api from "@/utils/config/axios-config";
import {
    setErrorMessage
} from "@/utils/errorHandler";

export default {
    setup() {
        const user = ref(null);
        const oldPassword = ref("");
        const newPassword = ref("");
        const confirmPassword = ref("");
        const errorMessage = ref("");
        const successMessage = ref("");
        const isPasswordValid = ref(false);
        const router = useRouter();

        const fetchUser = async () => {
            errorMessage.value = "";
            try {
                const response = await api.get("/api/Users/me");
                user.value = response.data;
            } catch (error) {
                console.error("Error fetching user:", error);
                setErrorMessage(errorMessage, error.response?.data || "Failed to load user data.");
            }
        };

        const validatePassword = () => {
            isPasswordValid.value =
                newPassword.value.length >= 8 &&
                /[A-Z]/.test(newPassword.value) &&
                /[0-9]/.test(newPassword.value);
        };

        const changePassword = async () => {
            errorMessage.value = "";
            successMessage.value = "";

            if (!isPasswordValid.value) {
                setErrorMessage(errorMessage, "New password does not meet security requirements.");
                return;
            }

            if (newPassword.value !== confirmPassword.value) {
                setErrorMessage(errorMessage, "Passwords do not match!");
                return;
            }

            try {
                await api.put("/api/Users/change-password", {
                    oldPassword: oldPassword.value,
                    newPassword: newPassword.value,
                });
                successMessage.value = "Password changed successfully!";
                oldPassword.value = "";
                newPassword.value = "";
                confirmPassword.value = "";
            } catch (error) {
                setErrorMessage(errorMessage, error.response?.data || "Failed to change password.");
                console.error("Error changing password:", error);
            }
        };

        const deleteAccount = async () => {
            if (!confirm("Are you sure you want to delete your account? This action is irreversible!")) {
                return;
            }

            errorMessage.value = "";
            try {
                await api.delete("/api/Users/delete");
                localStorage.removeItem("user");
                localStorage.removeItem("token");
                localStorage.removeItem("calendarId");
                window.dispatchEvent(new Event("storage")); // Ensure UI updates
                router.push("/login");
            } catch (error) {
                setErrorMessage(errorMessage, error.response?.data || "Failed to delete account.");
                console.error("Error deleting account:", error);
            }
        };

        onMounted(fetchUser);

        return {
            user,
            oldPassword,
            newPassword,
            confirmPassword,
            errorMessage,
            successMessage,
            isPasswordValid,
            validatePassword,
            changePassword,
            deleteAccount,
        };
    },
};
</script>

<style>
.profile-container {
    max-width: 600px;
    margin: auto;
    padding: 20px;
    background: #f9f9f9;
    border-radius: 8px;
    box-shadow: 0px 0px 10px rgba(0, 0, 0, 0.1);
}

h1,
h2 {
    text-align: center;
}

label {
    display: block;
    margin-top: 10px;
    font-weight: bold;
}

input {
    width: 100%;
    padding: 8px;
    margin-top: 5px;
    border: 1px solid #ccc;
    border-radius: 4px;
}

button:disabled {
    background: gray;
    cursor: not-allowed;
}

button:hover:not(:disabled) {
    background: #0056b3;
}

.delete-btn {
    background: red;
}

.delete-btn:hover {
    background: darkred;
}

.error {
    color: red;
    text-align: center;
    margin-top: 10px;
}

.success {
    color: green;
    text-align: center;
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
</style>
