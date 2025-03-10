<template>
<div class="container">
    <div class="day-view">
        <div class="header">{{ formattedDate }}</div>
        <p v-if="loading">Loading...</p>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <div class="time-container">
            <div class="time-scrollable">
                <div class="hour-marker" v-for="hour in 24" :key="hour" :style="{ top: `${hour * 100}px` }">
                    {{ hour }}:00
                </div>
                <div class="time-indicator" :style="timeIndicatorStyle">
                    <span class="time-label">{{ currentTime }}</span>
                </div>
                <div v-for="ticket in tickets" :key="ticket.id" class="ticket" :style="getTicketStyle(ticket)" @click="openEditTicketModalFromDayView(ticket)">

                    <div class="ticket-header">
                        <input type="checkbox" class="ticket-checkbox" :checked="ticket.isCompleted" @click.stop="toggleCompletion(ticket)" />
                        <strong class="ticket-name">{{ ticket.name }}</strong>
                    </div>

                    <div class="ticket-content">
                        <p v-if="ticket.description">{{ ticket.description }}</p>
                        <p v-if="ticket.priority">Priority: {{ ticket.priority }}</p>
                        <p v-if="ticket.startTime && ticket.endTime">
                            {{ formatTime(ticket.startTime) }} - {{ formatTime(ticket.endTime) }}
                        </p>
                    </div>

                    <div class="ticket-actions">
                        <button class="delete-btn" @click.stop="handleDelete(ticket.id)">Delete</button>
                        <button @click.stop="handleSendBack(ticket.id)" class="send-back-btn">Send Back</button>
                        <button @click.stop="openCopyTicketModal(ticket.id)" class="copy-btn">Copy</button>
                    </div>
                </div>

            </div>
        </div>
    </div>
    <EditTicketModalFromDayView :show="showEditTicketModalFromDayView" :ticketData="editedTicket" @update:show="showEditTicketModalFromDayView = $event" @ticketUpdated="fetchTickets" />
    <copyTicketModal :show="showCopyTicketModal" :ticketId="selectedTicketId" :date="route.params.date" @update:show="showCopyTicketModal = $event" />
</div>
</template>

