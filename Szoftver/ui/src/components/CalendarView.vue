<template>
  <div class="calendar-layout">
    <div class="calendar-container">
      <div class="calendar-header">
        <h2 v-if="calendar.name">{{ calendar.name }} - {{ formattedMonth }}</h2>
        <h2 v-else>Betöltés...</h2>
        <div class="navigation">
          <button @click="prevMonth">◀</button>
          <button @click="nextMonth">▶</button>
        </div>
      </div>
      <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

      <div class="calendar-grid">
        <div v-for="day in daysOfWeek" :key="day" class="calendar-day header">
          {{ day }}
        </div>
        <div
          v-for="day in calendarDays"
          :key="day.date"
          class="calendar-day"
          :class="{ 'other-month': !day.isCurrentMonth }"
          @click="goToDay(day.date)"
        >
          {{ day.number }}
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import api from "@/utils/config/axios-config";

export default {
  setup() {
    const currentDate = ref(new Date());
    const calendar = ref({ id: null, name: "" });
    const errorMessage = ref("");
    const router = useRouter();

    const daysOfWeek = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    const formattedMonth = computed(() => {
      return currentDate.value.toLocaleDateString("hu-HU", { year: "numeric", month: "long" });
    });

    const calendarDays = computed(() => {
      const year = currentDate.value.getFullYear();
      const month = currentDate.value.getMonth();
      const firstDayOfMonth = new Date(year, month, 1).getDay();
      const lastDateOfMonth = new Date(year, month + 1, 0).getDate();

      let days = [];
      let startOffset = firstDayOfMonth === 0 ? 6 : firstDayOfMonth - 1;
      let endOffset = (7 - ((startOffset + lastDateOfMonth) % 7)) % 7;

      const prevMonthLastDate = new Date(year, month, 0).getDate();
      for (let i = startOffset - 1; i >= 0; i--) {
        days.push({
          number: prevMonthLastDate - i,
          date: new Date(year, month - 1, prevMonthLastDate - i).toISOString().split('T')[0],
          isCurrentMonth: false
        });
      }

      for (let i = 1; i <= lastDateOfMonth; i++) {
        days.push({
          number: i,
          date: new Date(year, month, i+1).toISOString().split('T')[0],
          isCurrentMonth: true
        });
      }

      for (let i = 1; i <= endOffset; i++) {
        days.push({
          number: i,
          date: new Date(year, month + 1, i).toISOString().split('T')[0],
          isCurrentMonth: false
        });
      }

      return days;
    });

    const goToDay = (date) => {
      router.push(`/day/${date}`);
    };

    const fetchCalendar = async () => {
      try {
        const user = JSON.parse(localStorage.getItem("user"));
        if (!user || !user.defaultCalendarId) {
          throw new Error("Nincs alapértelmezett naptár beállítva.");
        }
        const calendarId = user.defaultCalendarId;
        const response = await api.get(`/api/Calendars/${calendarId}`);
        calendar.value = response.data;
      } catch (error) {
        console.error("Error loading calendar:", error);
        errorMessage.value = "Nem sikerült betölteni a naptárat.";
      }
    };

    const prevMonth = () => {
      currentDate.value = new Date(currentDate.value.getFullYear(), currentDate.value.getMonth() - 1, 1);
    };

    const nextMonth = () => {
      currentDate.value = new Date(currentDate.value.getFullYear(), currentDate.value.getMonth() + 1, 1);
    };

    onMounted(fetchCalendar);

    return {
      formattedMonth,
      daysOfWeek,
      calendarDays,
      calendar,
      errorMessage,
      goToDay,
      prevMonth,
      nextMonth,
    };
  },
};
</script>

<style scoped>
.calendar-layout {
  display: flex;
  justify-content: flex-start;
  align-items: flex-start;
  width: 100vw;
  height: 90vh;
}

.calendar-container {
  width: 66vw;
  height: 90vh;
  background: #e3f2fd;
  padding: 20px;
  border-radius: 10px;
}

.calendar-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #a5d6a7;
  padding: 10px;
  border-radius: 5px;
}

.navigation button {
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  margin: 0 5px;
  transition: all 0.2s ease-in-out;
}

.navigation button:hover {
  transform: scale(0.9);
  background: rgba(0, 0, 0, 0.1);
}

.calendar-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 5px;
  margin-top: 10px;
}

.calendar-day {
  background: #fff;
  padding: 20px;
  text-align: center;
  border-radius: 5px;
  height: 80px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease-in-out;
}

.calendar-day:not(.header):hover {
  background: #bdbdbd;
  transform: scale(0.95);
}

.calendar-day.header {
  font-weight: bold;
  background: #90caf9;
  cursor: default;
  transform: none !important;
  height: auto;
}

.other-month {
  color: gray;
  opacity: 0.5;
}

.error {
  color: red;
  text-align: center;
}
</style>
