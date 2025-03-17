<template>
<div class="container">
    <div class="header">{{ formattedDate }}</div>
    <div class="todo-list">
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <draggable v-model="tickets" @start="onDragStart" @end="onDragEnd" group="tickets" itemKey="id" class="ticket-container">
            <template #item="{ element }">
                <div class="ticket-item" draggable="true" @click="openEditTicketModalFromDayView(element)" :style="{ backgroundColor: element.backgroundColor || '#CCCCCC' }">

                    <div class="ticket-header">
                        <input type="checkbox" class="ticket-checkbox" :checked="element.isCompleted" @click.stop="toggleCompletion(element)" />
                        <p class="ticket-name"><strong>{{ element.name }}</strong></p>
                    </div>

                    <div class="ticket-description" v-if="element.description">
                        <p v-html="cleanDescription(element.description)" class="description"></p>
                    </div>

                    <div class="ticket-info">
                        <span v-if="element.description" class="description-icon" :style="{ color: colorShade(element.backgroundColor, -50) || '#CCCCCC' }">
                            <FileText />
                        </span>
                        <span v-if="element.priority" class="priority" :style="{ backgroundColor: getPriorityColor(element.priority) }">
                            {{ element.priority }}
                        </span>
                    </div>

                    <div class="ticket-actions">
                        <RotateCwSquare class="icon send-back-icon" @click.stop="handleSendBack(element.id)" />
                        <Copy class="icon copy-icon" @click.stop="openCopyTicketModal(element.id)" />
                        <Trash2 class="icon delete-icon" @click.stop="handleDelete(element.id)" />
                    </div>
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
import {
    Copy,
    Trash2,
    FileText,
    RotateCwSquare
} from "lucide-vue-next";
import {
    getPriorityColor
} from "@/components/atoms/getPriorityColor";
import {
    colorShade
} from "@/components/atoms/colorShader";

export default {
    components: {
        draggable,
        EditTicketModalFromDayView,
        copyTicketModal,
        Copy,
        Trash2,
        FileText,
        RotateCwSquare
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

        const cleanDescription = (desc) => {
            return desc.replace(/\n/g, "<br>");
        };

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
            connection.on("CalendarListUpdated", async () => {
                fetchTickets();
            });
            connection.on("CalendarListDeleted", async () => {
                fetchTickets();
            });
        });

        onBeforeUnmount(() => {
            connection.off("CalendarCopied");
            connection.off("CalendarListUpdated");
            connection.off("CalendarListDeleted");

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
            onDragStart,
            getPriorityColor,
            colorShade,
            cleanDescription
        };
    },
};
</script>

<style scoped>
.description {
    display: -webkit-box;
    -webkit-line-clamp: 5;
    -webkit-box-orient: vertical;
    display: box;
    line-clamp: 5;
    overflow: auto;
    text-overflow: ellipsis;
    word-wrap: break-word;
    scrollbar-width: none;
}

.description::-webkit-scrollbar {
    display: none;
    /* Chrome, Safari, Edge */
}


.container {
    display: flex;
    height: 100%;
    flex-direction: column;
    padding-top: 20px;
    padding-bottom: 20px;
}

.ticket-container
{
    margin-right: 20px;
    margin-left: 20px;
}

.todo-list {
    flex: 1;
    display: flex;
    flex-direction: column;
    position: relative;
    border-right: none;
    overflow: auto;
    scrollbar-width: none;
}

.todo-list::-webkit-scrollbar {
    display: none;
    /* Chrome, Safari, Edge */
}

.header {
    font-size: 1.5rem;
    font-weight: bold;
    text-align: center;
    margin-bottom: 10px;
    padding: 10px;
    background: #14919B;
    border-radius: 8px;
    margin: 20px 20px 35px 20px; 
}

.ticket-item {
    width: 100%;
}
</style>
