<template>
<div class="calendar-layout">
    <div class="calendar-container">
        <div class="calendar-header">
            <div class="header-top">
                <b class="calendar-name" v-if="calendar.name">{{ calendar.name }}</b>
                <h2 v-else>Loading...</h2>
                <div class="header-buttons">
                    <button class="add-button" @click="goToCalendars">+</button>
                    <button @click.stop="openCopyTicketModal()" class="copy-btn">Copy</button>
                </div>
            </div>
            <div class="header-divider"></div>
            <div class="header-bottom">
                <div class="date-navigation">
                    <b>{{ formattedMonth }}</b>
                    <div class="navigation">
                        <button @click="prevMonth">◀</button>
                        <button @click="nextMonth">▶</button>
                    </div>
                </div>
            </div>
        </div>

        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <div class="calendar-header-grid">
            <div v-for="day in daysOfWeek" :key="day" class="calendar-day header">
                {{ day }}
            </div>
        </div>
        <div class="calendar-grid" :style="{ gridTemplateRows: gridRowStyle }">
            <div v-for="day in daysInMonth" :key="day.date" class="calendar-day" :class="{ 'other-month': !day.isCurrentMonth, 'today': isToday(day.date)}" @click="goToDay(day.date)" @drop="onTicketDrop($event, day.date)" @dragover.prevent>
                <div class="day-number">{{ day.number }}</div>
                <div class="ticket-lists-container">
                    <div class="ticket-list">
                        <div v-for="(ticket, index) in day.scheduleTickets" :key="ticket.name + '-' + index" class="ticket" :class="{ 'completed-ticket': ticket.isCompleted }" :style="{ backgroundColor: ticket.color }">
                            {{ ticket.name }}
                        </div>
                    </div>
                    <div class="ticket-list">
                        <div v-for="(ticket, index) in day.todoTickets" :key="ticket.name + '-' + index" class="ticket" :class="{ 'completed-ticket': ticket.isCompleted }" :style="{ backgroundColor: ticket.color }">
                            {{ ticket.name }}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <Modal :show="showTimeModal" title="Ticket Time" confirmText="Mentés" @close="closeTimeModal" @confirm="confirmTimeModal">
        <div class="modal-content">
            <label for="start-time">Kezdés:</label>
            <input id="start-time" type="time" v-model="modalStartTime" />
            <label for="end-time">Befejezés:</label>
            <input id="end-time" type="time" v-model="modalEndTime" />
            <p v-if="modalErrorMessage" class="error">{{ modalErrorMessage }}</p>
        </div>
    </Modal>

    <CopyTicketModal :show="showCopyTicketModal" @update:show="showCopyTicketModal = $event" />
</div>
</template>

