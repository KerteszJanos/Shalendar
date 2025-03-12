<template>
<div class="container">
    <div class="todo-list">
        <div class="header">{{ formattedDate }}</div>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <draggable v-model="tickets" @start="onDragStart" @end="onDragEnd" group="tickets" itemKey="id">

            <template #item="{ element }">
                <div class="ticket" :style="{ backgroundColor: element.backgroundColor }" @click="openEditTicketModalFromDayView(element)">
                    <input type="checkbox" class="ticket-checkbox" :checked="element.isCompleted" @click.stop="toggleCompletion(element)" />
                    <p><strong>{{ element.name }}</strong></p>
                    <p v-if="element.description">{{ element.description }}</p>
                    <p v-if="element.priority">Priority: {{ element.priority }}</p>
                    <button @click.stop="handleDelete(element.id)" class="delete-btn">Delete</button>
                    <button @click.stop="handleSendBack(element.id)" class="send-back-btn">Send Back</button>
                    <button @click.stop="openCopyTicketModal(element.id)" class="copy-btn">Copy</button>
                </div>
            </template>
        </draggable>
    </div>

    <EditTicketModalFromDayView :show="showEditTicketModalFromDayView" :ticketData="editedTicket" @update:show="showEditTicketModalFromDayView = $event" @ticketUpdated="fetchTickets" />
    <copyTicketModal :show="showCopyTicketModal" :ticketId="selectedTicketId" :date="route.params.date" @update:show="showCopyTicketModal = $event" />

</div>
</template>

<script>
import {
    ref,
    onMounted,
    onBeforeUnmount,
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
import {
    setErrorMessage
} from "@/utils/errorHandler";
import {
    toggleTicketCompletion
} from "@/components/atoms/isCompletedCheckBox";
import
copyTicketModal from "@/components/molecules/CopyTicketModal.vue";
import {
    connection,
    ensureConnected
} from "@/services/signalRService";

export default {
    components: {
        draggable,
        EditTicketModalFromDayView,
        copyTicketModal,
    },
    setup() {
        const route = useRoute();
        const tickets = ref([]);
        const errorMessage = ref("");
        const calendarId = ref(localStorage.getItem("calendarId"));
        const showEditTicketModalFromDayView = ref(false);
        const showCopyTicketModal = ref(false);
        const selectedTicketId = ref(null);
        const editedTicket = ref({
            id: null,
            name: "",
            description: "",
            priority: null
        });

        const calendarListEvents = [
            "TicketCreatedInDayView",
            "TicketScheduled",
            "TicketCopiedInCalendar",
            "TicketReorderedInDayView",
            "TicketMovedBackToCalendar",
            "TicketUpdatedInDayView",
            "TicketCompletedUpdatedInDayView",
            "TicketMovedBetweenDays",
            "TicketDeletedInDayView"
        ];

        const onDragStart = (event) => {
            const ticket = event.item.__draggable_context.element;
            localStorage.setItem("draggedTicket", JSON.stringify(ticket));
        };

        const openCopyTicketModal = (ticketId) => {
            selectedTicketId.value = ticketId;
            showCopyTicketModal.value = true;
        };

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

        const toggleCompletion = async (ticket) => {
            try {
                await toggleTicketCompletion(ticket.id, !ticket.isCompleted, errorMessage);
                ticket.isCompleted = !ticket.isCompleted;
            } catch (error) {
                console.error("Failed to update ticket status:", error);
            }
        };

        const fetchTickets = async () => {
            const selectedDate = route.params.date;
            errorMessage.value = "";
            try {
                const response = await api.get(`/api/Tickets/todolist/${selectedDate}/${calendarId.value}`);
                tickets.value = response.data
                    .map(ticket => ({
                        ...ticket,
                        backgroundColor: ticket.color || "#ffffff",
                    }))
                    .sort((a, b) => a.currentPosition - b.currentPosition);
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(errorMessage, "Error loading tickets.");
                    console.error("Error loading tickets:", error);
                }
            }
        };

        const handleDelete = async (ticketId) => {
            await deleteTicket(ticketId, tickets.value, errorMessage);
            if (!errorMessage.value) {
                await fetchTickets();
                await tryDeleteDay(calendarId.value, route.params.date, tickets.value.length);
            }

        }

        const handleSendBack = async (ticketId) => {
            await sendBackToCalendarList(ticketId, errorMessage);
            if (!errorMessage.value) {
                await updateTicketOrder(tickets.value, errorMessage);
                await fetchTickets();
                await tryDeleteDay(calendarId.value, route.params.date, tickets.value.length);
            }
        };

        const onDragEnd = async () => {
            await updateTicketOrder(tickets.value, errorMessage);
        };

        onMounted(async () => {
            fetchTickets();
            emitter.on("ticketTimeUnSet", fetchTickets);
            emitter.on("newTicketCreatedWithoutTime", fetchTickets);
            emitter.on("ticketDateChanged", fetchTickets);

            await ensureConnected();
            await connection.invoke("JoinGroup", calendarId.value);
            calendarListEvents.forEach(event => {
                connection.on(event, async (receivedDayDate) => {
                    const formattedDate = receivedDayDate.split("T")[0];
                    if (route.params.date === formattedDate) {
                        fetchTickets();
                    }
                });
            });
            connection.on("CalendarCopied", async () => {
                fetchTickets();
            });
        });

        onBeforeUnmount(() => {
            connection.off("CalendarCopied");
            
            if (calendarId.value) {
                connection.invoke("LeaveGroup", calendarId.value);
            }
        });

        onUnmounted(() => {
            emitter.off("ticketTimeUnSet", fetchTickets);
            emitter.off("newTicketCreatedWithoutTime", fetchTickets);
            emitter.off("ticketDateChanged", fetchTickets);
        });

        watch(() => route.params.date, fetchTickets);

        return {
            tickets,
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
            toggleCompletion,
            openCopyTicketModal,
            selectedTicketId,
            showCopyTicketModal,
            route,
            onDragStart
        };
    },
};
</script>

<style scoped>
.ticket-checkbox {
    position: absolute;
    top: 5px;
    left: 5px;
    width: 20px;
    height: 20px;
}

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
    position: relative;
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
