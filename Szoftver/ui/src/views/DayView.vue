<template>
<div class="container">
    <DayPanel class="panel" />
    <TodoList class="panel" />
    <button class="add-ticket-btn" @click="showAddNewTicketModal = true">+ Add Ticket</button>
    <Modal :show="showAddNewTicketModal" title="Add New Ticket" confirmText="Add" @close="showAddNewTicketModal = false" @confirm="handleAddNewTicket">
        <div class="modal-content">
            <label for="ticket-name">Ticket Name</label>
            <input id="ticket-name" v-model="newTicket.name" placeholder="Enter ticket name" required />

            <label for="ticket-description">Description (optional)</label>
            <textarea id="ticket-description" v-model="newTicket.description" placeholder="Enter description"></textarea>

            <label for="ticket-priority">Priority (optional)</label>
            <input id="ticket-priority" v-model="newTicket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />

            <label for="ticket-start-time">Start Time (optional)</label>
            <input id="ticket-start-time" v-model="newTicket.startTime" type="time" />

            <label for="ticket-end-time">End Time (optional)</label>
            <input id="ticket-end-time" v-model="newTicket.endTime" type="time" />

            <label for="ticket-calendar-list">Select Calendar List</label>
            <select id="ticket-calendar-list" v-model="newTicket.calendarListId">
                <option v-for="list in calendarLists" :key="list.id" :value="list.id">{{ list.name }}</option>
            </select>
            <p v-if="!newTicket.calendarListId" class="error">Please select a calendar list before adding a ticket.</p>
            <p v-if="calendarLists.length === 0" class="error">No calendar lists available. Please create one first.</p>
        </div>
    </Modal>
</div>
</template>

<script>
import {
    ref,
    onMounted, // GPT generated
    onUnmounted // GPT generated
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
} from "@/utils/eventBus"; // GPT generated

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

        const handleAddNewTicket = async () => {
            if (!newTicket.value.calendarListId) {
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
                console.error
            );

            showAddNewTicketModal.value = false;

            // Emit the correct event based on the presence of startTime - GPT generated
            if (newTicket.value.startTime) {
                emitter.emit("newTicketCreatedWithTime"); // GPT generated - Ticket has a startTime
            } else {
                emitter.emit("newTicketCreatedWithoutTime"); // GPT generated - Ticket has no startTime
            }

            // Reset newTicket values to default after adding the ticket - GPT generated
            newTicket.value = {
                name: "",
                description: "",
                priority: null,
                startTime: "",
                endTime: "",
                calendarListId: null,
            };
        };

        // Function to reset currentDayId when a day is successfully deleted - GPT generated
        const handleDayDeletion = () => {
            currentDayId.value = null;
        };

        // Subscribe and unsubscribe from the event - GPT generated
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
