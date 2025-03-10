<template>
<div class="container">
    <button class="nav-btn left" @click="goToPreviousDay" @dragover.prevent @drop="handleDrop('previous')">&#9665;</button>
    <div class="content">
        <button class="add-ticket-btn" @click="showAddNewTicketModal = true">+ Add Ticket</button>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <div class="panels">
            <DayPanel class="panel" />
            <TodoList class="panel" />
        </div>
    </div>
    <button class="nav-btn right" @click="goToNextDay" @dragover.prevent @drop="handleDrop('next')">&#9655;</button>

    <Modal :show="showAddNewTicketModal" title="Add New Ticket" confirmText="Add" @close="showAddNewTicketModal = false" @confirm="handleAddNewTicket">
        <div class="modal-content">
            <label for="ticket-name">Ticket Name</label>
            <input id="ticket-name" v-model="newTicket.name" placeholder="Enter ticket name" required />
            <p v-if="nameErrorMessage" class="error">{{ nameErrorMessage }}</p>

            <label for="ticket-description">Description (optional)</label>
            <textarea id="ticket-description" v-model="newTicket.description" placeholder="Enter description"></textarea>

            <label for="ticket-priority">Priority (optional)</label>
            <input id="ticket-priority" v-model="newTicket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />

            <label for="ticket-start-time">Start Time (optional)</label>
            <input id="ticket-start-time" v-model="newTicket.startTime" type="time" />
            <p v-if="timeError" class="error">{{ timeError }}</p>

            <label for="ticket-end-time">End Time (optional)</label>
            <input id="ticket-end-time" v-model="newTicket.endTime" type="time" />

            <label for="ticket-calendar-list">Select Calendar List</label>
            <select id="ticket-calendar-list" v-model="newTicket.calendarListId">
                <option v-for="list in calendarLists" :key="list.id" :value="list.id">{{ list.name }}</option>
            </select>
            <p v-if="calendarListError" class="error">{{ calendarListError }}</p>
        </div>
    </Modal>
</div>
</template>

<script>
import {
    ref,
    onMounted,
    onUnmounted,
    computed,
    watch
} from "vue";
import {
    useRoute
} from "vue-router";
import DayPanel from "@/components/organisms/ScheduledPanel.vue";
import TodoList from "@/components/organisms/TodoList.vue";
import Modal from "@/components/molecules/Modal.vue";
import {
    addNewTicket
} from "@/components/atoms/AddNewTicket";
import api from "@/utils/config/axios-config";
import {
    emitter
} from "@/utils/eventBus";
import {
    validateNameField,
    validateTimeFieldsBothRequiredOrEmpty
} from "@/components/atoms/ValidateModalInputFields";
import {
    setErrorMessage
} from "@/utils/errorHandler";
import {
    useRouter
} from "vue-router";

