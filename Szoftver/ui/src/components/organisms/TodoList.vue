<template>
<div class="container">
    <div class="todo-list">
        <div class="header">{{ formattedDate }}</div>
        <p v-if="loading">Loading...</p>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <draggable v-model="tickets" @end="onDragEnd" group="tickets" itemKey="id">
            <template #item="{ element }">
                <div class="ticket" :style="{ backgroundColor: element.backgroundColor }" @click="openEditTicketModalFromDayView(element)">
                    <p><strong>{{ element.name }}</strong></p>
                    <p v-if="element.description">{{ element.description }}</p>
                    <p v-if="element.priority">Priority: {{ element.priority }}</p>
                    <button @click.stop="handleDelete(element.id)" class="delete-btn">Delete</button>
                    <button @click.stop="handleSendBack(element.id)" class="send-back-btn">Send Back</button>
                </div>
            </template>
        </draggable>
    </div>

    <EditTicketModalFromDayView :show="showEditTicketModalFromDayView" :ticketData="editedTicket" @update:show="showEditTicketModalFromDayView = $event" @ticketUpdated="fetchTickets" />
</div>
</template>

<script>
import {
    ref,
    onMounted,
    onUnmounted,
    watch,
    computed
} from "vue";
import {
    useRoute
} from "vue-router";
import api from "@/utils/config/axios-config";
import draggable from "vuedraggable";
import {
    updateTicketOrder
} from "@/components/atoms/updateTicketOrder";
import {
    deleteTicket
} from "@/components/atoms/deleteTicket";
import {
    sendBackToCalendarList
} from "@/components/atoms/SendBackToCalenderList";
import {
    tryDeleteDay
} from "@/components/atoms/TryDeleteDay";
import EditTicketModalFromDayView from "@/components/molecules/EditTicketModalFromDayView.vue";
import {
    emitter
} from "@/utils/eventBus";

export default {
    components: {
        draggable,
        EditTicketModalFromDayView
    },
    setup() {
        const route = useRoute();
        const tickets = ref([]);
        const loading = ref(true);
        const errorMessage = ref("");
        const calendarId = ref(null);
        const showEditTicketModalFromDayView = ref(false);
        const editedTicket = ref({
            id: null,
            name: "",
            description: "",
            priority: null
        });

        const openEditTicketModalFromDayView = (ticket) => {
            editedTicket.value = {
                ...ticket
            };
            showEditTicketModalFromDayView.value = true;
        };

        const formattedDate = computed(() => {
            const date = new Date(route.params.date);
            date.setDate(date.getDate());
            return date.toLocaleDateString("hu-HU", {
                year: "numeric",
                month: "long",
                day: "numeric",
            });
        });

        const fetchTickets = async () => {
            loading.value = true;
            errorMessage.value = "";
            try {
                const selectedDate = route.params.date;
                const storedCalendarId = localStorage.getItem("calendarId");
                if (!storedCalendarId) {
                    throw new Error("Nincs mentett calendarId a localStorage-ban.");
                }
                calendarId.value = storedCalendarId;
                const response = await api.get(`/api/Tickets/todolist/${selectedDate}/${calendarId.value}`);
                tickets.value = response.data
                    .map(ticket => ({
                        ...ticket,
                        backgroundColor: ticket.color || "#ffffff",
                    }))
                    .sort((a, b) => a.currentPosition - b.currentPosition);
            } catch (error) {
                console.error("Error loading tickets:", error);
                errorMessage.value = "Failed to load tickets.";
            } finally {
                loading.value = false;
            }
        };

        const handleDelete = async (ticketId) => {
            await deleteTicket(ticketId, tickets.value, errorMessage);
            await fetchTickets();
            await tryDeleteDay(calendarId.value, route.params.date, tickets.value.length);
        }

        const handleSendBack = async (ticketId) => {
            await sendBackToCalendarList(ticketId);
            await updateTicketOrder(tickets.value);
            await fetchTickets();
            await tryDeleteDay(calendarId.value, route.params.date, tickets.value.length);
        };

        const onDragEnd = async () => {
            await updateTicketOrder(tickets.value);
            await fetchTickets();
        };

        onMounted(() => {
            fetchTickets();
            emitter.on("ticketTimeUnSet", fetchTickets);
            emitter.on("newTicketCreatedWithoutTime", fetchTickets);
        });

        onUnmounted(() => {
            emitter.off("ticketTimeUnSet", fetchTickets);
            emitter.off("newTicketCreatedWithoutTime", fetchTickets);
        });

        watch(() => route.params.date, fetchTickets);

        return {
            tickets,
            loading,
            errorMessage,
            formattedDate,
            handleDelete,
            fetchTickets,
            calendarId,
            onDragEnd,
            handleSendBack,
            showEditTicketModalFromDayView,
            editedTicket,
            openEditTicketModalFromDayView,
            fetchTickets,
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
    background: linear-gradient(to right, #9dc1b7, #717877);
    border-radius: 8px;
}

.ticket {
    padding: 10px;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    transition: background-color 0.3s ease-in-out;
    cursor: pointer;
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
