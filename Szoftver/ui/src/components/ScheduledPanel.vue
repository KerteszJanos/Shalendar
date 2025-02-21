<template>
    <div class="container">
      <div class="day-view">
        <div class="header">{{ formattedDate }}</div>
        <div class="time-container">
          <div class="time-scrollable">
            <div class="hour-marker" v-for="hour in 24" :key="hour" :style="{ top: `${hour * 100}px` }">
              {{ hour }}:00
            </div>
            <div v-if="isToday" class="time-indicator" :style="timeIndicatorStyle">
              <span class="time-label">{{ currentTime }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </template>
  
  <script>
  import { useRoute } from "vue-router";
  import { computed, ref, onMounted, onUnmounted } from "vue";
  
  export default {
    setup() {
      const route = useRoute();
      
      const formattedDate = computed(() => {
        const date = new Date(route.params.date);
        return date.toLocaleDateString("hu-HU", { year: "numeric", month: "long", day: "numeric" });
      });
  
      const currentTime = ref(getCurrentTime());
  
      function getCurrentTime() {
        const now = new Date();
        return now.toLocaleTimeString("hu-HU", { hour: "2-digit", minute: "2-digit" });
      }
  
      const timeIndicatorStyle = ref(getTimeIndicatorStyle());
  
      function getTimeIndicatorStyle() {
        const now = new Date();
        const percentage = ((now.getHours() * 60 + now.getMinutes()) / (24 * 60)) * 100;
        return { top: `${percentage}%` };
      }
  
      const isToday = computed(() => {
        const now = new Date();
        const selectedDate = new Date(route.params.date);
        return (
          now.getFullYear() === selectedDate.getFullYear() &&
          now.getMonth() === selectedDate.getMonth() &&
          now.getDate() === selectedDate.getDate()
        );
      });
  
      let intervalId;
      onMounted(() => {
        if (isToday.value) {
          const updateClock = () => {
            currentTime.value = getCurrentTime();
            timeIndicatorStyle.value = getTimeIndicatorStyle();
            const now = new Date();
            const delay = (60 - now.getSeconds()) * 1000;
            clearInterval(intervalId);
            intervalId = setInterval(updateClock, delay);
          };
          updateClock();
        }
      });
  
      onUnmounted(() => {
        if (intervalId) {
          clearInterval(intervalId);
        }
      });
  
      return {
        formattedDate,
        timeIndicatorStyle,
        currentTime,
        isToday,
      };
    },
  };
  </script>
  
  <style scoped>
  .container {
    display: flex;
    height: 100vh;
  }
  
  .day-view {
    flex: 1;
    display: flex;
    flex-direction: column;
    position: relative;
    padding: 20px;
    border-right: 1px solid #ccc;
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
  
  .time-container {
    flex-grow: 1;
    overflow-y: auto;
    position: relative;
    border-radius: 8px;
    margin-top: 20px;
  }
  
  .time-scrollable {
    position: relative;
    height: 2400px;
  }
  
  .hour-marker {
    position: absolute;
    width: 100%;
    height: 100px;
    border-bottom: 1px solid rgba(0, 0, 0, 0.2);
    line-height: 100px;
    padding-left: 10px;
    font-size: 1rem;
    font-weight: bold;
    color: #333;
  }
  
  .time-indicator {
    position: absolute;
    left: 10px;
    right: 10px;
    height: 4px;
    background: #ff6347;
    transition: top 1s linear;
    display: flex;
    align-items: center;
    justify-content: flex-start;
    padding-left: 5px;
  }
  
  .time-label {
    background: white;
    padding: 2px 5px;
    border-radius: 4px;
    font-size: 0.8rem;
    font-weight: bold;
    color: #333;
  }
  </style>