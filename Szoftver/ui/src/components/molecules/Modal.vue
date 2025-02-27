<template>
  <div v-if="show" class="modal-overlay">
    <div class="modal">
      <h2>{{ title }}</h2>
      <slot></slot> <!-- Itt jelenik meg a tartalom -->
      <div class="modal-actions">
        <button @click="closeModal">Mégse</button>
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
  background: rgba(0, 0, 0, 0.7); /* Gpt generated - sötétebb háttér a jobb láthatóság érdekében */
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 9999; /* Gpt generated - biztosítja, hogy a modal minden felett legyen */
}

.modal {
  background: white;
  padding: 20px;
  border-radius: 10px;
  width: 400px; /* Gpt generated - kicsit nagyobb méret a jobb megjelenéshez */
  text-align: center;
  box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2); /* Gpt generated - árnyék hozzáadása a kiemeléshez */
  z-index: 10000; /* Gpt generated - modal maga is kiemelt */
}

.modal-actions {
  margin-top: 20px;
  display: flex;
  justify-content: space-between;
}
</style>
