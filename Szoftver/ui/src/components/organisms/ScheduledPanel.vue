<!--
  This component is the left side of the DayView screen.

  Responsibilities:
  - Displays a full 24-hour vertical timeline with markers every 15 minutes.
  - Shows all tickets that are scheduled to specific times on a given day.
  - Dynamically positions and sizes ticket elements based on their start and end times.
  - Handles:
    - Ticket completion toggle
    - Editing scheduled tickets
    - Deleting or sending tickets back to calendar lists
    - Copying tickets to another calendar or date
  - Draws a live time indicator that updates every minute and auto-scrolls to the current time when the components opened.
  - Fetches ticket data from the backend and listens for real-time updates via SignalR.
  - Visually adjusts overlapping tickets using vertical offsetting.

  This component is critical for users to manage and visualize their time-specific tasks on a selected day.
-->

<template>
<div class="container">
    <div class="day-view">
        <div class="header">{{ formattedDate }}</div>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <div class="time-container">
            <div class="time-scrollable">
                <div v-for="hour in Array.from({ length: 24 }, (_, i) => i)" :key="hour">
                    <div class="hour-marker" :style="{ top: `${hour * 400}px` }">
                        {{ formatHour(hour) }}
                    </div>
                    <div class="half-hour-marker" :style="{ top: `${hour * 400 + 200}px` }"></div>
                    <div class="quarter-hour-marker" :style="{ top: `${hour * 400 + 100}px` }"></div>
                    <div class="quarter-hour-marker" :style="{ top: `${hour * 400 + 300}px` }"></div>
                </div>
                <div class="time-indicator" :style="timeIndicatorStyle">
                    <span class="time-label">{{ currentTime }}</span>
                </div>
                <div v-for="ticket in tickets" :key="ticket.id" class="ticket-item" :style="getTicketStyle(ticket)" @click="openEditTicketModalFromDayView(ticket)" title="Click to edit or remove this ticket from the schedule">

                    <div class="ticket-header">
                        <input type="checkbox" class="ticket-checkbox" :checked="ticket.isCompleted" @click.stop="toggleCompletion(ticket)" :id="'checkbox-' + ticket.id" :title="ticket.isCompleted ? 'Mark as not completed' : 'Mark as completed'" />
                        <strong class="ticket-name " :title="ticket.name">{{ ticket.name }}</strong>
                    </div>
                    <div class="ticket-time">
                        <p v-if="ticket.startTime && ticket.endTime" class="ticket-time-text">
                            {{ formatTime(ticket.startTime) }} - {{ formatTime(ticket.endTime) }}
                        </p>
                    </div>
                    <div class="ticket-footer">
                        <div class="ticket-info">
                            <span v-if="ticket.description" class="description-icon " :style="{ color: colorShade(ticket.backgroundColor, -50) || '#CCCCCC' }" :title="ticket.description">
                                <FileText />
                            </span>
                            <span v-if="ticket.priority" class="priority" :style="{ backgroundColor: getPriorityColor(ticket.priority) }" title="1 is the highest priority, 9 is the lowest">
                                {{ ticket.priority }}
                            </span>
                        </div>
                        <div class="ticket-actions">
                            <span title="Send back to the list it is assigned to">
                                <RotateCwSquare class="icon send-back-icon" @click.stop="handleSendBack(ticket.id)" />
                            </span>
                            <span title="Copy this ticket to another calendar if it doesn't already exist there">
                                <Copy class="icon copy-icon" @click.stop="openCopyTicketModal(ticket.id)" />
                            </span>
                            <span title="Delete this ticket">
                                <Trash2 class="icon delete-icon" @click.stop="handleDelete(ticket.id)" />
                            </span>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    </div>
    <EditTicketModalFromDayView :show="showEditTicketModalFromDayView" :ticketData="editedTicket" @update:show="showEditTicketModalFromDayView = $event" @ticketUpdated="fetchTickets" />
    <CopyTicketModal :show="showCopyTicketModal" :ticketId="selectedTicketId" :date="route.params.date" @update:show="showCopyTicketModal = $event" />
</div>
</template>

<script>
import {
    ref,
    computed,
    onMounted,
    onBeforeUnmount,
    onUnmounted,
    watch
} from "vue";
import {
    useRoute
} from "vue-router";
import api from "@/utils/config/axios-config";
import {
    deleteTicket
} from "@/components/atoms/deleteTicket";
import {
    sendBackToCalendarList
} from "@/components/atoms/SendBackToCalenderList";
import {
    tryDeleteDay
} from "@/components/atoms/TryDeleteDay";
import {
    emitter
} from "@/utils/eventBus";
import EditTicketModalFromDayView from "@/components/molecules/EditTicketModalFromDayView.vue";
import {
    setErrorMessage
} from "@/utils/errorHandler";
import {
    toggleTicketCompletion
} from "@/components/atoms/isCompletedCheckBox";
import CopyTicketModal from "@/components/molecules/CopyTicketModal.vue";
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
    colorShade
} from "@/components/atoms/colorShader";
import {
    getPriorityColor
} from "@/components/atoms/getPriorityColor";

