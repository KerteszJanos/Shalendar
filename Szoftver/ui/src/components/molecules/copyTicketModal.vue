<template>
<Modal :show="show" title="Copy Ticket to Calendar" confirmText="Copy" @close="closeModal" @confirm="copyTicket">
    <div class="modal-content">
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

        <div v-if="loading" class="loading">Loading calendars...</div>
        <div v-else-if="calendars.length === 0" class="no-access">
            No calendars found with owner or write permissions.
        </div>
        <div v-else>
            <label for="calendar-select" class="required-label">Select Calendar</label>
            <div class="custom-dropdown">
                <div class="selected-option" @click="toggleDropdown" :title="selectedCalendarName">
                    {{ selectedCalendarName || "Select Calendar" }}
                </div>
                <ul v-if="dropdownOpen" class="options-list">
                    <li v-for="calendar in calendars" :key="calendar.id" :class="{ 'selected-calendar': selectedCalendar === calendar.id }" @click="selectCalendar(calendar)" :title="calendar.name">
                        {{ calendar.name }}
                    </li>
                </ul>
            </div>
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
        const selectedCalendarName = ref("");
        const dropdownOpen = ref(false);
        const errorMessage = ref("");
        const loading = ref(false);

        watch(
            () => props.show,
            (newValue) => {
                if (newValue) {
                    selectedCalendar.value = null;
                    selectedCalendarName.value = "";
                    dropdownOpen.value = false;
                    getAccessibleCalendars();
                }
            }
        );

        const getAccessibleCalendars = async () => {
            loading.value = true;
            errorMessage.value = "";
            try {
                const response = await api.get(`/api/Calendars/accessible`);
                const currentCalendarId = localStorage.getItem("calendarId");
                calendars.value = response.data.filter(calendar => calendar.id != currentCalendarId);
            } catch (error) {
                console.error("Error fetching calendars:", error);
            } finally {
                loading.value = false;
            }
        };

        const toggleDropdown = () => {
            dropdownOpen.value = !dropdownOpen.value;
        };

        const selectCalendar = (calendar) => {
            selectedCalendar.value = calendar.id;
            selectedCalendarName.value = calendar.name;
            dropdownOpen.value = false;
        };

        const copyTicket = async () => {
            if (!selectedCalendar.value) {
                setErrorMessage(errorMessage, "Please select a calendar.");
                return;
            }

            try {
                if (props.ticketId) {
                    await api.post(`/api/tickets/copy-ticket`, null, {
                        params: {
                            ticketId: props.ticketId,
                            calendarId: selectedCalendar.value,
                            date: props.date || null
                        }
                    });
                } else {
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
            selectedCalendarName,
            dropdownOpen,
            toggleDropdown,
            selectCalendar,
            errorMessage,
            loading,
            closeModal,
            copyTicket
        };
    }
};
</script>

<style scoped>
.custom-dropdown {
    position: relative;
    width: 100%;
}

.selected-option {
    padding: 8px;
    border: 2px solid #0B6477;
    border-radius: 5px;
    font-size: 16px;
    cursor: pointer;
    text-align: center;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.options-list {
    position: absolute;
    width: 100%;
    border: 1px solid #ccc;
    border-radius: 5px;
    background-color: white;
    list-style: none;
    padding: 0;
    margin: 0;
    max-height: 200px;
    overflow-y: auto;
    z-index: 1000;
    /* biztosítja, hogy ne takarja más elem */
}

.options-list li {
    padding: 8px;
    cursor: pointer;
    text-align: center;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.options-list li:hover,
.options-list .selected-calendar {
    background-color: #f0f0f0;
}

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
