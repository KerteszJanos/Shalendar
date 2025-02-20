<template>
    <div class="day-view">
      <h1>{{ formattedDate }}</h1>
    </div>
  </template>
  
  <script>
  import { useRoute } from "vue-router";
  import { computed } from "vue";
  
  export default {
    setup() {
      const route = useRoute();
  
      const formattedDate = computed(() => {
        const date = new Date(route.params.date);
        return date.toLocaleDateString("hu-HU", { year: "numeric", month: "long", day: "numeric" });
      });
  
      return {
        formattedDate,
      };
    },
  };
  </script>
  
  <style scoped>
  .day-view {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
    font-size: 2rem;
  }
  </style>
  