<script>
import {
    ref,
    computed,
    onMounted,
    onBeforeUnmount,
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
import {
    setErrorMessage
} from "@/utils/errorHandler";
import
CopyTicketModal from "@/components/molecules/CopyTicketModal.vue";
import {
    connection,
    ensureConnected
} from "@/services/signalRService";

export default {
    components: {
        Modal,
        CopyTicketModal,
    },
    setup() {
        const currentDate = ref(new Date());
        const currentDay = ref("");
        const calendar = ref({
            id: null,
            name: ""
        });
        const errorMessage = ref("");
        const router = useRouter();

        const showTimeModal = ref(false);
        const modalStartTime = ref("");
        const modalEndTime = ref("");
        const modalErrorMessage = ref("");
        const dropTicketData = ref(null);
        const dropDate = ref("");
        const daysInMonth = ref([]);
        const showCopyTicketModal = ref(false);
        const selectedTicketId = ref(null);
        const gridRowStyle = ref("repeat(6, minmax(0, 1fr))");

        const dayViewEvents = [
            "TicketScheduled",
            "TicketCopiedInCalendar",
            "TicketCreatedInDayView",
            "TicketReorderedInDayView",
            "TicketMovedBackToCalendar",
            "TicketUpdatedInDayView",
            "TicketCompletedUpdatedInDayView",
            "TicketMovedBetweenDays",
            "TicketDeletedInDayView"
        ];

        const calendarId = ref(localStorage.getItem("calendarId"));

        const daysOfWeek = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

        const formattedMonth = computed(() => {
            return currentDate.value.toLocaleDateString("en-US", {
                year: "numeric",
                month: "long",
            });
        });

        const openCopyTicketModal = () => {
            showCopyTicketModal.value = true;
        };

        const fetchTicketsForDate = async (date, dayId = null) => {
            try {
                let endpoint = dayId ?
                    `/api/Tickets/AllDailyTickets/${dayId}` :
                    `/api/Tickets/AllDailyTickets/${date}/${calendar.value.id}`;

                const response = await api.get(endpoint);
                const tickets = response.data.map(ticket => ({
                    name: ticket.name,
                    startTime: ticket.startTime,
                    currentPosition: ticket.currentPosition,
                    color: ticket.color,
                    isCompleted: ticket.isCompleted
                }));

                const todoTickets = tickets
                    .filter(ticket => ticket.startTime === null)
                    .sort((a, b) => a.currentPosition - b.currentPosition);

                const scheduleTickets = tickets
                    .filter(ticket => ticket.startTime !== null)
                    .sort((a, b) => a.startTime.localeCompare(b.startTime));

                return {
                    todoTickets,
                    scheduleTickets
                };
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(errorMessage, "Error fetching tickets.");
                    console.error("Error fetching tickets:", error);
                }
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

            const formatDate = (dateObj) => {
                return `${dateObj.getFullYear()}-${String(dateObj.getMonth() + 1).padStart(2, '0')}-${String(dateObj.getDate()).padStart(2, '0')}`;
            };

            const startDate = formatDate(new Date(year, month - 1, prevMonthLastDate - startOffset + 1));
            const endDate = formatDate(new Date(year, month + 1, endOffset));

            let dayIdMap = {};

            try {
                const response = await api.get(`/api/days/range/${startDate}/${endDate}/${calendar.value.id}`);
                if (response.data.days && response.data.days.length > 0) {
                    response.data.days.forEach(day => {
                        dayIdMap[day.date] = day.id;
                    });
                }
            } catch (error) {
                console.error("Error fetching existing days:", error);
            }

            const getDayIdByDate = (date) => dayIdMap[date] || null;

            for (let i = startOffset - 1; i >= 0; i--) {
                let dateObj = new Date(year, month - 1, prevMonthLastDate - i);
                let date = formatDate(dateObj);
                let dayId = getDayIdByDate(date);

                let {
                    todoTickets,
                    scheduleTickets
                } = dayId ? await fetchTicketsForDate(null, dayId) : {
                    todoTickets: [],
                    scheduleTickets: []
                };

                days.push({
                    number: prevMonthLastDate - i,
                    date: date,
                    isCurrentMonth: false,
                    todoTickets: todoTickets,
                    scheduleTickets: scheduleTickets
                });
            }

            for (let i = 1; i <= lastDateOfMonth; i++) {
                let dateObj = new Date(year, month, i);
                let date = formatDate(dateObj);
                let dayId = getDayIdByDate(date);

                let {
                    todoTickets,
                    scheduleTickets
                } = dayId ? await fetchTicketsForDate(null, dayId) : {
                    todoTickets: [],
                    scheduleTickets: []
                };

                days.push({
                    number: i,
                    date: date,
                    isCurrentMonth: true,
                    todoTickets: todoTickets,
                    scheduleTickets: scheduleTickets
                });
            }

            for (let i = 1; i <= endOffset; i++) {
                let dateObj = new Date(year, month + 1, i);
                let date = formatDate(dateObj);
                let dayId = getDayIdByDate(date);

                let {
                    todoTickets,
                    scheduleTickets
                } = dayId ? await fetchTicketsForDate(null, dayId) : {
                    todoTickets: [],
                    scheduleTickets: []
                };

                days.push({
                    number: i,
                    date: date,
                    isCurrentMonth: false,
                    todoTickets: todoTickets,
                    scheduleTickets: scheduleTickets
                });
            }

            daysInMonth.value = days;
            updateGridRowStyle();
        };

        const updateGridRowStyle = () => {
            gridRowStyle.value = Math.ceil(daysInMonth.value.length / 7) < 6 ?
                "repeat(5, minmax(0, 1fr))" :
                "repeat(6, minmax(0, 1fr))";
        };

        const isToday = (date) => {
            return date === currentDay.value;
        };

        const goToDay = (date) => {
            router.push(`/day/${date}`);
        };

        const goToCalendars = () => {
            router.push("/calendars");
        };

        const fetchCalendar = async () => {
            try {
                const response = await api.get(`/api/Calendars/${calendarId.value}`);
                calendar.value = response.data;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    console.error("Error loading calendar:", error);
                    setErrorMessage(errorMessage, "Failed to load calendar.");
                }
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

        const validateTimeFields = (startTime, endTime) => {
            if (!startTime || !endTime) {
                return "Both start and end times are required.";
            }

            const [startHours, startMinutes] = startTime.split(":").map(Number);
            const [endHours, endMinutes] = endTime.split(":").map(Number);

            if (
                isNaN(startHours) || isNaN(startMinutes) ||
                isNaN(endHours) || isNaN(endMinutes)
            ) {
                return "Invalid time format.";
            }

            const startTotalMinutes = startHours * 60 + startMinutes;
            const endTotalMinutes = endHours * 60 + endMinutes;

            if (startTotalMinutes >= endTotalMinutes - 9) {
                return "The start time must be at least 10 minutes earlier than the end time.";
            }

            return null; // No errors
        };

        const onTicketDrop = async (event, date) => {
            const ticketDataStr = localStorage.getItem("draggedTicket");
            if (!ticketDataStr) return;
            const ticketData = JSON.parse(ticketDataStr);

            const dropZone = event.currentTarget;
            const rect = dropZone.getBoundingClientRect();
            const x = event.clientX - rect.left;
            const side = x < rect.width / 2 ? "left" : "right";

            if (side === "left") {
                dropTicketData.value = ticketData;
                dropDate.value = date;
                showTimeModal.value = true;
            } else {
                const scheduleTicketPayload = {
                    CalendarId: parseInt(calendarId.value),
                    Date: date,
                    TicketId: ticketData.id,
                    StartTime: null,
                    EndTime: null
                };

                try {
                    await api.post("/api/Tickets/ScheduleTicket", scheduleTicketPayload);

                    emitter.emit("ticketScheduled", {
                        ticketId: ticketData.id
                    });

                    await updateDayTickets(date);
                } catch (error) {
                    if (error.response && error.response.status === 403) {
                        setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                        console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    } else {
                        setErrorMessage(errorMessage, "Right drop - error scheduling ticket.")
                        console.error("Right drop - error scheduling ticket:", error);
                    }
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

                const dayIndex = daysInMonth.value.findIndex(day => day.date === date);
                if (dayIndex !== -1) {
                    daysInMonth.value[dayIndex].todoTickets = todoTickets;
                    daysInMonth.value[dayIndex].scheduleTickets = scheduleTickets;
                }
            } catch (error) {
                setErrorMessage(errorMessage, "Error updating day tickets.");
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
            const validationError = validateTimeFields(modalStartTime.value, modalEndTime.value);
            if (validationError) {
                setErrorMessage(modalErrorMessage, validationError);
                return;
            }

            const scheduleTicketPayload = {
                CalendarId: parseInt(calendarId.value),
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

                await updateDayTickets(dropDate.value);
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(errorMessage, "Left drop - error scheduling ticket.");
                    console.error("Left drop - error scheduling ticket:", error);
                }
            }

            localStorage.removeItem("draggedTicket");
            closeTimeModal();
        };

        const updateCurrentDayAtMidnight = () => {
            currentDay.value = getLocalDateString();
            const now = new Date();
            const millisTillMidnight = new Date(now.getFullYear(), now.getMonth(), now.getDate() + 1, 0, 0, 0) - now;

            setTimeout(() => {
                currentDay.value = getLocalDateString();

                setInterval(() => {
                    currentDay.value = getLocalDateString();
                }, 24 * 60 * 60 * 1000);
            }, millisTillMidnight);
        };

        const getLocalDateString = () => {
            const now = new Date();
            return now.getFullYear() + '-' +
                String(now.getMonth() + 1).padStart(2, '0') + '-' +
                String(now.getDate()).padStart(2, '0');
        };

        currentDay.value = getLocalDateString();

        watchEffect(() => {
            if (calendar.value.id) {
                fetchCalendarDays();
            }
        });

        onMounted(async () => {
            fetchCalendar();
            fetchCalendarDays();
            updateCurrentDayAtMidnight();
            updateGridRowStyle();
            emitter.on("calendarUpdated", fetchCalendarDays);
            window.addEventListener("dragstart", onGlobalDragStart);
            window.addEventListener("dragend", onGlobalDragEnd);

            await ensureConnected();
            await connection.invoke("JoinGroup", calendarId.value);
            dayViewEvents.forEach(event => {
                connection.on(event, async (receivedDayDate) => {
                    const formattedDate = receivedDayDate.split("T")[0];
                    updateDayTickets(formattedDate);
                });
            });
            connection.on("CalendarDeleted", async () => {
                window.location.reload();
            });
            connection.on("CalendarCopied", async () => {
                fetchCalendarDays();
            });
        });

        onBeforeUnmount(() => {
            if (calendarId.value) {
                connection.invoke("LeaveGroup", calendarId.value);
            }
        });

        onUnmounted(() => {
            emitter.off("calendarUpdated", fetchCalendarDays);
            window.removeEventListener("dragstart", onGlobalDragStart);
            window.removeEventListener("dragend", onGlobalDragEnd);
        });

        return {
            formattedMonth,
            daysOfWeek,
            daysInMonth,
            calendar,
            errorMessage,
            goToDay,
            prevMonth,
            nextMonth,
            onTicketDrop,
            showTimeModal,
            modalStartTime,
            modalEndTime,
            modalErrorMessage,
            closeTimeModal,
            confirmTimeModal,
            goToCalendars,
            openCopyTicketModal,
            selectedTicketId,
            showCopyTicketModal,
            isToday,
            gridRowStyle,
        };
    },
};
</script>

<style scoped>
.calendar-name {
    font-size: 20px;
}

.today {
    border: 3px solid #80ED99 !important;
}


.ticket-lists-container {
    display: flex;
    justify-content: space-between;
    align-items: stretch;
    width: 100%;
    gap: 5px;
}

.ticket-list {
    display: flex;
    flex-direction: column;
    align-items: stretch;
    width: 50%;
    max-height: 100%;
    overflow-y: auto;
    overflow-x: hidden;
    scrollbar-width: none;
    /* Firefox */
    padding-bottom: 4px;
}

.ticket-list::-webkit-scrollbar {
    display: none;
    /* Chrome, Safari, Edge */
}

.ticket-list:first-child {
    border-right: 2px solid #ccc;
}

.ticket-list:last-child {
    align-items: flex-end;
}

.add-button {
    background: #213A57;
    color: white;
    border: none;
    padding: 5px 10px;
    font-size: 18px;
    border-radius: 50%;
    cursor: pointer;
}

.copy-btn {
    background: #213A57;
    color: white;
    border: none;
    border-radius: 10px;
    cursor: pointer;
}

.ticket {
    width: 90%;
    font-size: 10px;
    border-radius: 4px;
    text-align: center;
    white-space: nowrap;
    overflow: hidden;
    flex-shrink: 0;
    position: relative;
}

.ticket::after {
    content: "";
    position: absolute;
    right: 0;
    top: 0;
    height: 100%;
    width: 15%;
    background: linear-gradient(to left, rgba(255, 255, 255, 1), rgba(255, 255, 255, 0));
    pointer-events: none;
}

.calendar-layout {
    display: flex;
    justify-content: flex-start;
    align-items: flex-start;
    width: 100%;
    height: 100%;
    box-sizing: border-box;
    min-height: 300px;
}

.calendar-container {
    display: flex;
    flex-direction: column;
    flex-grow: 1;
    width: 100%;
    padding: 18px;
    overflow: hidden;
    min-width: 330px;
    min-height: 300px;
    background: #e3f2fd;
}


.calendar-header {
    display: flex;
    box-sizing: border-box;
    flex-direction: column;
    align-items: center;
    background: #14919B;
    padding: 10px;
    border-radius: 5px;
    width: 100%;
    border: 1px solid black;
}

.header-top {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
}

.header-buttons {
    display: flex;
    gap: 10px;
}

.header-bottom {
    width: 100%;
    display: flex;
    justify-content: center;
}

.date-navigation {
    display: flex;
    align-items: center;
    gap: 5px;
}

.navigation {
    display: flex;
}

.header-top button {
    margin-left: 10px;
    transition: all 0.3s ease-in-out;
}

.header-top button:hover {
    transform: scale(1.1);
}

.navigation button {
    background: none;
    border: none;
    font-size: 18px;
    cursor: pointer;
    transition: all 0.3s ease-in-out;
}

.navigation button:hover {
    transform: scale(1.3);
    background: none;
}

.date-navigation b {
    display: inline-block;
    text-align: center;
    min-width: 120px;
}

.calendar-header-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 5px;
    width: 100%;
    text-align: center;
    margin-bottom: 5px;
    margin-top: 5px;
    height: auto;
}

.calendar-grid {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    grid-template-rows: repeat(6, minmax(0, 1fr));
    gap: 5px;
    width: 100%;
    height: calc(100vh - 220px);
    overflow: hidden;
}

.completed-ticket {
    text-decoration: line-through;
    opacity: 0.6;
}

.ticket-lists-container {
    flex: 1;
    display: flex;
    flex-direction: row;
    gap: 5px;
    overflow: hidden;
}

.calendar-day {
    background: #fff;
    border-radius: 5px;
    height: auto;
    position: relative;
    cursor: pointer;
    overflow: hidden;
    transition: all 0.2s ease-in-out;
    text-align: center;
    font-weight: bold;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: space-between;
    padding: 5px;
    min-width: 30px;
    border: 1px solid black;
}

.calendar-day:hover:not(.header) {
    background: #bdbdbd;
    transform: scale(0.95);
}

.calendar-day.header {
    font-weight: bold;
    background: #0AD1C8;
    cursor: default;
    transform: none !important;
    height: auto;
    line-height: normal;
}

.other-month {
    color: gray;
    opacity: 0.5;
    overflow: hidden;
    white-space: nowrap;
    text-overflow: ellipsis;
}

.error {
    color: red;
    text-align: center;
}

.header-divider {
    width: 100%;
    height: 2px;
    background: linear-gradient(to right, rgba(11, 100, 119, 0) 0%, #0B6477 50%, rgba(11, 100, 119, 0) 100%);
    border-radius: 2px;
    margin-top: 2px;
}

@media (max-width: 700px) {
    .calendar-container {
        width: 100%;
        height: 100%;
        background: #e3f2fd;
        padding: 20px;
        border-radius: 10px 10px 0 0;
    }
    
}
</style>