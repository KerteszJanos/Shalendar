<template>
    <div class="dashboard">
      <CalendarView />
      <CalendarLists />
    </div>
  </template>
  
  <script>
  import CalendarView from "@/components/organisms/CalendarView.vue";
  import CalendarLists from "@/components/organisms/CalendarLists.vue";
  
  export default {
    components: {
      CalendarView,
      CalendarLists,
    },
  };
  </script>
  
  <style scoped>
  .dashboard {
    display: flex;
    width: 100vw;
    height: 100vh;
  }
  </style>
  