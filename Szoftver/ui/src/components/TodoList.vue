<template>
    <div class="container">
      <div class="todo-list">
        <div class="header">To do list</div>
        <div class="task-list">
          <div class="task" v-for="(task, index) in tasks" :key="index">
            {{ task.text }}
          </div>
        </div>
      </div>
    </div>
  </template>
  
  <script>
  import { ref } from "vue";
  
  export default {
    setup() {
      const tasks = ref([
        { text: "Lorem ipsum" },
        { text: "Lorem ipsum dolor sit amet consectetur adipiscing elit." },
        { text: "Lorem ipsum dolor sit amet consectetur adipiscing elit." },
      ]);
  
      return {
        tasks,
      };
    },
  };
  </script>
  
  <style scoped>
  .container {
    display: flex;
    height: 100vh;
  }
  
  .todo-list {
    flex: 1;
    display: flex;
    flex-direction: column;
    position: relative;
    padding: 20px;
    border-right: none;
  }
  
  .header {
    font-size: 1.5rem;
    font-weight: bold;
    text-align: center;
    margin-bottom: 10px;
    padding: 10px;
    background: linear-gradient(to right, #9dc1b7, #b1dbd3);
    border-radius: 8px;
  }
  
  .task-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  
  .task {
    padding: 10px;
    background: white;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }
  </style>