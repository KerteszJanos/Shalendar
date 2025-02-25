<template>
<div class="calendar-layout">
    <div class="calendar-container">
        <div class="calendar-header">
            <h2 v-if="calendar.name">{{ calendar.name }} - {{ formattedMonth }}</h2>
            <h2 v-else>Loading...</h2>
            <div class="navigation">
                <button @click="prevMonth">◀</button>
                <button @click="nextMonth">▶</button>
            </div>
        </div>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

        <div class="calendar-grid">
            <div v-for="day in daysOfWeek" :key="day" class="calendar-day header">
                {{ day }}
            </div>
            <div v-for="day in calendarDays" :key="day.date" class="calendar-day" :class="{ 'other-month': !day.isCurrentMonth }" @click="goToDay(day.date)" @drop="onTicketDrop($event, day.date)" @dragover.prevent>
                <!-- Separator line, visible during dragging -->
                <div v-if="isDraggingTicket" class="drop-divider"></div>
                <div class="day-number">{{ day.number }}</div>
                <div class="ticket-lists-container">
                    <div class="ticket-list">
                        <div v-for="ticket in day.scheduleTickets" :key="ticket.name" class="ticket" :style="{ backgroundColor: ticket.color }">
                            {{ ticket.name }}
                        </div>
                    </div>
                    <div class="ticket-list">
                        <div v-for="ticket in day.todoTickets" :key="ticket.name" class="ticket" :style="{ backgroundColor: ticket.color }">
                            {{ ticket.name }}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <!-- Modal for entering ticket time (only shown for left drop) -->
    <Modal :show="showTimeModal" title="Ticket Time" confirmText="Mentés" @close="closeTimeModal" @confirm="confirmTimeModal">
        <div class="modal-content">
            <label for="start-time">Kezdés:</label>
            <input id="start-time" type="time" v-model="modalStartTime" />
            <label for="end-time">Befejezés:</label>
            <input id="end-time" type="time" v-model="modalEndTime" />
            <p v-if="modalErrorMessage" class="error">{{ modalErrorMessage }}</p>
        </div>
    </Modal>
</div>
</template>

<script>
import {
    ref,
    computed,
    onMounted,
    onUnmounted,
    watchEffect
} from "vue";
import {
    useRouter
} from "vue-router";
import api from "@/utils/config/axios-config";
import Modal from "@/components/molecules/Modal.vue";
import {
    emitter
} from "@/utils/eventBus";

