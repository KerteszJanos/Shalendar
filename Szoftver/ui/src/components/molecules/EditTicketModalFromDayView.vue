<!--
  Modal for editing a ticket's name, description, priority, and time fields.
  Includes validation and saves changes via API.
-->

<template>
<Modal :show="show" title="Edit Ticket" confirmText="Save" @close="closeModal" @confirm="saveChanges">
    <div class="modal-content">
        <label for="edit-ticket-name" class="required-label">Ticket Name</label>
        <input id="edit-ticket-name" v-model="ticket.name" placeholder="Enter ticket name" required />
        <p v-if="nameError" class="error">{{ nameError }}</p>

        <label for="edit-ticket-description">Description</label>
        <textarea id="edit-ticket-description" v-model="ticket.description" placeholder="Enter description"></textarea>

        <label for="edit-ticket-priority">Priority</label>
        <input id="edit-ticket-priority" v-model="ticket.priority" type="number" min="1" max="9" placeholder="Enter priority (1-9)" />
        <p v-if="priorityError" class="error">{{ priorityError }}</p>

        <label for="edit-ticket-startTime">Start Time</label>
        <input id="edit-ticket-startTime" v-model="ticket.startTime" type="time" />
        <p v-if="timeError" class="error">{{ timeError }}</p>

        <label for="edit-ticket-endTime">End Time</label>
        <input id="edit-ticket-endTime" v-model="ticket.endTime" type="time" />
        <p v-if="permissionError" class="error">{{ permissionError }}</p>
    </div>
</Modal>
</template>

<script>
import {
    ref,
    watch
} from "vue";
import Modal from "@/components/molecules/Modal.vue";
import api from "@/utils/config/axios-config";
import {
    emitter
} from "@/utils/eventBus";
import {
    validateNameField,
    validateTimeFieldsBothRequiredOrEmpty,
    validatePriorityField
} from "@/components/atoms/ValidateModalInputFields";
import {
    setErrorMessage
} from "@/utils/errorHandler";

export default {
    components: {
        Modal
    },
    props: {
        show: Boolean,
        ticketData: Object,
    },
    emits: ["update:show", "ticketUpdated"],
    setup(props, {
        emit
    }) {
        // ---------------------------------
        // Reactive state		           |
        // ---------------------------------
        const ticket = ref({
            ...props.ticketData
        });
        const previousStartTime = ref(null);
        const nameError = ref("");
        const timeError = ref("");
        const permissionError = ref("")
        const priorityError = ref("");

        // ---------------------------------
        // Methods		                   |
        // ---------------------------------
        const closeModal = () => {
            emit("update:show", false);

            if (!previousStartTime.value && ticket.value.startTime) {
                emitter.emit("ticketTimeSet");
            }
            if (previousStartTime.value && !ticket.value.startTime) {
                emitter.emit("ticketTimeUnSet");
            }
        };

        const saveChanges = async () => {
            setErrorMessage(nameError, validateNameField(ticket.value.name));
            setErrorMessage(timeError, validateTimeFieldsBothRequiredOrEmpty(ticket.value.startTime, ticket.value.endTime));
            if (ticket.value.priority !== null) {
                const priorityValidationError = validatePriorityField(ticket.value.priority);
                if (priorityValidationError) {
                    setErrorMessage(priorityError, priorityValidationError);
                    return;
                }
            }

            if (nameError.value || timeError.value) {
                return;
            }

            try {
                await api.put(`/api/Tickets/updateTicket`, {
                    id: ticket.value.id,
                    name: ticket.value.name,
                    description: ticket.value.description,
                    priority: ticket.value.priority || null,
                    startTime: ticket.value.startTime || null,
                    endTime: ticket.value.endTime || null,
                });

                emit("ticketUpdated");
                closeModal();
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(permissionError, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    console.error("Error updating ticket:", error);
                }
            }
        };

        // ---------------------------------
        // Lifecycle hooks		           |
        // ---------------------------------
        watch(
            () => props.show,
            (newValue) => {
                if (newValue) {
                    ticket.value = {
                        ...props.ticketData
                    };
                    nameError.value = "";
                    timeError.value = "";
                    permissionError.value = "";
                    previousStartTime.value = props.ticketData.startTime;
                }
            }
        );

        return {
            ticket,
            closeModal,
            saveChanges,
            nameError,
            timeError,
            permissionError,
            priorityError
        };
    },
};
</script>

<style scoped>
.error {
    color: red;
    font-size: 0.9rem;
    margin-top: 4px;
}
</style>