export default {
    components: {
        EditTicketModalFromDayView,
        CopyTicketModal,
        Copy,
        Trash2,
        FileText,
        RotateCwSquare
    },
    setup() {
        // ---------------------------------
        // Constants        		   |
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
            "TicketDeletedInDayView",
        ];

        // ---------------------------------    
        // Reactive state       		   |
        // ---------------------------------
        const tickets = ref([]);
        const errorMessage = ref("");
        const calendarId = ref(localStorage.getItem("calendarId"));
        const showEditTicketModalFromDayView = ref(false);
        const showCopyTicketModal = ref(false);
        const selectedTicketId = ref(null);
        const currentTime = ref(getCurrentTime());
        const latestFetchId = ref(0);
        const timeIndicatorStyle = ref(getTimeIndicatorStyle());
        const editedTicket = ref({
            id: null,
            name: "",
            description: "",
            priority: null
        });
        // Non-reactive internal variable
        let intervalId;
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
        // Methods		               |
        // ---------------------------------
        // --------------
        // Modals   	|
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
                const response = await api.get(
                    `/api/Tickets/scheduled/${selectedDate}/${calendarId.value}`
                );
                if (fetchId !== latestFetchId.value) {
                    return;
                }
                tickets.value = response.data.map((ticket) => ({
                    ...ticket,
                    backgroundColor: ticket.color || "#ffffff",
                }));
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
        };

        const handleSendBack = async (ticketId) => {
            await sendBackToCalendarList(ticketId, errorMessage);
            if (!errorMessage.value) {
                await fetchTickets();
                await tryDeleteDay(calendarId.value, route.params.date, tickets.value.length);
            }
        };

        // Returns the inline style object for a ticket based on its start/end time and overlap with others.
        const getTicketStyle = (ticket) => {
            const startDateTime = `${route.params.date}T${ticket.startTime}`;
            const endDateTime = `${route.params.date}T${ticket.endTime}`;
            const start = new Date(startDateTime);
            const end = new Date(endDateTime);
            const topPosition = (start.getHours() + start.getMinutes() / 60) * 400;
            const durationHours = (end - start) / (1000 * 60 * 60);
            const height = durationHours * 400;

            // Check overlapping tickets
            let overlappingTickets = tickets.value.filter(t => {
                const tStart = new Date(`${route.params.date}T${t.startTime}`);
                const tEnd = new Date(`${route.params.date}T${t.endTime}`);
                return (start < tEnd && end > tStart);
            });

            // Sort overlapping tickets by duration (longer first)
            overlappingTickets = overlappingTickets.sort((a, b) => {
                const durationA = new Date(`${route.params.date}T${a.endTime}`) - new Date(`${route.params.date}T${a.startTime}`);
                const durationB = new Date(`${route.params.date}T${b.endTime}`) - new Date(`${route.params.date}T${b.startTime}`);
                return durationB - durationA;
            });

            const index = overlappingTickets.findIndex(t => t.id === ticket.id);
            const offset = 25;

            return {
                top: `${topPosition + index * offset}px`,
                left: `${index * offset}px`,
                height: `${height}px`,
                backgroundColor: ticket.backgroundColor,
                position: "absolute",
                padding: "5px",
                boxSizing: "border-box",
                zIndex: index, // Higher index means ticket is on top
                right: "10px",
                width: "auto",
            };
        };

        // --------------
        // Helpers	    |
        // --------------
        const formatTime = (timeString) => {
            const dateTimeString = `${route.params.date}T${timeString}`;
            const date = new Date(dateTimeString);
            return date.toLocaleTimeString("en-US", {
                hour: "2-digit",
                minute: "2-digit",
            });
        };

        const formatHour = (hour) => {
            const period = hour < 12 ? 'AM' : 'PM';
            const displayHour = hour % 12 === 0 ? 12 : hour % 12;
            return `${displayHour}:00 ${period}`;
        };

        function getCurrentTime() {
            const now = new Date();
            return now.toLocaleTimeString("en-US", {
                hour: "2-digit",
                minute: "2-digit",
            });
        };

        function getTimeIndicatorStyle() {
            const now = new Date();
            const topPx = (now.getHours() + now.getMinutes() / 60) * 400;
            return {
                top: `${topPx}px`
            };
        };

        const scrollToCurrentTime = () => {

            const timeIndicator = document.querySelector(".time-indicator");
            if (timeIndicator) {
                const offset = 100;
                const container = document.querySelector(".time-container");

                if (container) {
                    container.scrollTo({
                        top: timeIndicator.offsetTop - offset,
                        behavior: "smooth"
                    });
                }
            }

        };

        // ---------------------------------
        // Lifecycle hooks		           |
        // ---------------------------------
        watch(() => route.params.date, fetchTickets);

        onMounted(async () => {
            intervalId = setInterval(() => {
                currentTime.value = getCurrentTime();
                timeIndicatorStyle.value = getTimeIndicatorStyle();
            }, 60000);

            fetchTickets();
            emitter.on("ticketTimeSet", fetchTickets);
            emitter.on("newTicketCreatedWithTime", fetchTickets);

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
            connection.on("CalendarDeleted", async () => {
                window.location.reload();
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

            scrollToCurrentTime();
        });

        onUnmounted(() => {
            if (intervalId) clearInterval(intervalId);
            emitter.off("ticketTimeSet", fetchTickets);
            emitter.off("newTicketCreatedWithTime", fetchTickets);
        });

        onBeforeUnmount(() => {
            connection.off("CalendarCopied");
            connection.off("CalendarListUpdated");
            connection.off("CalendarListDeleted");

            if (calendarId.value) {
                connection.invoke("LeaveGroup", calendarId.value);
            }
        });

        return {
            formattedDate,
            tickets,
            errorMessage,
            handleDelete,
            currentTime,
            timeIndicatorStyle,
            getTicketStyle,
            formatTime,
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
            colorShade,
            getPriorityColor,
            formatHour
        };
    },
};
</script>

