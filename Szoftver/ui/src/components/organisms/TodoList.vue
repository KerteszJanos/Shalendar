<!--
  This component is the right side of the DayView screen.

  Responsibilities:
  - Displays a list of tickets that are not assigned to specific time intervals, but still scheduled for the current day.
  - Enables:
    - Reordering via drag-and-drop
    - Editing ticket details
    - Marking tickets as completed
    - Copying tickets to another calendar or date
    - Sending tickets back to their original calendar list
    - Deleting tickets
  - Tickets are sorted by their current position and displayed with priority and description indicators.
  - Listens for real-time updates through SignalR to keep the list in sync.
  - Designed to complement the left DayView component, which handles time-based tickets.
-->

<template>
<div class="container">
    <div class="header">{{ formattedDate }}</div>
    <div class="todo-list">
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <draggable v-model="tickets" @start="onDragStart" @end="onDragEnd" group="tickets" itemKey="id" class="ticket-container">
            <template #item="{ element }">
                <div class="ticket-item" draggable="true" @click="openEditTicketModalFromDayView(element)" :style="{ backgroundColor: element.backgroundColor || '#CCCCCC' }" title="Click to edit or schedule this ticket">

                    <div class="ticket-header">
                        <input type="checkbox" class="ticket-checkbox" :checked="element.isCompleted" @click.stop="toggleCompletion(element)" :id="'checkbox-' + element.id" :title="element.isCompleted ? 'Mark as not completed' : 'Mark as completed'"/>
                        <p class="ticket-name " :title="element.name"><strong>{{ element.name }}</strong></p>
                    </div>

                    <div class="ticket-description" v-if="element.description" title="">
                        <p v-html="cleanDescription(element.description)" class="description"></p>
                    </div>

                    <div class="ticket-info">
                        <span v-if="element.description" class="description-icon " :style="{ color: colorShade(element.backgroundColor, -50) || '#CCCCCC' }" :title="element.description">
                            <FileText />
                        </span>
                        <span v-if="element.priority" class="priority" :style="{ backgroundColor: getPriorityColor(element.priority) }" title="1 is the highest priority, 9 is the lowest">
                            {{ element.priority }}
                        </span>
                    </div>

                    <div class="ticket-actions">
                        <span title="Send back to the list it is assigned to">
                            <RotateCwSquare class="icon send-back-icon" @click.stop="handleSendBack(element.id)" />
                        </span>
                        <span title="Copy this ticket to another calendar if it doesn't already exist there">
                            <Copy class="icon copy-icon" @click.stop="openCopyTicketModal(element.id)" />
                        </span>
                        <span title="Delete this ticket">
                            <Trash2 class="icon delete-icon" @click.stop="handleDelete(element.id)" />
                        </span>
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
        // ---------------------------------
        // Constants	  	        	   |
        // --------------------------------- 
        const route = useRoute();
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

        // ---------------------------------
        // Reactive state	        	   |
        // ---------------------------------
        const tickets = ref([]);
        const errorMessage = ref("");
        const calendarId = ref(localStorage.getItem("calendarId"));
        const showEditTicketModalFromDayView = ref(false);
        const showCopyTicketModal = ref(false);
        const selectedTicketId = ref(null);
        const latestFetchId = ref(0);
        const editedTicket = ref({
            id: null,
            name: "",
            description: "",
            priority: null
        });
        // Non-reactive computed variable
        const formattedDate = computed(() => {
            const date = new Date(route.params.date);
            date.setDate(date.getDate());
            return date.toLocaleDateString("en-US", {
                year: "numeric",
                month: "long",
                day: "numeric",
            });
        });
        // ---------------------------------
        // Methods		       |
        // ---------------------------------
        // --------------
        // Modals	    |
        // --------------
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

        // --------------
        // Core actions	|
        // --------------
        const toggleCompletion = async (ticket) => {
            try {
                await toggleTicketCompletion(ticket.id, !ticket.isCompleted, errorMessage);
                if (errorMessage.value) {
                    document.getElementById(`checkbox-${ticket.id}`).checked = ticket.isCompleted;
                } else {
                    ticket.isCompleted = !ticket.isCompleted;
                }
            } catch (error) {
                console.error("Failed to update ticket status:", error);
            }
        };

        const fetchTickets = async () => {
            const selectedDate = route.params.date;
            const fetchId = ++latestFetchId.value;
            errorMessage.value = "";
            try {
                const response = await api.get(`/api/Tickets/todolist/${selectedDate}/${calendarId.value}`);
                if (fetchId !== latestFetchId.value) {

                    return;
                }
                tickets.value = response.data
                    .map(ticket => ({
                        ...ticket,
                        backgroundColor: ticket.color || "#ffffff",
                    }))
                    .sort((a, b) => a.currentPosition - b.currentPosition);
            } catch (error) {
                if (fetchId !== latestFetchId.value) {
                    return;
                }
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

        const onDragStart = (event) => {
            const ticket = event.item.__draggable_context.element;
            localStorage.setItem("draggedTicket", JSON.stringify(ticket));
        };

        const onDragEnd = async () => {
            await updateTicketOrder(tickets.value, errorMessage);
        };

        // --------------
        // Helpers  	|
        // --------------
        // Replaces newline characters with <br> tags to properly format multiline descriptions in HTML.
        const cleanDescription = (desc) => {
            return desc.replace(/\n/g, "<br>");
        };

        // ---------------------------------
        // Lifecycle hooks	        	   |
        // ---------------------------------
        watch(() => route.params.date, fetchTickets);

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
    overflow: hidden;
    text-overflow: ellipsis;
    word-wrap: break-word;
    scrollbar-width: none;
}

.description::-webkit-scrollbar {
    display: none;
}

.container {
    display: flex;
    height: 100%;
    flex-direction: column;
    padding-top: 20px;
    padding-bottom: 20px;
}

.ticket-container {
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
