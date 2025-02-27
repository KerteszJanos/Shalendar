<template>
<div class="container">
    <div class="day-view">
        <div class="header">{{ formattedDate }}</div>
        <p v-if="loading">Loading...</p>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <div class="time-container">
            <div class="time-scrollable">
                <!-- Hour markers for each hour of the day -->
                <div class="hour-marker" v-for="hour in 24" :key="hour" :style="{ top: `${hour * 100}px` }">
                    {{ hour }}:00
                </div>
                <!-- Current time indicator (only shown if the selected date is today) -->
                <div v-if="isToday" class="time-indicator" :style="timeIndicatorStyle">
                    <span class="time-label">{{ currentTime }}</span>
                </div>
                <!-- Render scheduled tickets positioned based on their start time -->
                <div v-for="ticket in tickets" :key="ticket.id" class="ticket" :style="getTicketStyle(ticket)" @click="openEditTicketModalFromDayView(ticket)">
                    <div class="ticket-content">
                        <strong>{{ ticket.name }}</strong>
                        <p v-if="ticket.description">{{ ticket.description }}</p>
                        <p v-if="ticket.priority">Priority: {{ ticket.priority }}</p>
                        <!-- Display ticket time range if both startTime and endTime exist -->
                        <p v-if="ticket.startTime && ticket.endTime">
                            {{ formatTime(ticket.startTime) }} - {{ formatTime(ticket.endTime) }}
                        </p>
                    </div>
                    <button class="delete-btn" @click="handleDelete(ticket.id)">
                        Delete
                    </button>
                    <button @click="handleSendBack(ticket.id)" class="send-back-btn">Send Back</button>
                </div>
            </div>
        </div>
    </div>
    <EditTicketModalFromDayView :show="showEditTicketModalFromDayView" :ticketData="editedTicket" @update:show="showEditTicketModalFromDayView = $event" @ticketUpdated="fetchTickets" /> <!-- Gpt generated -->
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
import { tryDeleteDay } from "@/components/atoms/TryDeleteDay";
import { emitter } from "@/utils/eventBus";
import EditTicketModalFromDayView from "@/components/molecules/EditTicketModalFromDayView.vue";

export default {
    components: {
        EditTicketModalFromDayView // Gpt generated
    },
    setup() {
        const route = useRoute();
        const tickets = ref([]);
        const loading = ref(true);
        const errorMessage = ref("");
        const calendarId = ref(null);
        const showEditTicketModalFromDayView = ref(false); // Gpt generated
        const editedTicket = ref({ id: null, name: "", description: "", priority: null }); // Gpt generated

        const openEditTicketModalFromDayView = (ticket) => { // Gpt generated
            editedTicket.value = { ...ticket };
            showEditTicketModalFromDayView.value = true;
        };


        // Format the selected date for display
        const formattedDate = computed(() => {
            const date = new Date(route.params.date);
            return date.toLocaleDateString("en-US", {
                year: "numeric",
                month: "long",
                day: "numeric",
            });
        });

        // Get the current time formatted as HH:mm
        const currentTime = ref(getCurrentTime());

        function getCurrentTime() {
            const now = new Date();
            return now.toLocaleTimeString("en-US", {
                hour: "2-digit",
                minute: "2-digit",
            });
        }

        // Calculate the position of the current time indicator
        const timeIndicatorStyle = ref(getTimeIndicatorStyle());

        function getTimeIndicatorStyle() {
            const now = new Date();
            const topPx = (now.getHours() + now.getMinutes() / 60) * 100;
            return {
                top: `${topPx}px`
            };
        }

        // Check if the selected date is today
        const isToday = computed(() => {
            const now = new Date();
            const selectedDate = new Date(route.params.date);
            return now.toDateString() === selectedDate.toDateString();
        });

        let intervalId;
        onMounted(() => {
            // Update the current time indicator every minute if viewing today
            if (isToday.value) {
                intervalId = setInterval(() => {
                    currentTime.value = getCurrentTime();
                    timeIndicatorStyle.value = getTimeIndicatorStyle();
                }, 60000);
            }
            fetchTickets();
            emitter.on("ticketTimeSet", fetchTickets);
        });

        onUnmounted(() => {
            if (intervalId) clearInterval(intervalId);
            emitter.off("ticketTimeSet", fetchTickets);
        });

        // Fetch scheduled tickets from the API endpoint using the selected date and calendarId
        const fetchTickets = async () => {
            loading.value = true;
            errorMessage.value = "";
            try {
                const selectedDate = route.params.date;
                const storedCalendarId = localStorage.getItem("calendarId");
                if (!storedCalendarId) {
                    throw new Error("No calendarId found in localStorage.");
                }
                calendarId.value = storedCalendarId;

                // Call the API endpoint: /api/Tickets/scheduled/{date}/{calendarId}
                const response = await api.get(
                    `/api/Tickets/scheduled/${selectedDate}/${calendarId.value}`
                );
                // Each ticket has a valid 'startTime' (time string, e.g., "09:00:00")
                tickets.value = response.data.map((ticket) => ({
                    ...ticket,
                    backgroundColor: ticket.color || "#ffffff",
                }));
            } catch (error) {
                console.error("Error loading tickets:", error);
                errorMessage.value = "Failed to load tickets.";
            } finally {
                loading.value = false;
            }
        };

        // Delete a ticket by its id
        const handleDelete = async (ticketId) => {
            await deleteTicket(ticketId, tickets.value, errorMessage);
            await fetchTickets();
            await tryDeleteDay(calendarId.value, route.params.date, tickets.value.length);
        };

        const handleSendBack = async (ticketId) => {
            await sendBackToCalendarList(ticketId);
            await fetchTickets();
            await tryDeleteDay(calendarId.value, route.params.date, tickets.value.length);
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

        // Format a given time string (from ticket) to HH:mm format by combining with the selected date
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
            isToday,
            getTicketStyle,
            formatTime,
            handleSendBack,
            showEditTicketModalFromDayView, // Gpt generated
            editedTicket, // Gpt generated
            openEditTicketModalFromDayView, // Gpt generated
            fetchTickets,
        };
    },
};
</script>

<style scoped>
.ticket {
    cursor: pointer; /* Gpt generated */
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