<script>
import {
    ref,
    computed,
    onMounted,
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
import copyTicketModal from "@/components/molecules/copyTicketModal.vue";

export default {
    components: {
        EditTicketModalFromDayView,
        copyTicketModal
    },
    setup() {
        const route = useRoute();
        const tickets = ref([]);
        const loading = ref(true);
        const errorMessage = ref("");
        const calendarId = ref(null);
        const showEditTicketModalFromDayView = ref(false);
        const showCopyTicketModal = ref(false);
        const selectedTicketId = ref(null);
        const editedTicket = ref({
            id: null,
            name: "",
            description: "",
            priority: null
        });

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

        const currentTime = ref(getCurrentTime());

        function getCurrentTime() {
            const now = new Date();
            return now.toLocaleTimeString("en-US", {
                hour: "2-digit",
                minute: "2-digit",
            });
        }

        const timeIndicatorStyle = ref(getTimeIndicatorStyle());

        const toggleCompletion = async (ticket) => {
            try {
                await toggleTicketCompletion(ticket.id, !ticket.isCompleted, errorMessage);
                ticket.isCompleted = !ticket.isCompleted;
            } catch (error) {
                console.error("Failed to update ticket status:", error);
            }
        };

        function getTimeIndicatorStyle() {
            const now = new Date();
            const topPx = (now.getHours() + now.getMinutes() / 60) * 100;
            return {
                top: `${topPx}px`
            };
        }

        let intervalId;
        onMounted(() => {
            intervalId = setInterval(() => {
                currentTime.value = getCurrentTime();
                timeIndicatorStyle.value = getTimeIndicatorStyle();
            }, 60000);

            fetchTickets();
            emitter.on("ticketTimeSet", fetchTickets);
            emitter.on("newTicketCreatedWithTime", fetchTickets);
        });

        onUnmounted(() => {
            if (intervalId) clearInterval(intervalId);
            emitter.off("ticketTimeSet", fetchTickets);
            emitter.off("newTicketCreatedWithTime", fetchTickets);
        });

        const fetchTickets = async () => {
            loading.value = true;
            errorMessage.value = "";
            try {
                const selectedDate = route.params.date;
                const storedCalendarId = localStorage.getItem("calendarId");
                if (!storedCalendarId) {
                    setErrorMessage(errorMessage, "No calendarId found in localStorage.");
                    console.error("No calendarId found in localStorage.");
                }
                calendarId.value = storedCalendarId;

                const response = await api.get(
                    `/api/Tickets/scheduled/${selectedDate}/${calendarId.value}`
                );
                tickets.value = response.data.map((ticket) => ({
                    ...ticket,
                    backgroundColor: ticket.color || "#ffffff",
                }));
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(errorMessage, "Error loading tickets.");
                    console.error("Error loading tickets:", error);
                }
            } finally {
                loading.value = false;
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

        // Compute CSS style for a ticket based on its startTime and endTime by combining the selected date and time
        const getTicketStyle = (ticket) => {
            // Combine the selected date with the ticket's start and end time to form complete datetime strings
            const startDateTime = `${route.params.date}T${ticket.startTime}`;
            const endDateTime = `${route.params.date}T${ticket.endTime}`;
            const start = new Date(startDateTime);
            const end = new Date(endDateTime);
            const topPosition = (start.getHours() + start.getMinutes() / 60) * 100;
            // Calculate duration in hours and convert it to pixels (100px per hour)
            const durationHours = (end - start) / (1000 * 60 * 60);
            const height = durationHours * 100;
            return {
                top: `${topPosition}px`,
                height: `${height}px`,
                backgroundColor: ticket.backgroundColor,
                position: "absolute",
                left: "10px",
                right: "10px",
                padding: "5px",
                borderRadius: "4px",
            };
        };

        const formatTime = (timeString) => {
            const dateTimeString = `${route.params.date}T${timeString}`;
            const date = new Date(dateTimeString);
            return date.toLocaleTimeString("en-US", {
                hour: "2-digit",
                minute: "2-digit",
            });
        };

        watch(() => route.params.date, fetchTickets);

        return {
            formattedDate,
            tickets,
            loading,
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
        };
    },
};
</script>

<style scoped>
.ticket {
    border: 1px solid #ccc;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    background: #ddd;
    padding: 10px;
    border-radius: 5px;
    position: relative;
    display: flex;
    flex-direction: column;
    gap: 5px;
}

.ticket-header {
    display: flex;
    align-items: center;
    gap: 10px;
}

.container {
    display: flex;
    height: 100vh;
}

.day-view {
    flex: 1;
    display: flex;
    flex-direction: column;
    position: relative;
    padding: 20px;
    border-right: 1px solid #ccc;
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

.time-container {
    flex-grow: 1;
    overflow-y: auto;
    position: relative;
    border-radius: 8px;
    margin-top: 20px;
}

.time-scrollable {
    position: relative;
    height: 2400px;
}

.hour-marker {
    position: absolute;
    width: 100%;
    height: 100px;
    border-bottom: 1px solid rgba(0, 0, 0, 0.2);
    line-height: 100px;
    padding-left: 10px;
    font-size: 1rem;
    font-weight: bold;
    color: #333;
}

.ticket-checkbox {
    width: 16px;
    height: 16px;
    flex-shrink: 0;
}

.ticket-name {
    font-weight: bold;
    flex-grow: 1;
}

.time-indicator {
    position: absolute;
    left: 10px;
    right: 10px;
    height: 4px;
    background: #ff6347;
    transition: top 1s linear;
    display: flex;
    align-items: center;
    justify-content: flex-start;
    padding-left: 5px;
}

.ticket-content {
    font-size: 14px;
}

.ticket-actions {
    display: flex;
    gap: 5px;
}

.delete-btn {
    background: red;
    color: white;
    border: none;
    padding: 3px 6px;
    cursor: pointer;
    border-radius: 4px;
}

.send-back-btn {
    background: white;
    border: 1px solid #ccc;
    padding: 3px 6px;
    cursor: pointer;
    border-radius: 4px;
}

.time-label {
    background: white;
    padding: 2px 5px;
    border-radius: 4px;
    font-size: 0.8rem;
    font-weight: bold;
    color: #333;
}

.ticket {
    border: 1px solid #ccc;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.delete-btn {
    background: red;
    color: white;
    border: none;
    padding: 3px 6px;
    cursor: pointer;
    border-radius: 4px;
    margin-top: 5px;
}
</style>
