<template>
  <Modal :show="show" title="Edit Ticket" confirmText="Save" @close="closeModal" @confirm="saveChanges"> <!-- Gpt generated -->
    <div class="modal-content"> <!-- Gpt generated -->
      <label for="edit-ticket-name">Ticket Name</label>
      <input id="edit-ticket-name" v-model="ticket.name" placeholder="Enter ticket name" required />

      <label for="edit-ticket-description">Description</label>
      <textarea id="edit-ticket-description" v-model="ticket.description" placeholder="Enter description"></textarea>

      <label for="edit-ticket-priority">Priority</label>
      <input id="edit-ticket-priority" v-model="ticket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />

      <label for="edit-ticket-startTime">Start Time</label> <!-- Gpt generated -->
      <input id="edit-ticket-startTime" v-model="ticket.startTime" type="time" /> <!-- Gpt generated -->

      <label for="edit-ticket-endTime">End Time</label> <!-- Gpt generated -->
      <input id="edit-ticket-endTime" v-model="ticket.endTime" type="time" /> <!-- Gpt generated -->
    </div>
  </Modal>
</template>

<script>
import { ref, watch } from "vue";
import Modal from "@/components/molecules/Modal.vue";
import api from "@/utils/config/axios-config";

export default {
components: { Modal },
props: {
  show: Boolean, // Gpt generated
  ticketData: Object, // Gpt generated
},
emits: ["update:show", "ticketUpdated"], // Gpt generated
setup(props, { emit }) {
  const ticket = ref({ ...props.ticketData });

  watch(
    () => props.ticketData,
    (newData) => {
      ticket.value = { ...newData };
    },
    { deep: true }
  );

  const closeModal = () => { // Gpt generated
    emit("update:show", false);
  };

  const saveChanges = async () => { // Gpt generated
    try {
      await api.put(`/api/Tickets/${ticket.value.id}`, {
        id: ticket.value.id,
        name: ticket.value.name,
        description: ticket.value.description,
        priority: ticket.value.priority,
        startTime: ticket.value.startTime, // Gpt generated
        endTime: ticket.value.endTime, // Gpt generated
      });

      emit("ticketUpdated"); // Gpt generated
      closeModal();
    } catch (error) {
      console.error("Error updating ticket:", error);
    }
  };

  return {
    ticket,
    closeModal,
    saveChanges,
  };
},
};
</script>