export default {
    components: {
        Modal,
    },
    setup() {
        const currentDate = ref(new Date());
        const calendar = ref({
            id: null,
            name: ""
        });
        const errorMessage = ref("");
        const router = useRouter();
        const isDraggingTicket = ref(false);

        // Modal state and temporary storage for drop adatai
        const showTimeModal = ref(false);
        const modalStartTime = ref("");
        const modalEndTime = ref("");
        const modalErrorMessage = ref("");
        const dropTicketData = ref(null);
        const dropDate = ref("");
        const calendarDays = ref([]);

        const daysOfWeek = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        const formattedMonth = computed(() => {
            return currentDate.value.toLocaleDateString("hu-HU", {
                year: "numeric",
                month: "long",
            });
        });

        const fetchTicketsForDate = async (date) => {
            try {
                const response = await api.get(`/api/Tickets/AllDailyTickets/${date}/${calendar.value.id}`);
                const tickets = response.data.map(ticket => ({
                    name: ticket.name,
                    startTime: ticket.startTime, // Hozzáadjuk a StartTime mezőt
                    currentPosition: ticket.currentPosition, // Hozzáadjuk a CurrentPosition mezőt
                    color: ticket.color
                }));

                // Két külön lista létrehozása és rendezése
                const todoTickets = tickets
                    .filter(ticket => ticket.startTime === null)
                    .sort((a, b) => a.currentPosition - b.currentPosition); // CurrentPosition szerint növekvő sorrendben

                const scheduleTickets = tickets
                    .filter(ticket => ticket.startTime !== null)
                    .sort((a, b) => a.startTime.localeCompare(b.startTime)); // StartTime szerint növekvő sorrendben

                return {
                    todoTickets,
                    scheduleTickets
                };
            } catch (error) {
                console.error("Error fetching tickets:", error);
                return {
                    todoTickets: [],
                    scheduleTickets: []
                };
            }
        };

        const fetchCalendarDays = async () => {
            if (!calendar.value.id) return;

            const year = currentDate.value.getFullYear();
            const month = currentDate.value.getMonth();
            const firstDayOfMonth = new Date(year, month, 1).getDay();
            const lastDateOfMonth = new Date(year, month + 1, 0).getDate();

            let days = [];
            let startOffset = firstDayOfMonth === 0 ? 6 : firstDayOfMonth - 1;
            let endOffset = (7 - ((startOffset + lastDateOfMonth) % 7)) % 7;

            const prevMonthLastDate = new Date(year, month, 0).getDate();

            // Előző hónap napjai
            for (let i = startOffset - 1; i >= 0; i--) {
                let date = new Date(year, month - 1, prevMonthLastDate - i).toISOString().split("T")[0];
                let {
                    todoTickets,
                    scheduleTickets
                } = await fetchTicketsForDate(date);

                days.push({
                    number: prevMonthLastDate - i,
                    date: date,
                    isCurrentMonth: false,
                    todoTickets: todoTickets,
                    scheduleTickets: scheduleTickets
                });
            }

            // Aktuális hónap napjai
            for (let i = 1; i <= lastDateOfMonth; i++) {
                let date = new Date(year, month, i).toISOString().split("T")[0];
                let {
                    todoTickets,
                    scheduleTickets
                } = await fetchTicketsForDate(date);

                days.push({
                    number: i,
                    date: date,
                    isCurrentMonth: true,
                    todoTickets: todoTickets,
                    scheduleTickets: scheduleTickets
                });
            }

            // Következő hónap napjai
            for (let i = 1; i <= endOffset; i++) {
                let date = new Date(year, month + 1, i).toISOString().split("T")[0];
                let {
                    todoTickets,
                    scheduleTickets
                } = await fetchTicketsForDate(date);

                days.push({
                    number: i,
                    date: date,
                    isCurrentMonth: false,
                    todoTickets: todoTickets,
                    scheduleTickets: scheduleTickets
                });
            }

            calendarDays.value = days;
        };

        const goToDay = (date) => {
            router.push(`/day/${date}`);
        };

        const fetchCalendar = async () => {
            try {
                const user = JSON.parse(localStorage.getItem("user"));
                if (!user || !user.defaultCalendarId) {
                    throw new Error("No default calendar set.");
                }
                const calendarId = user.defaultCalendarId;
                localStorage.setItem("calendarId", calendarId);
                const response = await api.get(`/api/Calendars/${calendarId}`);
                calendar.value = response.data;
            } catch (error) {
                console.error("Error loading calendar:", error);
                errorMessage.value = "Failed to load calendar.";
            }
        };

        const prevMonth = () => {
            currentDate.value = new Date(
                currentDate.value.getFullYear(),
                currentDate.value.getMonth() - 1,
                1
            );
        };

        const nextMonth = () => {
            currentDate.value = new Date(
                currentDate.value.getFullYear(),
                currentDate.value.getMonth() + 1,
                1
            );
        };

        // Single drop event for the whole day cell.
        // Determines left/right drop based on drop event coordinates.
        const onTicketDrop = async (event, date) => {
            // Retrieve ticket data from localStorage
            const ticketDataStr = localStorage.getItem("draggedTicket");
            if (!ticketDataStr) return;
            const ticketData = JSON.parse(ticketDataStr);

            const dropZone = event.currentTarget;
            const rect = dropZone.getBoundingClientRect();
            const x = event.clientX - rect.left; // X coordinate within the cell
            const side = x < rect.width / 2 ? "left" : "right";

            const calendarId = localStorage.getItem("calendarId");

            if (side === "left") {
                // Left drop: open modal to input times.
                dropTicketData.value = ticketData;
                dropDate.value = date;
                showTimeModal.value = true;
            } else {
                // Right drop: normal processing via API call without time details.
                const scheduleTicketPayload = {
                    CalendarId: parseInt(calendarId),
                    Date: date,
                    TicketId: ticketData.id,
                    StartTime: null,
                    EndTime: null
                };

                try {
                    await api.post("/api/Tickets/ScheduleTicket", scheduleTicketPayload);

                    // Emitter esemény kibocsátása, ha más komponenseknek is kell
                    emitter.emit("ticketScheduled", {
                        ticketId: ticketData.id
                    });

                    // **Frissítsük az adott nap ticketjeit az API-ból**
                    await updateDayTickets(date);
                } catch (error) {
                    console.error("DEBUG: Right drop - error scheduling ticket:", error);
                }
                localStorage.removeItem("draggedTicket");
            }
        };

        const updateDayTickets = async (date) => {
            try {
                const {
                    todoTickets,
                    scheduleTickets
                } = await fetchTicketsForDate(date);

                // Keresd meg az adott napot a calendarDays tömbben, és frissítsd az értékeket
                const dayIndex = calendarDays.value.findIndex(day => day.date === date);
                if (dayIndex !== -1) {
                    calendarDays.value[dayIndex].todoTickets = todoTickets;
                    calendarDays.value[dayIndex].scheduleTickets = scheduleTickets;
                }
            } catch (error) {
                console.error("Error updating day tickets:", error);
            }
        };

        const closeTimeModal = () => {
            showTimeModal.value = false;
            modalStartTime.value = "";
            modalEndTime.value = "";
            modalErrorMessage.value = "";
            localStorage.removeItem("draggedTicket");
        };

        const confirmTimeModal = async () => {
            // Ellenőrizzük, hogy mindkét időpont ki van-e töltve
            if (!modalStartTime.value || !modalEndTime.value) {
                modalErrorMessage.value = "Both start and end times must be provided.";
                return;
            }

            // Ellenőrizzük, hogy a kezdési idő kisebb-e, mint a befejezési idő
            if (modalStartTime.value >= modalEndTime.value) {
                modalErrorMessage.value = "Start time must be less than end time.";
                return;
            }

            const calendarId = localStorage.getItem("calendarId");
            if (!calendarId) {
                modalErrorMessage.value = "Calendar ID is missing.";
                return;
            }

            const scheduleTicketPayload = {
                CalendarId: parseInt(calendarId),
                Date: dropDate.value,
                TicketId: dropTicketData.value.id,
                StartTime: modalStartTime.value,
                EndTime: modalEndTime.value
            };

            try {
                await api.post("/api/Tickets/ScheduleTicket", scheduleTicketPayload);
                emitter.emit("ticketScheduled", {
                    ticketId: dropTicketData.value.id
                });

                // **Frissítsük az adott nap ticketjeit az API-ból**
                await updateDayTickets(dropDate.value);
            } catch (error) {
                console.error("DEBUG: Left drop - error scheduling ticket:", error);
            }

            localStorage.removeItem("draggedTicket");
            closeTimeModal();
        };

        const onGlobalDragStart = () => {
            isDraggingTicket.value = true;
        };

        const onGlobalDragEnd = () => {
            isDraggingTicket.value = false;
        };

        watchEffect(() => {
            if (calendar.value.id) {
                fetchCalendarDays();
            }
        });

        onMounted(() => {
            fetchCalendar();
            fetchCalendarDays()
            window.addEventListener("dragstart", onGlobalDragStart);
            window.addEventListener("dragend", onGlobalDragEnd);
        });

        onUnmounted(() => {
            window.removeEventListener("dragstart", onGlobalDragStart);
            window.removeEventListener("dragend", onGlobalDragEnd);
        });

        window.addEventListener("dragend", () => {
            isDraggingTicket.value = false;
        });

        return {
            formattedMonth,
            daysOfWeek,
            calendarDays,
            calendar,
            errorMessage,
            goToDay,
            prevMonth,
            nextMonth,
            onTicketDrop,
            isDraggingTicket,
            showTimeModal,
            modalStartTime,
            modalEndTime,
            modalErrorMessage,
            closeTimeModal,
            confirmTimeModal,
        };
    },
};
</script>

