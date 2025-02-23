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
        <div
          v-for="day in calendarDays"
          :key="day.date"
          class="calendar-day"
          :class="{ 'other-month': !day.isCurrentMonth }"
          @click="goToDay(day.date)"
          @drop="onTicketDrop($event, day.date)"
          @dragover.prevent
        >
          <!-- Separator line, visible during dragging -->
          <div v-if="isDraggingTicket" class="drop-divider"></div>
          {{ day.number }}
        </div>
      </div>
    </div>
    <!-- Modal for entering ticket time (only shown for left drop) -->
    <Modal 
      :show="showTimeModal" 
      title="Ticket Time" 
      confirmText="Mentés" 
      @close="closeTimeModal" 
      @confirm="confirmTimeModal"
    >
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
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import api from "@/utils/config/axios-config";
import Modal from "@/components/Modal.vue";

export default {
  components: {
    Modal,
  },
  setup() {
    const currentDate = ref(new Date());
    const calendar = ref({ id: null, name: "" });
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

    const daysOfWeek = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

    const formattedMonth = computed(() => {
      return currentDate.value.toLocaleDateString("hu-HU", {
        year: "numeric",
        month: "long",
      });
    });

    const calendarDays = computed(() => {
      const year = currentDate.value.getFullYear();
      const month = currentDate.value.getMonth();
      const firstDayOfMonth = new Date(year, month, 1).getDay();
      const lastDateOfMonth = new Date(year, month + 1, 0).getDate();

      let days = [];
      let startOffset = firstDayOfMonth === 0 ? 6 : firstDayOfMonth - 1;
      let endOffset = (7 - ((startOffset + lastDateOfMonth) % 7)) % 7;

      const prevMonthLastDate = new Date(year, month, 0).getDate();
      for (let i = startOffset - 1; i >= 0; i--) {
        days.push({
          number: prevMonthLastDate - i,
          date: new Date(year, month - 1, prevMonthLastDate - i)
            .toISOString()
            .split("T")[0],
          isCurrentMonth: false,
        });
      }

      for (let i = 1; i <= lastDateOfMonth; i++) {
        days.push({
          number: i,
          date: new Date(year, month, i + 1)
            .toISOString()
            .split("T")[0],
          isCurrentMonth: true,
        });
      }

      for (let i = 1; i <= endOffset; i++) {
        days.push({
          number: i,
          date: new Date(year, month + 1, i)
            .toISOString()
            .split("T")[0],
          isCurrentMonth: false,
        });
      }

      return days;
    });

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
  
  console.log("DEBUG: Retrieved ticketData:", ticketData);
  
  const dropZone = event.currentTarget;
  const rect = dropZone.getBoundingClientRect();
  const x = event.clientX - rect.left; // X coordinate within the cell
  const side = x < rect.width / 2 ? "left" : "right";
  
  console.log("DEBUG: Drop event details:", { x, rectWidth: rect.width, side });
  
  if (side === "left") {
    // Left drop: open modal to input times.
    dropTicketData.value = ticketData;
    dropDate.value = date;
    showTimeModal.value = true;
  } else {
    // Right drop: normal processing via API call.
    const calendarId = localStorage.getItem("calendarId");
    const scheduleTicketPayload = {
      CalendarId: parseInt(calendarId),
      Date: date,
      Ticket: ticketData
    };
    
    console.log("DEBUG: ScheduleTicket payload:", scheduleTicketPayload);
    
    try {
      const response = await api.post("/api/Tickets/ScheduleTicket", scheduleTicketPayload);
      console.log("DEBUG: Right drop - scheduled ticket response:", response.data);
    } catch (error) {
      console.error("DEBUG: Right drop - error scheduling ticket:", error);
    }
    localStorage.removeItem("draggedTicket");
  }
};


    const closeTimeModal = () => {
      showTimeModal.value = false;
      modalStartTime.value = "";
      modalEndTime.value = "";
      modalErrorMessage.value = "";
      localStorage.removeItem("draggedTicket");
    };

    const confirmTimeModal = () => {
      // Ha mindkét időpont meg van adva, ellenőrizzük, hogy a kezdési idő kisebb-e, mint a befejezés.
      if (modalStartTime.value && modalEndTime.value && modalStartTime.value >= modalEndTime.value) {
        modalErrorMessage.value = "Kezdésnek kisebbnek kell lennie, mint a befejezés.";
        return;
      }
      const startTime = modalStartTime.value ? modalStartTime.value : null;
      const endTime = modalEndTime.value ? modalEndTime.value : null;
      // Logoljuk a modalban megadott időpontokat a ticket adataival együtt
      console.log(
        "Left drop confirmed - ticket data:",
        dropTicketData.value,
        "on date:",
        dropDate.value,
        "Start:",
        startTime,
        "End:",
        endTime
      );
      // További feldolgozás (pl. API hívás) itt is történhet, ha szükséges
      closeTimeModal();
    };

    const onGlobalDragStart = () => {
      isDraggingTicket.value = true;
    };

    const onGlobalDragEnd = () => {
      isDraggingTicket.value = false;
    };

    onMounted(() => {
      fetchCalendar();
      window.addEventListener("dragstart", onGlobalDragStart);
      window.addEventListener("dragend", onGlobalDragEnd);
    });

    onUnmounted(() => {
      window.removeEventListener("dragstart", onGlobalDragStart);
      window.removeEventListener("dragend", onGlobalDragEnd);
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
  line-height: 80px;
  font-weight: bold;
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