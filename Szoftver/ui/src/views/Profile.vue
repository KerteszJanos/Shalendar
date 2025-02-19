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
      <input type="password" id="oldPassword" v-model="passwordChange.oldPassword" required />

      <label for="newPassword">New Password:</label>
      <input type="password" id="newPassword" v-model="passwordChange.newPassword" required @input="validatePassword" />

      <ul class="password-criteria">
        <li :class="{ valid: passwordChange.newPassword.length >= 8 }">✔ At least 8 characters</li>
        <li :class="{ valid: /[A-Z]/.test(passwordChange.newPassword) }">✔ At least one uppercase letter</li>
        <li :class="{ valid: /[0-9]/.test(passwordChange.newPassword) }">✔ At least one number</li>
      </ul>

      <label for="confirmPassword">Confirm New Password:</label>
      <input type="password" id="confirmPassword" v-model="passwordChange.confirmPassword" required />

      <button type="submit" :disabled="!isPasswordValid">Change Password</button>
    </form>

    <h2>Delete Account</h2>
    <button @click="deleteAccount" class="delete-btn">Delete Account</button>

    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
    <p v-if="successMessage" class="success">{{ successMessage }}</p>
  </div>
</template>

<script>
import api from "@/utils/config/axios-config";

export default {
  data() {
    return {
      user: {
        email: "",
        username: "",
      },
      passwordChange: {
        oldPassword: "",
        newPassword: "",
        confirmPassword: "",
      },
      errorMessage: "",
      successMessage: "",
      isPasswordValid: false,
    };
  },
  async created() {
    await this.fetchUser();
  },
  methods: {
    async fetchUser() {
      this.errorMessage = "";
      try {
        const response = await api.get("/api/Users/me");
        this.user = response.data;
      } catch (error) {
        console.error("Error fetching user:", error);
        this.errorMessage = error.response?.data || "Failed to load user data.";
      }
    },

    validatePassword() {
      const { newPassword } = this.passwordChange;
      this.isPasswordValid =
        newPassword.length >= 8 &&
        /[A-Z]/.test(newPassword) &&
        /[0-9]/.test(newPassword);
    },

    async changePassword() {
      this.errorMessage = "";
      this.successMessage = "";

      if (!this.isPasswordValid) {
        this.errorMessage = "New password does not meet security requirements.";
        return;
      }

      if (this.passwordChange.newPassword !== this.passwordChange.confirmPassword) {
        this.errorMessage = "Passwords do not match!";
        return;
      }

      try {
        await api.put("/api/Users/change-password", {
          oldPassword: this.passwordChange.oldPassword,
          newPassword: this.passwordChange.newPassword,
        });
        this.successMessage = "Password changed successfully!";
        this.passwordChange.oldPassword = "";
        this.passwordChange.newPassword = "";
        this.passwordChange.confirmPassword = "";
      } catch (error) {
        console.error("Error changing password:", error);
        this.errorMessage = error.response?.data || "Failed to change password.";
      }
    },

    async deleteAccount() {
      if (!confirm("Are you sure you want to delete your account? This action is irreversible!")) {
        return;
      }

      this.errorMessage = "";
      try {
        await api.delete("/api/Users/delete");
        localStorage.removeItem("token");
        this.$router.push("/login");
      } catch (error) {
        console.error("Error deleting account:", error);
        this.errorMessage = error.response?.data || "Failed to delete account.";
      }
    },
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

h1, h2 {
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
