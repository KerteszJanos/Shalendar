<template>
<div class="container">
    <DayPanel class="panel" />
    <TodoList class="panel" />
    <button class="add-ticket-btn" @click="showAddNewTicketModal = true">+ Add Ticket</button>
    <Modal :show="showAddNewTicketModal" title="Add New Ticket" confirmText="Add" @close="showAddNewTicketModal = false" @confirm="handleAddNewTicket">
        <div class="modal-content">
            <label for="ticket-name">Ticket Name</label>
            <input id="ticket-name" v-model="newTicket.name" placeholder="Enter ticket name" required />
            <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

            <label for="ticket-description">Description (optional)</label>
            <textarea id="ticket-description" v-model="newTicket.description" placeholder="Enter description"></textarea>

            <label for="ticket-priority">Priority (optional)</label>
            <input id="ticket-priority" v-model="newTicket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />

            <label for="ticket-start-time">Start Time (optional)</label>
            <input id="ticket-start-time" v-model="newTicket.startTime" type="time" />
            <p v-if="timeError" class="error">{{ timeError }}</p>

            <label for="ticket-end-time">End Time (optional)</label>
            <input id="ticket-end-time" v-model="newTicket.endTime" type="time" />

            <label for="ticket-calendar-list">Select Calendar List</label>
            <select id="ticket-calendar-list" v-model="newTicket.calendarListId">
                <option v-for="list in calendarLists" :key="list.id" :value="list.id">{{ list.name }}</option>
            </select>
            <p v-if="calendarListError" class="error">{{ calendarListError }}</p>
        </div>
    </Modal>

</div>
</template>

<script>
import {
    ref,
    onMounted,
    onUnmounted
} from "vue";
import {
    useRoute
} from "vue-router";
import DayPanel from "@/components/organisms/ScheduledPanel.vue";
import TodoList from "@/components/organisms/TodoList.vue";
import Modal from "@/components/molecules/Modal.vue";
import {
    addNewTicket
} from "@/components/atoms/AddNewTicket";
import api from "@/utils/config/axios-config";
import {
    emitter
} from "@/utils/eventBus";
import {
    validateNameField,
    validateTimeFieldsBothRequired
} from "@/components/atoms/ValidateModalInputFields";

export default {
    components: {
        DayPanel,
        TodoList,
        Modal,
    },
    setup() {
        const showAddNewTicketModal = ref(false);
        const newTicket = ref({
            name: "",
            description: "",
            priority: null,
            startTime: "",
            endTime: "",
            calendarListId: null,
        });
        const route = useRoute();
        const currentDayId = ref(null);
        const calendarId = ref(localStorage.getItem("calendarId"));
        const calendarLists = ref([]);
        const errorMessage = ref("");
        const calendarListError = ref("");
        const timeError = ref("");

        const handleAddNewTicket = async () => {
            errorMessage.value = "";
            calendarListError.value = "";
            timeError.value = "";

            if (!newTicket.value.calendarListId) {
                calendarListError.value = "Please select a calendar list before adding a ticket.";
                return;
            }

            const nameValidationError = validateNameField(newTicket.value.name);
            if (nameValidationError) {
                errorMessage.value = nameValidationError;
                return;
            }

            const timeValidationError = validateTimeFieldsBothRequired(newTicket.value.startTime, newTicket.value.endTime);
            if (timeValidationError) {
                timeError.value = timeValidationError;
                return;
            }

            if (!currentDayId.value) {
                currentDayId.value = await createDay(route.params.date, calendarId.value);
            }

            await addNewTicket({
                    name: newTicket.value.name,
                    description: newTicket.value.description || null,
                    priority: newTicket.value.priority || null,
                    startTime: newTicket.value.startTime || null,
                    endTime: newTicket.value.endTime || null,
                    calendarListId: newTicket.value.calendarListId,
                    currentParentType: newTicket.value.startTime ? "ScheduledList" : "TodoList",
                    parentId: currentDayId.value,
                },
                null,
                [],
                showAddNewTicketModal,
                (msg) => (errorMessage.value = msg)
            );

            if (!errorMessage.value && !calendarListError.value && !timeError.value) {
                showAddNewTicketModal.value = false;

                if (newTicket.value.startTime) {
                    emitter.emit("newTicketCreatedWithTime");
                } else {
                    emitter.emit("newTicketCreatedWithoutTime");
                }

                newTicket.value = {
                    name: "",
                    description: "",
                    priority: null,
                    startTime: "",
                    endTime: "",
                    calendarListId: null,
                };
            }
        };

        const fetchDayId = async () => {
            try {
                const response = await api.get(`/api/Days/${route.params.date}/${calendarId.value}`);
                currentDayId.value = response.data?.id || null;
            } catch (error) {
                console.error("Error fetching day ID:", error);
            }
        };

        const fetchCalendarLists = async () => {
            try {
                const response = await api.get(`/api/CalendarLists/calendar/${calendarId.value}`);
                calendarLists.value = response.data;
            } catch (error) {
                console.error("Error fetching calendar lists:", error);
            }
        };

        fetchDayId();
        fetchCalendarLists();

        const createDay = async (date, calendarId) => {
            try {
                const response = await api.post("/api/Days/create", {
                    date,
                    calendarId
                });
                return response.data.id;
            } catch (error) {
                console.error("Error creating new day:", error);
                return null;
            }
        };

        const handleDayDeletion = () => {
            currentDayId.value = null;
        };

        onMounted(() => {
            emitter.on("successfulDayDelete", handleDayDeletion);
        });

        onUnmounted(() => {
            emitter.off("successfulDayDelete", handleDayDeletion);
        });

        return {
            showAddNewTicketModal,
            newTicket,
            handleAddNewTicket,
            currentDayId,
            calendarId,
            calendarLists,
            errorMessage,
            calendarListError,
            timeError,
        };
    },
};
</script>

<style scoped>
.container {
    display: flex;
    height: 100vh;
    position: relative;
}

.panel {
    flex: 1;
    min-width: 50%;
}

.add-ticket-btn {
    position: absolute;
    bottom: 20px;
    right: 20px;
    background-color: #4caf50;
    color: white;
    border: none;
    padding: 10px 15px;
    font-size: 16px;
    border-radius: 5px;
    cursor: pointer;
}

.add-ticket-btn:hover {
    background-color: #388e3c;
}

.modal-content {
    display: flex;
    flex-direction: column;
    gap: 10px;
    align-items: center;
}

.error {
    color: red;
    font-weight: bold;
    text-align: center;
}
</style>
