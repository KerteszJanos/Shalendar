<template>
    <div class="login-container">
      <h2>Bejelentkezés</h2>
      <form @submit.prevent="loginUser">
        <label for="username">Felhasználónév:</label>
        <input type="text" id="username" v-model="user.username" required />
  
        <label for="password">Jelszó:</label>
        <input type="password" id="password" v-model="user.password" required />
  
        <button type="submit">Bejelentkezés</button>
      </form>
      
      <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
  
      <p class="register-link">
        Még nincs fiókod?
        <router-link to="/register">Regisztrálj itt!</router-link>
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
          password: '',
        },
        errorMessage: '',
      };
    },
    methods: {
      async loginUser() {
        try {
          const response = await axios.post('http://localhost:5000/api/auth/login', this.user); //kitenni services mappába
          localStorage.setItem('token', response.data.token); // JWT elmentése
          alert('Sikeres bejelentkezés!');
          this.$router.push('/dashboard'); // Átirányítás a főoldalra (később készítheted el)
        } catch (error) {
          this.errorMessage = error.response?.data?.message || 'Hibás bejelentkezési adatok.';
        }
      },
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
  