<style scoped>
.ticket-item {
    width: auto;
    margin-left: 30px;
    border: 2px solid black;
    min-width: 90px;
}

.ticket-item:hover {
    z-index: 999 !important;
}

.ticket-footer {
    display: flex;
    flex-direction: row;
    justify-content: space-between;
}

.ticket-time {
    text-align: center;

}

.ticket-time-text {
    margin: 0;
}

.ticket-info {
    position: static;
    display: flex;
    align-items: center;
    gap: 5px;
}

.ticket-header {
    margin-bottom: 10px;
}

.container {
    display: flex;
    height: 100%;
}

.day-view {
    flex: 1;
    display: flex;
    flex-direction: column;
    position: relative;
    padding: 20px;
    border-radius: 5px;
}

.header {
    font-size: 1.5rem;
    font-weight: bold;
    text-align: center;
    margin-bottom: 20px;
    padding: 10px;
    background: #14919B;
    border-radius: 8px;
}

.time-container {
    flex-grow: 1;
    overflow-y: auto;
    position: relative;
    border-radius: 8px;
    margin-top: 20px;
}

.time-container::-webkit-scrollbar {
    width: 10px;
    height: 10px;
}

.time-container::-webkit-scrollbar-track {
    background: #e3f2fd;
    border-radius: 8px;
}

.time-container::-webkit-scrollbar-corner {
    background: #e3f2fd;
}

.time-container::-webkit-scrollbar-thumb {
    background: #0B6477;
    border-radius: 8px;
}

.time-container::-webkit-scrollbar-thumb:hover {
    background: #213A57;
}

.time-scrollable {
    position: relative;
    height: 9600px;
}

.hour-marker {
    position: absolute;
    width: 100%;
    height: 400px;
    border-bottom: 2px solid rgba(0, 0, 0, 0.4);
    font-size: 1rem;
    font-weight: bold;
    color: #333;
}

.half-hour-marker {
    position: absolute;
    width: 100%;
    border-bottom: 1px solid rgba(0, 0, 0, 0.2);
}

.quarter-hour-marker {
    position: absolute;
    width: 100%;
    border-bottom: 1px dashed rgba(0, 0, 0, 0.2);
}

.time-indicator {
    position: absolute;
    left: 10px;
    right: 10px;
    height: 4px;
    background: #80ED99;
    transition: top 1s linear;
    display: flex;
    align-items: center;
    justify-content: flex-start;
    z-index: 99;
    border-radius: 0 50% 50% 0;
}

.time-label {
    position: absolute;
    left: -10px;
    padding: 1px 8px 1px 1px;
    clip-path: polygon(0% 0%, 80% 0%, 100% 50%, 80% 100%, 0% 100%);
    font-size: 0.8rem;
    font-weight: bold;
    background: #80ED99;
}
</style>
