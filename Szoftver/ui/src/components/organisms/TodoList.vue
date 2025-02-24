<template>
<div class="container">
    <div class="todo-list">
        <div class="header">{{ formattedDate }}</div>
        <p v-if="loading">Loading...</p>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <!-- Using Vue 3 slot syntax for draggable -->
        <draggable v-model="tickets" @end="onDragEnd" group="tickets" itemKey="id">
            <template #item="{ element }">
                <div class="task" :style="{ backgroundColor: element.backgroundColor }">
                    <p><strong>{{ element.name }}</strong></p>
                    <p v-if="element.description">{{ element.description }}</p>
                    <p v-if="element.priority">Priority: {{ element.priority }}</p>
                    <button @click="deleteTicket(element.id)" class="delete-btn">Delete</button>
                </div>
            </template>
        </draggable>
    </div>
</div>
</template>

  
<script>
import {
    ref,
    onMounted,
    watch,
    computed
} from "vue";
import {
    useRoute
} from "vue-router";
import api from "@/utils/config/axios-config";
import draggable from "vuedraggable";

export default {
    components: {
        draggable,
    },
    setup() {
        const route = useRoute();
        const tickets = ref([]);
        const loading = ref(true);
        const errorMessage = ref("");
        const calendarId = ref(null);

        // Formázza a kiválasztott dátumot
        const formattedDate = computed(() => {
            const date = new Date(route.params.date);
            return date.toLocaleDateString("hu-HU", {
                year: "numeric",
                month: "long",
                day: "numeric",
            });
        });

        // Ticketek betöltése a dátum és a calendarId alapján
        const fetchTickets = async () => {
            loading.value = true;
            errorMessage.value = "";
            try {
                const selectedDate = route.params.date;
                // Ellenőrzi, hogy van-e mentett calendarId a localStorage-ban
                const storedCalendarId = localStorage.getItem("calendarId");
                if (!storedCalendarId) {
                    throw new Error("Nincs mentett calendarId a localStorage-ban.");
                }
                calendarId.value = storedCalendarId;
                const response = await api.get(`/api/Tickets/todolist/${selectedDate}/${calendarId.value}`);
                // Map-elés és rendezés a currentPosition alapján
                tickets.value = response.data
                    .map(ticket => ({
                        ...ticket,
                        backgroundColor: ticket.color || "#ffffff", // Alapértelmezett fehér szín, ha nincs megadva
                    }))
                    .sort((a, b) => a.currentPosition - b.currentPosition);
            } catch (error) {
                console.error("Error loading tickets:", error);
                errorMessage.value = "Failed to load tickets.";
            } finally {
                loading.value = false;
            }
        };

        // Ticket törlése, majd újrasorrendeli a ticketeket
        const deleteTicket = async (ticketId) => {
            try {
                await api.delete(`/api/tickets/${ticketId}`);
                // After deletion, update the tickets array
                tickets.value = tickets.value.filter(ticket => ticket.id !== ticketId);

                // Only update positions and call reorder if the tickets array is not empty
                if (tickets.value.length > 0) {
                    // Recalculate positions: position = index + 1
                    tickets.value.forEach((ticket, index) => {
                        ticket.currentPosition = index + 1;
                    });
                    // Prepare orderUpdates array and update the backend
                    const orderUpdates = tickets.value.map((ticket, index) => ({
                        ticketId: ticket.id,
                        newPosition: index + 1,
                    }));
                    await api.put('/api/Tickets/reorder', orderUpdates);
                }
            } catch (error) {
                console.error("Error deleting ticket:", error);
                errorMessage.value = "Failed to delete ticket.";
            }
        };

        // Drag and drop művelet befejezése után frissítjük a pozíciókat
        const onDragEnd = async () => {
            // Újraszámoljuk a pozíciókat: pozíció = index + 1
            const orderUpdates = tickets.value.map((ticket, index) => {
                ticket.currentPosition = index + 1;
                return {
                    ticketId: ticket.id,
                    newPosition: index + 1
                };
            });
            try {
                await api.put('/api/Tickets/reorder', orderUpdates);
            } catch (error) {
                console.error("Error updating ticket positions:", error);
                errorMessage.value = "Failed to update ticket positions.";
            }
        };

        watch(() => route.params.date, fetchTickets);
        onMounted(fetchTickets);

        return {
            tickets,
            loading,
            errorMessage,
            formattedDate,
            deleteTicket,
            fetchTickets,
            calendarId,
            onDragEnd,
        };
    },
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

.task {
    padding: 10px;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    transition: background-color 0.3s ease-in-out;
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
