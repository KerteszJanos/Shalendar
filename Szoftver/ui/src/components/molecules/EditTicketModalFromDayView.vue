<template>
  <Modal :show="show" title="Edit Ticket" confirmText="Save" @close="closeModal" @confirm="saveChanges">
    <div class="modal-content">
      <label for="edit-ticket-name">Ticket Name</label>
      <input id="edit-ticket-name" v-model="ticket.name" placeholder="Enter ticket name" required />

      <label for="edit-ticket-description">Description</label>
      <textarea id="edit-ticket-description" v-model="ticket.description" placeholder="Enter description"></textarea>

      <label for="edit-ticket-priority">Priority</label>
      <input id="edit-ticket-priority" v-model="ticket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />

      <label for="edit-ticket-startTime">Start Time</label>
      <input id="edit-ticket-startTime" v-model="ticket.startTime" type="time" />

      <label for="edit-ticket-endTime">End Time</label>
      <input id="edit-ticket-endTime" v-model="ticket.endTime" type="time" />
    </div>
  </Modal>
</template>

<script>
import { ref, watch } from "vue";
import Modal from "@/components/molecules/Modal.vue";
import api from "@/utils/config/axios-config";
import { emitter } from "@/utils/eventBus";

export default {
  components: { Modal },
  props: {
    show: Boolean,
    ticketData: Object,
  },
  emits: ["update:show", "ticketUpdated"],
  setup(props, { emit }) {
    const ticket = ref({ ...props.ticketData });
    const previousStartTime = ref(null);

    // Update previousStartTime when modal opens
    watch(
      () => props.ticketData,
      (newData) => {
        ticket.value = { ...newData };
        previousStartTime.value = newData.startTime;
      },
      { deep: true, immediate: true }
    );

    const closeModal = () => {
      emit("update:show", false);

      // Emit if startTime changed from null to valid value
      if (!previousStartTime.value && ticket.value.startTime) {
        emitter.emit("ticketTimeSet");
      }

      // Emit if startTime changed from valid value to null
      if (previousStartTime.value && !ticket.value.startTime) {
        emitter.emit("ticketTimeUnSet");
      }
    };

    const saveChanges = async () => {
      try {
        await api.put(`/api/Tickets/${ticket.value.id}`, {
          id: ticket.value.id,
          name: ticket.value.name,
          description: ticket.value.description,
          priority: ticket.value.priority,
          startTime: ticket.value.startTime || null,
          endTime: ticket.value.endTime || null,
        });

        emit("ticketUpdated");
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