<template>
  <div class="container">
    <DayPanel class="panel" />
    <TodoList class="panel" />
  </div>
</template>

<script>
import DayPanel from "@/components/ScheduledPanel.vue";
import TodoList from "@/components/TodoList.vue";

export default {
  components: {
    DayPanel,
    TodoList,
  },
};
</script>

<style scoped>
.container {
  display: flex;
  height: 100vh;
}

.panel {
  flex: 1;
  min-width: 50%;
}
</style>