<template>
<div class="lists-container">
    <div class="header">
        <h2>To be scheduled</h2>
        <button class="add-button" @click="openModal">+</button>
    </div>

    <p v-if="loading">Loading...</p>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <div class="lists-content" v-if="calendarLists.length > 0">
        <div v-for="list in calendarLists" :key="list.id" class="list-item" :style="{ backgroundColor: list.color || '#CCCCCC' }">
            <p class="list-title">{{ list.name }}</p>

            <!-- Ticketek megjelenítése -->
            <div class="ticket-list" v-if="list.tickets && list.tickets.length > 0">
                <div v-for="ticket in list.tickets" :key="ticket.id" class="ticket-item">
                    <p><strong>{{ ticket.name }}</strong></p>
                    <p v-if="ticket.description">{{ ticket.description }}</p>
                    <p v-if="ticket.priority">Priority: {{ ticket.priority }}</p>
                    <button @click="deleteTicket(ticket.id)" class="delete-btn">Delete</button>
                </div>
            </div>
            <p v-else>No tickets.</p>

            <!-- Ticket hozzáadás gomb -->
            <button class="add-ticket-button" @click="openTicketModal(list.id)">+</button>
        </div>
    </div>
    <p v-else>No scheduled lists.</p>

    <!-- Új lista hozzáadás MODAL -->
    <Modal :show="showListModal" title="Új lista hozzáadása" confirmText="Hozzáadás" @close="showListModal = false" @confirm="addCalendarList">
        <div class="modal-content">
            <input v-model="newList.name" placeholder="Lista neve" />

            <div class="color-picker">
                <input v-model="newList.color" type="color" />
                <div class="color-preview" :style="{ backgroundColor: newList.color }"></div>
            </div>
        </div>
    </Modal>

    <!-- Új ticket hozzáadás MODAL -->
    <Modal :show="showTicketModal" title="Add New Ticket" confirmText="Add" @close="showTicketModal = false" @confirm="addTicket">
        <div class="modal-content">
            <label for="ticket-name">Ticket Name</label>
            <input id="ticket-name" v-model="newTicket.name" placeholder="Enter ticket name" required />

            <label for="ticket-description">Description (optional)</label>
            <textarea id="ticket-description" v-model="newTicket.description" placeholder="Enter description"></textarea>

            <label for="ticket-priority">Priority (optional)</label>
            <input id="ticket-priority" v-model="newTicket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />
        </div>
    </Modal>
</div>
</template>

<script>
import {
    ref,
    onMounted
} from "vue";
import api from "@/utils/config/axios-config";
import Modal from "@/components/Modal.vue";

