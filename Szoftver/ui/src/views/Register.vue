<template>
    <div class="register-container">
      <h2>Regisztráció</h2>
      <form @submit.prevent="registerUser">
        <label for="username">Felhasználónév:</label>
        <input type="text" id="username" v-model="user.username" required />
  
        <label for="email">Email:</label>
        <input type="email" id="email" v-model="user.email" required />
  
        <label for="password">Jelszó:</label>
        <input type="password" id="password" v-model="user.password" required />
  
        <button type="submit">Regisztráció</button>
      </form>
  
      <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
  
      <p class="login-link">
        Már van fiókod?
        <router-link to="/login">Jelentkezz be itt!</router-link>
      </p>
    </div>
  </template>
  
  <script>
  import axios from 'axios';
  
  export default {
    data() {
      return {
        user: {
          username: '',
          email: '',
          password: '',
        },
        errorMessage: '',
      };
    },
    methods: {
      async registerUser() {
        try {
          const response = await axios.post('http://localhost:5000/api/auth/register', this.user); //kitenni services mappába
          alert('Sikeres regisztráció!');
          this.$router.push('/login'); // Átirányítás a bejelentkezésre
        } catch (error) {
          this.errorMessage = error.response?.data?.message || 'Hiba történt a regisztráció során.';
        }
      },
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
  