<style scoped>
/* A két ticket lista konténere */
.ticket-lists-container {
    display: flex;
    justify-content: space-between; /* Egyenletes elrendezés */
    align-items: stretch;
    width: 100%;
    gap: 5px; /* Kis térköz a két lista között */
}

/* Egyéni ticket lista beállítások */
.ticket-list {
    display: flex;
    flex-direction: column; /* Ticketek egymás alatt */
    align-items: center; /* Középre igazítás vízszintesen */
    justify-content: flex-start; /* Ticketek felülre igazítása */
    padding: 5px;
    max-height: 60px;
    min-height: 60px; /* Megakadályozza az összenyomódást */
    overflow-y: auto; /* Csak függőleges görgetés engedélyezett */
    overflow-x: hidden; /* Vízszintes görgetés tiltása */
    width: 50%; /* Mindkét lista egyforma szélességet kap */
}

/* A scheduleTickets (bal oldali) lista */
.ticket-list:first-child {
    border-right: 2px solid #ccc; /* Szeparáló vonal a két lista között */
}

/* A todoTickets (jobb oldali) lista */
.ticket-list:last-child {
    align-items: flex-end; /* Ticketek jobbra zárása */
}

/* Egyéni ticket beállítások */
.ticket {
    width: 90%; /* A ticketek szélessége igazodik a listához */
    font-size: 10px; /* Kisebb, de olvasható betűméret */
    padding: 2px 4px; /* Kis padding a jobb elrendezésért */
    border-radius: 4px; /* Lekerekített sarkak */
    text-align: center; /* Szöveg középre igazítása */
    white-space: nowrap; /* Ne törjön több sorba */
    overflow: hidden;
    text-overflow: ellipsis; /* Ha túl hosszú a szöveg, "..."-al rövidül */
    flex-shrink: 0; /* Ne nyomódjon össze, ha sok elem van */
}