export default {
    components: {
        Modal
    },
    setup() {
        const calendarLists = ref([]);
        const loading = ref(true);
        const errorMessage = ref("");
        const showTicketModal = ref(false);
        const selectedListId = ref(null);
        const showListModal = ref(false);
        const newList = ref({
            name: "",
            color: "#CCCCCC",
        });

        const newTicket = ref({
            name: "",
            description: "",
            startTime: "",
            endTime: "",
            priority: null
        });

        const openModal = () => {
            newList.value = {
                name: "",
                color: "#CCCCCC"
            };
            showListModal.value = true;
        };

        const addCalendarList = async () => {
            try {
                const user = JSON.parse(localStorage.getItem("user"));
                if (!user || !user.defaultCalendarId) {
                    throw new Error("Nincs alapértelmezett naptár beállítva.");
                }

                const calendarId = user.defaultCalendarId;
                const response = await api.post("/api/CalendarLists", {
                    name: newList.value.name,
                    color: newList.value.color,
                    calendarId
                });

                calendarLists.value.push(response.data);
                showListModal.value = false;
                console.log("Modal state:", showListModal.value);
            } catch (error) {
                console.error("Error adding list:", error);
                errorMessage.value = "Nem sikerült hozzáadni a listát.";
            }
        };

        const fetchCalendarLists = async () => {
            try {
                const user = JSON.parse(localStorage.getItem("user"));
                if (!user || !user.defaultCalendarId) {
                    throw new Error("No default calendar set.");
                }

                const calendarId = user.defaultCalendarId;
                const response = await api.get(`/api/CalendarLists/calendar/${calendarId}`);
                calendarLists.value = response.data.map(list => ({
                    ...list,
                    tickets: list.tickets || []
                }));
            } catch (error) {
                console.error("Error loading calendar lists:", error);
                errorMessage.value = error.response?.data || "Failed to load lists.";
            } finally {
                loading.value = false;
            }
        };

        const openTicketModal = (listId) => {
            selectedListId.value = listId;
            newTicket.value = {
                name: "",
                description: "",
                startTime: "",
                endTime: "",
                priority: null
            };
            showTicketModal.value = true;
        };

        const addTicket = async () => {
            if (!newTicket.value.name.trim()) {
                errorMessage.value = "Ticket name is required.";
                return;
            }

            try {
                // Ha a dátummezők üresek, akkor `null`-t küldünk, nem üres stringet
                const ticketData = {
                    name: newTicket.value.name,
                    description: newTicket.value.description || null,
                    startTime: null,
                    endTime: null,
                    priority: newTicket.value.priority || null,
                    calendarListId: selectedListId.value,
                    currentParentType: "CalendarList",
                    parentId: selectedListId.value
                };

                const response = await api.post("/api/Tickets", ticketData);

                // Ticket hozzáadása a megfelelő listához
                const list = calendarLists.value.find(list => list.id === selectedListId.value);
                if (list) {
                    list.tickets.push(response.data);
                }

                showTicketModal.value = false;
            } catch (error) {
                console.error("Error adding ticket:", error);
                errorMessage.value = error.response?.data || "Failed to add ticket.";
            }
        };

        const deleteTicket = async (ticketId) => {
            try {
                await api.delete(`/api/tickets/${ticketId}`);
                calendarLists.value.forEach(list => {
                    list.tickets = list.tickets.filter(ticket => ticket.id !== ticketId);
                });
            } catch (error) {
                console.error("Error deleting ticket:", error);
                errorMessage.value = "Failed to delete ticket.";
            }
        };

        const formatDate = (dateString) => {
            if (!dateString) return "";
            return new Date(dateString).toLocaleDateString();
        };

        onMounted(fetchCalendarLists);

        return {
            calendarLists,
            loading,
            errorMessage,
            showListModal,
            newList,
            showTicketModal,
            newTicket,
            openModal,
            addCalendarList,
            openTicketModal,
            addTicket,
            deleteTicket,
            formatDate
        };
    }

};
</script>

<style scoped>
.lists-container {
    width: 30vw;
    height: 90vh;
    background: #c8e6c9;
    padding: 15px;
    border-radius: 10px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}

.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
}

.add-button {
    background: #4caf50;
    color: white;
    border: none;
    padding: 5px 10px;
    font-size: 18px;
    border-radius: 50%;
    cursor: pointer;
}

.lists-content {
    display: flex;
    flex-direction: row;
    gap: 10px;
    overflow-x: auto;
    white-space: nowrap;
    padding-bottom: 10px;
}

.list-item {
    padding: 10px;
    border-radius: 5px;
    box-shadow: 2px 2px 5px rgba(0, 0, 0, 0.1);
    min-width: 200px;
    flex-shrink: 0;
    text-align: center;
}

.ticket-item {
    padding: 5px;
    border: 2px solid black;
}

.ticket-item:last-child {
    border-bottom: none;
}

.modal-content {
    display: flex;
    flex-direction: column;
    gap: 10px;
    align-items: center;
}

.color-picker {
    display: flex;
    align-items: center;
    gap: 10px;
}

.color-preview {
    width: 30px;
    height: 30px;
    border-radius: 5px;
    border: 1px solid #ccc;
}

label {
    font-weight: bold;
}

.error {
    color: red;
}
</style>
