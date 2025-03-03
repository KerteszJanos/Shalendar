<template>
  <div class="dashboard" v-if="isMounted">
    <CalendarView />
    <CalendarLists />
  </div>
</template>

<script>
import { ref, onMounted } from "vue";
import CalendarView from "@/components/organisms/CalendarView.vue";
import CalendarLists from "@/components/organisms/CalendarLists.vue";

export default {
  components: {
    CalendarView,
    CalendarLists,
  },
  setup() {
    const isMounted = ref(false);

    onMounted(() => {
      try {
        const user = JSON.parse(localStorage.getItem("user"));
        if (!user || !user.defaultCalendarId) {
          throw new Error("No default calendar set.");
        }

        const existingCalendarId = localStorage.getItem("calendarId");
        if (!existingCalendarId) {
          localStorage.setItem("calendarId", user.defaultCalendarId);
        }
      } catch (error) {
        console.error("Error loading calendar data:", error.message);
      } finally {
        isMounted.value = true;
      }
    });

    return {
      isMounted,
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
