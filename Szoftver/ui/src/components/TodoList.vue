<template>
    <div class="container">
      <div class="todo-list">
        <div class="header">Todo List</div>
        <p v-if="loading">Loading...</p>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <div class="task-list">
          <div v-for="ticket in tickets" :key="ticket.id" class="task" :style="{ backgroundColor: ticket.color || '#ffffff' }">
            <p><strong>{{ ticket.name }}</strong></p>
            <p v-if="ticket.description">{{ ticket.description }}</p>
            <p v-if="ticket.priority">Priority: {{ ticket.priority }}</p>
            <button @click="deleteTicket(ticket.id)" class="delete-btn">Delete</button>
          </div>
        </div>
      </div>
    </div>
</template>

<script>
import { ref, onMounted } from "vue";
import api from "@/utils/config/axios-config";

export default {
    setup() {
        const tickets = ref([]);
        const loading = ref(true);
        const errorMessage = ref("");
        const calendarId = ref(null);

        // API hívás megfelelő calendarId alapján
        const fetchTickets = async () => {
            loading.value = true;
            errorMessage.value = "";

            try {
                // Ellenőrizzük, hogy van-e mentett calendarId a localStorage-ban
                const storedCalendarId = localStorage.getItem("calendarId");
                if (!storedCalendarId) {
                    throw new Error("Nincs mentett calendarId a localStorage-ban.");
                }

                calendarId.value = storedCalendarId;

                const response = await api.get(`/api/Tickets/todolist/${calendarId.value}`);
                tickets.value = response.data.map(ticket => ({
                    ...ticket,
                    color: ticket.color || "#ffffff" // Ha nincs szín, alapértelmezett fehér
                }));
            } catch (error) {
                console.error("Error loading tickets:", error);
                errorMessage.value = "Failed to load tickets.";
            } finally {
                loading.value = false;
            }
        };

        // Ticket törlése
        const deleteTicket = async (ticketId) => {
            try {
                await api.delete(`/api/tickets/${ticketId}`);
                tickets.value = tickets.value.filter(ticket => ticket.id !== ticketId);
            } catch (error) {
                console.error("Error deleting ticket:", error);
                errorMessage.value = "Failed to delete ticket.";
            }
        };

        onMounted(fetchTickets);

        return {
            tickets,
            loading,
            errorMessage,
            deleteTicket,
            fetchTickets,
            calendarId,
        };
    }
};
</script>

<style scoped>
.container {
    display: flex;
    height: 100vh;
}

.todo-list {
    flex: 1;
    display: flex;
    flex-direction: column;
    position: relative;
    padding: 20px;
    border-right: none;
}

.header {
    font-size: 1.5rem;
    font-weight: bold;
    text-align: center;
    margin-bottom: 10px;
    padding: 10px;
    background: linear-gradient(to right, #9dc1b7, #b1dbd3);
    border-radius: 8px;
}

.task-list {
    display: flex;
    flex-direction: column;
    gap: 10px;
}

.task {
    padding: 10px;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    transition: background 0.3s ease-in-out;
}

.delete-btn {
    background: red;
    color: white;
    border: none;
    padding: 5px 10px;
    cursor: pointer;
    border-radius: 5px;
    margin-top: 5px;
}
</style>
