<template>
<Modal :show="show" title="Copy Ticket to Calendar" confirmText="Copy" @close="closeModal" @confirm="copyTicket">
    <div class="modal-content">
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

        <div v-if="loading" class="loading">Loading calendars...</div>
        <div v-else-if="calendars.length === 0" class="no-access">
            No calendars found with owner or write permissions.
        </div>
        <div v-else>
            <label for="calendar-select">Select Calendar</label>
            <select id="calendar-select" v-model="selectedCalendar">
                <option v-for="calendar in calendars" :key="calendar.id" :value="calendar.id">
                    {{ calendar.name }}
                </option>
            </select>
        </div>
    </div>
</Modal>
</template>

    
<script>
import {
    ref,
    watch
} from "vue";
import api from "@/utils/config/axios-config";
import Modal from "@/components/molecules/Modal.vue";
import {
    setErrorMessage
} from "@/utils/errorHandler";

export default {
    components: {
        Modal
    },
    props: {
        show: Boolean,
        ticketId: Number,
        date: String
    },
    emits: ["update:show"],
    setup(props, {
        emit
    }) {
        const calendars = ref([]);
        const selectedCalendar = ref(null);
        const errorMessage = ref("");
        const loading = ref(false);

        watch(
            () => props.show,
            (newValue) => {
                if (newValue) {
                    selectedCalendar.value = null;
                    getAccessibleCalendars();
                }
            }
        );

        const getAccessibleCalendars = async () => {
            loading.value = true;
            errorMessage.value = "";
            try {
                const response = await api.get(`/api/Calendars/accessible`);
                const currentCalendarId = localStorage.getItem("calendarId"); // GPT generated
                calendars.value = response.data.filter(calendar => calendar.id != currentCalendarId); // GPT generated
            } catch (error) {
                console.error("Error fetching calendars:", error);
            } finally {
                loading.value = false;
            }
        };

        const copyTicket = async () => {
            if (!selectedCalendar.value) {
                setErrorMessage(errorMessage, "Please select a calendar.");
                return;
            }

            try {
                if (props.ticketId) { // GPT generated - ellenőrzés ticketId alapján
                    await api.post(`/api/tickets/copy-ticket`, null, {
                        params: {
                            ticketId: props.ticketId,
                            calendarId: selectedCalendar.value,
                            date: props.date || null
                        }
                    });
                } else { // GPT generated - Ha nincs ticketId
                    await api.post(`/api/Calendars/copy-all-tickets`, null, {
                        params: {
                            calendarId: selectedCalendar.value
                        }
                    });
                }

                errorMessage.value = "";
                closeModal();
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(errorMessage, "Failed to copy ticket.");
                }
                console.error("Error copying ticket:", error);
            }
        };

        const closeModal = () => {
            emit("update:show", false);
        };

        return {
            calendars,
            selectedCalendar,
            errorMessage,
            loading,
            closeModal,
            copyTicket
        };
    }
};
</script>

    
<style scoped>
.loading {
    font-size: 14px;
    color: gray;
    margin-bottom: 10px;
}

.no-access {
    font-size: 14px;
    color: gray;
    margin-bottom: 10px;
}

label {
    font-weight: bold;
}

.error {
    color: red;
    font-size: 14px;
    margin-bottom: 10px;
}
</style>