export default {
    components: {
        DayPanel,
        TodoList,
        Modal,
    },
    setup() {
        const showAddNewTicketModal = ref(false);
        const newTicket = ref({
            name: "",
            description: "",
            priority: null,
            startTime: "",
            endTime: "",
            calendarListId: null,
        });
        const route = useRoute();
        const currentDayId = ref(null);
        const calendarId = ref(localStorage.getItem("calendarId"));
        const calendarLists = ref([]);
        const errorMessage = ref("");
        const nameErrorMessage = ref("");
        const calendarListError = ref("");
        const timeError = ref("");
        const router = useRouter();

        const changeTicketDate = async (ticketId, newDate) => {
            try {
                await api.put(`/api/Tickets/changeDate/${ticketId}`, JSON.stringify(newDate), {
                    headers: {
                        "Content-Type": "application/json"
                    }
                });
                emitter.emit("ticketDateChanged");
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    console.error("Error changing ticket date:", error);
                }
            }
        };

        // Kezeljük a ticket áthúzását a nyilakra
        const handleDrop = async (direction) => {
            const storedTicket = localStorage.getItem("draggedTicket");
            if (!storedTicket) return;

            const draggedTicket = JSON.parse(storedTicket);

            let newDate = new Date(route.params.date);
            if (direction === 'previous') {
                newDate.setDate(newDate.getDate() - 1);
            } else if (direction === 'next') {
                newDate.setDate(newDate.getDate() + 1);
            }

            const formattedDate = newDate.toISOString().split("T")[0];

            await changeTicketDate(draggedTicket.id, formattedDate);

            localStorage.removeItem("draggedTicket"); // Töröljük az adatokat a localStorage-ból
        };

        const currentDate = computed(() => {
            return route.params.date ? new Date(route.params.date) : new Date();
        });

        const goToPreviousDay = () => {
            const previousDate = new Date(currentDate.value);
            previousDate.setDate(previousDate.getDate() - 1);
            router.push({
                path: `/day/${previousDate.toISOString().split("T")[0]}`
            });
        };

        const goToNextDay = () => {
            const nextDate = new Date(currentDate.value);
            nextDate.setDate(nextDate.getDate() + 1);
            router.push({
                path: `/day/${nextDate.toISOString().split("T")[0]}`
            });
        };

        const handleAddNewTicket = async () => {
            nameErrorMessage.value = "";
            calendarListError.value = "";
            timeError.value = "";

            if (!newTicket.value.calendarListId) {
                setErrorMessage(calendarListError, "Please select a calendar list before adding a ticket.");
                return;
            }

            const nameValidationError = validateNameField(newTicket.value.name);
            if (nameValidationError) {
                setErrorMessage(nameErrorMessage, nameValidationError);
                return;
            }

            const timeValidationError = validateTimeFieldsBothRequiredOrEmpty(newTicket.value.startTime, newTicket.value.endTime);
            if (timeValidationError) {
                setErrorMessage(timeError, timeValidationError);
                return;
            }

            if (!currentDayId.value) {
                currentDayId.value = await createDay(route.params.date, calendarId.value);
            }

            await addNewTicket({
                    name: newTicket.value.name,
                    description: newTicket.value.description || null,
                    priority: newTicket.value.priority || null,
                    startTime: newTicket.value.startTime || null,
                    endTime: newTicket.value.endTime || null,
                    calendarListId: newTicket.value.calendarListId,
                    currentParentType: newTicket.value.startTime ? "ScheduledList" : "TodoList",
                    parentId: currentDayId.value,
                },
                null,
                [],
                showAddNewTicketModal,
                nameErrorMessage
            );

            if (!nameErrorMessage.value && !calendarListError.value && !timeError.value) {
                showAddNewTicketModal.value = false;

                if (newTicket.value.startTime) {
                    emitter.emit("newTicketCreatedWithTime");
                } else {
                    emitter.emit("newTicketCreatedWithoutTime");
                }

                newTicket.value = {
                    name: "",
                    description: "",
                    priority: null,
                    startTime: "",
                    endTime: "",
                    calendarListId: null,
                };
            }
        };

        const fetchDayId = async () => {
            try {
                const response = await api.get(`/api/Days/${route.params.date}/${calendarId.value}`);
                currentDayId.value = response.data?.id || null;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(errorMessage, "Error fetching day ID.");
                    console.error("Error fetching day ID:", error);
                }
            }
        };

        const fetchCalendarLists = async () => {
            try {
                const response = await api.get(`/api/CalendarLists/calendar/${calendarId.value}`);
                calendarLists.value = response.data;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage("Error fetching calendar lists.");
                    console.error("Error fetching calendar lists:", error);
                }
            }
        };

        fetchDayId();
        fetchCalendarLists();

        const createDay = async (date, calendarId) => {
            try {
                const response = await api.post("/api/Days/create", {
                    date,
                    calendarId
                });
                return response.data.id;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage("Error creating new day.");
                    console.error("Error creating new day:", error);
                    return null;
                }
            }
        };

        watch(() => route.params.date, async () => {
            await fetchDayId();
        });

        const handleDayDeletion = () => {
            currentDayId.value = null;
        };

        onMounted(() => {
            emitter.on("successfulDayDelete", handleDayDeletion);
        });

        onUnmounted(() => {
            emitter.off("successfulDayDelete", handleDayDeletion);
        });

        return {
            showAddNewTicketModal,
            newTicket,
            handleAddNewTicket,
            currentDayId,
            calendarId,
            calendarLists,
            errorMessage,
            nameErrorMessage,
            calendarListError,
            timeError,
            goToPreviousDay,
            goToNextDay,
            currentDate,
            handleDrop
        };
    },
};
</script>

<style scoped>
.container {
    display: flex;
    height: 100vh;
    padding: 20px;
}

.content {
    display: flex;
    flex-direction: column;
    flex: 1;
    width: 100%;
    align-items: center;
    justify-content: center;
}

.panels {
    display: flex;
    gap: 20px;
    width: 100%;
}

.panel {
    flex: 1;
    min-width: 45%;
    border: 1px solid #ccc;
    padding: 10px;
    border-radius: 5px;
    background: white;
}

.nav-btn {
    background-color: #007bff;
    color: white;
    border: none;
    padding: 12px 18px;
    font-size: 16px;
    cursor: pointer;
    border-radius: 30px;
    box-shadow: none;
    outline: none;
    transition: background-color 0.3s ease-in-out, transform 0.2s ease-in-out;
}

.nav-btn:hover {
    background-color: #0056b3;
}

.add-ticket-btn {
    margin-top: 20px;
    background-color: #4caf50;
    color: white;
    border: none;
    padding: 10px 15px;
    font-size: 16px;
    border-radius: 5px;
    cursor: pointer;
    align-self: center;
}

.add-ticket-btn:hover {
    background-color: #388e3c;
}

.modal-content {
    display: flex;
    flex-direction: column;
    gap: 10px;
    align-items: center;
}

.error {
    color: red;
    font-weight: bold;
    text-align: center;
}
</style>