.calendar-layout {
    display: flex;
    justify-content: flex-start;
    align-items: flex-start;
    width: 100vw;
    height: 90vh;
}

.calendar-container {
    width: 66vw;
    height: 90vh;
    background: #e3f2fd;
    padding: 20px;
    border-radius: 10px;
}

.calendar-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    background: #a5d6a7;
    padding: 10px;
    border-radius: 5px;
}

.navigation button {
    background: none;
    border: none;
    font-size: 18px;
    cursor: pointer;
    margin: 0 5px;
    transition: all 0.2s ease-in-out;
}

.navigation button:hover {
    transform: scale(0.9);
    background: rgba(0, 0, 0, 0.1);
}

.calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 5px;
    margin-top: 10px;
}

.calendar-day {
    background: #fff;
    border-radius: 5px;
    height: 80px;
    position: relative;
    cursor: pointer;
    overflow: hidden;
    transition: all 0.2s ease-in-out;
    text-align: center;
    font-weight: bold;
    display: flex;
    flex-direction: column;
    /* A tartalom vertikálisan rendezi az elemeket */
    align-items: center;
    justify-content: space-between;
    padding: 5px;
    /* Hogy ne legyen teljesen szétnyomva */
}

.calendar-day:hover:not(.header) {
    background: #bdbdbd;
    transform: scale(0.95);
}

.calendar-day.header {
    font-weight: bold;
    background: #90caf9;
    cursor: default;
    transform: none !important;
    height: auto;
    line-height: normal;
}

.other-month {
    color: gray;
    opacity: 0.5;
}

.error {
    color: red;
    text-align: center;
}

/* Drop divider styling – only visible during dragging */
.drop-divider {
    position: absolute;
    top: 0;
    bottom: 0;
    left: 50%;
    width: 2px;
    background-color: #000;
    z-index: 1;
    pointer-events: none;
}

/* Modal stílusok a Modal komponensben vannak definiálva */
</style>
