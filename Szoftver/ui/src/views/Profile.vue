<template>
<div class="profile-container">
    <h1>Profile</h1>

    <div v-if="user">
        <p><strong>Username:</strong> {{ user.username }}</p>
        <p><strong>Email:</strong> {{ user.email }}</p>
        <p v-if="defaultCalendarName"><strong>Default Calendar:</strong> {{ defaultCalendarName }}</p>
    </div>

    <div class="action-buttons">
        <h2>Change Password</h2>
        <button @click="showPasswordModal = true" class="btn changePassword">Change Password</button>
    </div>

    <div class="action-buttons">
        <h2>Delete Account</h2>
        <button @click="deleteAccount" class="btn deleteAccount">Delete Account</button>
    </div>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-if="successMessage" class="success">{{ successMessage }}</p>

    <Modal :show="showPasswordModal" title="Change Password" @close="showPasswordModal = false" @confirm="changePassword">
        <p v-if="PasswordModalErrorMessage" class="error">{{ PasswordModalErrorMessage }}</p>
        <form @submit.prevent="changePassword" class="modal-content">
            <label for="oldPassword" class="required-label">Old Password:</label>
            <input type="password" id="oldPassword" v-model="oldPassword" required />

            <label for="newPassword" class="required-label">New Password:</label>
            <input type="password" id="newPassword" v-model="newPassword" required @input="validatePassword" />

            <ul class="password-criteria">
                <li :class="{ valid: newPassword.length >= 8 }">✔ At least 8 characters</li>
                <li :class="{ valid: /[A-Z]/.test(newPassword) }">✔ At least one uppercase letter</li>
                <li :class="{ valid: /[0-9]/.test(newPassword) }">✔ At least one number</li>
            </ul>

            <label for="confirmPassword" class="required-label">Confirm New Password:</label>
            <input type="password" id="confirmPassword" v-model="confirmPassword" required />
        </form>
    </Modal>
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
import Modal from "@/components/molecules/Modal.vue";

export default {
    components: {
        Modal
    },
    setup() {
        const user = ref(null);
        const oldPassword = ref("");
        const newPassword = ref("");
        const confirmPassword = ref("");
        const errorMessage = ref("");
        const successMessage = ref("");
        const isPasswordValid = ref(false);
        const router = useRouter();
        const defaultCalendarName = ref("");
        const showPasswordModal = ref(false);
        const PasswordModalErrorMessage = ref(false);

        const fetchUser = async () => {
            errorMessage.value = "";
            try {
                const response = await api.get("/api/Users/me");
                user.value = response.data;
                if (user.value.defaultCalendarId) {
                    fetchDefaultCalendar(user.value.defaultCalendarId);
                } else {
                    defaultCalendarName.value = "No default calendar set"
                }
            } catch (error) {
                console.error("Error fetching user:", error);
                setErrorMessage(errorMessage, error.response?.data || "Failed to load user data.");
            }
        };

        const fetchDefaultCalendar = async (calendarId) => {
            try {
                const response = await api.get(`/api/Calendars/${calendarId}`);
                defaultCalendarName.value = response.data.name;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    defaultCalendarName.value = `Access denied: ${error.response.data?.message || "You do not have permission."}`;
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(errorMessage, "Error fetching default calendar");
                    console.error("Error fetching default calendar:", error);
                }
            }
        };

        const validatePassword = () => {
            isPasswordValid.value =
                newPassword.value.length >= 8 &&
                /[A-Z]/.test(newPassword.value) &&
                /[0-9]/.test(newPassword.value);
        };

        const changePassword = async () => {
            PasswordModalErrorMessage.value = "";
            successMessage.value = "";

            if (!isPasswordValid.value) {
                setErrorMessage(PasswordModalErrorMessage, "New password does not meet security requirements.");
                return;
            }

            if (newPassword.value !== confirmPassword.value) {
                setErrorMessage(PasswordModalErrorMessage, "Passwords do not match!");
                return;
            }

            try {
                await api.put("/api/Users/change-password", {
                    oldPassword: oldPassword.value,
                    newPassword: newPassword.value,
                });
                setErrorMessage(successMessage, "Password changed successfully!");
                oldPassword.value = "";
                newPassword.value = "";
                confirmPassword.value = "";
                showPasswordModal.value = false;
            } catch (error) {
                setErrorMessage(PasswordModalErrorMessage, error.response?.data || "Failed to change password.");
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
                window.dispatchEvent(new Event("storage"));
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
            defaultCalendarName,
            showPasswordModal,
            PasswordModalErrorMessage
        };
    },
};
</script>

<style>
.action-buttons {
    text-align: center;
    margin-top: 20px;
}

.btn {
    display: inline-block;
    padding: 10px 20px;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
    transition: all 0.3s ease-in-out;
}

.btn:hover
{
    transform: scale(1.1);
}

.btn.changePassword
{
    background-color: #213A57;
}

.btn.changePassword:hover
{
    background-color: #294b72;
}

.btn.deleteAccount
{
    background-color: #e74c3c;
}
.btn.deleteAccount:hover
{
    background-color: #fa5240;
}


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
