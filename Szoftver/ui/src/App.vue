<template>
    <div>
      <h1>{{ title }}</h1>
      <button @click="incrementCounter">Click Me</button>
      <p>Counter: {{ counter }}</p>
    </div>
  </template>
  
  <script>
  export default {
    data() {
      return {
        title: "Hello Vue!",
        counter: 0
      };
    },
    methods: {
      incrementCounter() {
        this.counter++;
      }
    },
    computed: {
      doubleCounter() {
        return this.counter * 2;
      }
    },
    watch: {
      counter(newValue) {
        console.log("Counter changed:", newValue);
      }
    },
    mounted() {
      console.log("Component mounted!");
    }
  };
  </script>
  
  <style scoped>
  h1 {
    color: blue;
  }
  </style>