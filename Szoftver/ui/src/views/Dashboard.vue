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
  gap: 5px;
}
</style>
