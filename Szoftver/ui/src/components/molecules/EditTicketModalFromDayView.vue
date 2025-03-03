<template>
<Modal :show="show" title="Edit Ticket" confirmText="Save" @close="closeModal" @confirm="saveChanges">
    <div class="modal-content">
        <label for="edit-ticket-name">Ticket Name</label>
        <input id="edit-ticket-name" v-model="ticket.name" placeholder="Enter ticket name" required />
        <p v-if="errors.name" class="error">{{ errors.name }}</p>

        <label for="edit-ticket-description">Description</label>
        <textarea id="edit-ticket-description" v-model="ticket.description" placeholder="Enter description"></textarea>

        <label for="edit-ticket-priority">Priority</label>
        <input id="edit-ticket-priority" v-model="ticket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />

        <label for="edit-ticket-startTime">Start Time</label>
        <input id="edit-ticket-startTime" v-model="ticket.startTime" type="time" />
        <p v-if="errors.time" class="error">{{ errors.time }}</p>

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
    validateTimeFieldsBothRequiredOrEmpty
} from "@/components/atoms/ValidateModalInputFields";

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
        const ticket = ref({
            ...props.ticketData
        });
        const previousStartTime = ref(null);
        const errors = ref({
            name: "",
            time: ""
        });
        const permissionError = ref("");

        // Update previousStartTime when modal opens
        watch(
            () => props.show,
            (newValue) => {
                if (newValue) {
                    ticket.value = {
                        ...props.ticketData
                    };
                    errors.value = {
                        name: "",
                        time: ""
                    }; // Reset errors when modal opens
                    previousStartTime.value = props.ticketData.startTime;
                }
            }
        );

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
            errors.value.name = validateNameField(ticket.value.name);
            errors.value.time = validateTimeFieldsBothRequiredOrEmpty(ticket.value.startTime, ticket.value.endTime);

            if (errors.value.name || errors.value.time) {
                return;
            }

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
                if (error.response && error.response.status === 403) {
                    permissionError.value = `Access denied: ${error.response.data?.message || "You do not have permission."}`;
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    console.error("Error updating ticket:", error);
                }
            }
        };

        return {
            ticket,
            errors,
            closeModal,
            saveChanges,
            permissionError
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
