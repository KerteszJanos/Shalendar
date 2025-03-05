<template>
  <div class="dashboard" v-if="isMounted">
    <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
    <CalendarView />
    <CalendarLists />
  </div>
</template>

<script>
import { ref, onMounted } from "vue";
import CalendarView from "@/components/organisms/CalendarView.vue";
import CalendarLists from "@/components/organisms/CalendarLists.vue";
import { setErrorMessage } from "@/utils/errorHandler";

export default {
  components: {
    CalendarView,
    CalendarLists,
  },
  setup() {

    const errorMessage = ref("");
    const isMounted = ref(false);

    onMounted(() => {
      try {
        const user = JSON.parse(localStorage.getItem("user"));
        if (!user || !user.defaultCalendarId) {
          setErrorMessage(errorMessage, "No default calendar set.");
          console.error("No default calendar set.");
        }

        const existingCalendarId = localStorage.getItem("calendarId");
        if (!existingCalendarId) {
          localStorage.setItem("calendarId", user.defaultCalendarId);
        }
      } catch (error) {
        setErrorMessage(errorMessage, "Error loading calendar data.");
        console.error("Error loading calendar data:", error.message);
      } finally {
        isMounted.value = true;
      }
    });

    return {
      isMounted,
      errorMessage,
    };
  },
};
</script>

<style scoped>
.dashboard {
  display: flex;
  gap: 5px;
}
</style>
