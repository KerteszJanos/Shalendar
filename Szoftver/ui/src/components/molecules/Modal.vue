<template>
  <div v-if="show" class="modal-overlay">
    <div class="modal">
      <h2>{{ title }}</h2>
      <slot></slot>
      <div class="modal-actions">
        <button @click="closeModal">Cancel</button>
        <button @click="confirmAction">{{ confirmText }}</button>
      </div>
    </div>
  </div>
</template>

<script>
import { defineComponent } from "vue";

export default defineComponent({
  props: {
    show: Boolean,
    title: String,
    confirmText: { type: String, default: "Mentés" }
  },
  emits: ["close", "confirm"],
  setup(props, { emit }) {
    const closeModal = () => {
      emit("close");
    };

    const confirmAction = () => {
      emit("confirm");
    };

    return {
      closeModal,
      confirmAction
    };
  }
});
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(0, 0, 0, 0.7);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 9999;
}

.modal {
  background: white;
  padding: 20px;
  border-radius: 10px;
  width: 400px;
  text-align: center;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
  z-index: 10000;
}

.modal-actions {
  margin-top: 20px;
  display: flex;
  justify-content: space-between;
}
</style>
