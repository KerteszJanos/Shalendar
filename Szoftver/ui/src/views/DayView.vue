<!--
  This is the main DayView layout component.

  Features:
  - Contains left and right navigation buttons to change the selected day.
  - Central content includes two panels:
    - DayPanel: scheduled tickets (time-bound tasks).
    - TodoList: unscheduled tickets (floating tasks).
  - Includes drag-and-drop ticket support between days via nav buttons.
  - Handles modal logic for creating new tickets with optional start/end times and other details.
  - Automatically fetches, deletes or creates the appropriate day record if needed.
-->

<template>
<div class="container">
    <button class="nav-btn left" @click="goToPreviousDay" @dragover.prevent @drop="handleDrop('previous')" :disabled="isHandlingDrop" title="Click to go to previous day, or drop a ticket to move it there">
        &#9665;
    </button>
    <div class="content">
        <button class="add-ticket-btn" @click="openAddTicketModal" title="Add new ticket to the day">+</button>
        <p v-if="errorMessage" class="error">{{ errorMessage }}</p>
        <div class="panels">
            <DayPanel class="dayPanel" />
            <TodoList class="todoList" />
        </div>
    </div>
    <button class="nav-btn right" @click="goToNextDay" @dragover.prevent @drop="handleDrop('next')" :disabled="isHandlingDrop" title="Click to go to next day, or drop a ticket to move it there">
        &#9655;
    </button>

    <Modal :show="showAddNewTicketModal" title="Add New Ticket" confirmText="Add" @close="showAddNewTicketModal = false" @confirm="handleAddNewTicket">
        <div class="modal-content">
            <label for="ticket-name" class="required-label">Ticket Name</label>
            <input id="ticket-name" v-model="newTicket.name" placeholder="Enter ticket name" required />
            <p v-if="nameErrorMessage" class="error">{{ nameErrorMessage }}</p>

            <label for="ticket-calendar-list" class="required-label">Select Calendar List</label>
            <DropdownSelect v-model="newTicket.calendarListId" :calendarLists="calendarLists" />
            <p v-if="calendarListError" class="error">{{ calendarListError }}</p>
            <p v-if="calendarLists && calendarLists.length === 0" class="error">
                No calendar lists available. Please create one first!
            </p>

            <label for="ticket-description">Description (optional)</label>
            <textarea id="ticket-description" v-model="newTicket.description" placeholder="Enter description"></textarea>

            <label for="ticket-priority">Priority (optional)</label>
            <input id="ticket-priority" v-model="newTicket.priority" type="number" min="1" max="9" placeholder="Enter priority (1-9)" />
            <p v-if="priorityErrorMessage" class="error">{{ priorityErrorMessage }}</p>

            <label for="ticket-start-time">Start Time (optional)</label>
            <input id="ticket-start-time" v-model="newTicket.startTime" type="time" />
            <p v-if="timeError" class="error">{{ timeError }}</p>

            <label for="ticket-end-time">End Time (optional)</label>
            <input id="ticket-end-time" v-model="newTicket.endTime" type="time" />
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
    validateTimeFieldsBothRequiredOrEmpty,
    validatePriorityField
} from "@/components/atoms/ValidateModalInputFields";
import {
    setErrorMessage
} from "@/utils/errorHandler";
import {
    useRouter
} from "vue-router";
import DropdownSelect from "@/components/atoms/DropdownSelect.vue";

export default {
    components: {
        DayPanel,
        TodoList,
        Modal,
        DropdownSelect
    },
    setup() {
        // ---------------------------------
        // Constants		               |
        // --------------------------------- 
        const route = useRoute();
        const router = useRouter();

        // ---------------------------------
        // Reactive state		           |
        // ---------------------------------
        const showAddNewTicketModal = ref(false);
        const currentDayId = ref(null);
        const calendarId = ref(localStorage.getItem("calendarId"));
        const calendarLists = ref([]);
        const errorMessage = ref("");
        const nameErrorMessage = ref("");
        const priorityErrorMessage = ref("");
        const calendarListError = ref("");
        const isHandlingDrop = ref(false);
        const timeError = ref("");
        const newTicket = ref({
            name: "",
            description: "",
            priority: null,
            startTime: "",
            endTime: "",
            calendarListId: null,
        });
        // Non-reactive computed variable
        const currentDate = computed(() => {
            return route.params.date ? new Date(route.params.date) : new Date();
        });
        // ---------------------------------
        // Methods		                   |
        // ---------------------------------
        // --------------
        // Modals   	|
        // --------------
        const openAddTicketModal = () => {
            nameErrorMessage.value = "";
            priorityErrorMessage.value = "";
            calendarListError.value = "";
            timeError.value = "";

            newTicket.value = {
                name: "",
                description: "",
                priority: null,
                startTime: "",
                endTime: "",
                calendarListId: null,
            };

            showAddNewTicketModal.value = true;
        };

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

        const handleDrop = async (direction) => {
            if (isHandlingDrop.value) return;
            isHandlingDrop.value = true;

            const storedTicket = localStorage.getItem("draggedTicket");
            if (!storedTicket) {
                isHandlingDrop.value = false;
                return;
            }

            const draggedTicket = JSON.parse(storedTicket);

            let newDate = new Date(route.params.date);
            if (direction === 'previous') {
                newDate.setDate(newDate.getDate() - 1);
            } else if (direction === 'next') {
                newDate.setDate(newDate.getDate() + 1);
            }

            const formattedDate = `${newDate.getFullYear()}-${String(newDate.getMonth() + 1).padStart(2, '0')}-${String(newDate.getDate()).padStart(2, '0')}`;

            try {
                await changeTicketDate(draggedTicket.id, formattedDate);
            } catch (error) {
                console.error("Hiba a drop közben:", error);
            }

            localStorage.removeItem("draggedTicket");
            isHandlingDrop.value = false;
        };

        const goToPreviousDay = () => {
            const previousDate = new Date(currentDate.value);
            previousDate.setDate(previousDate.getDate() - 1);

            const formattedDate = `${previousDate.getFullYear()}-${String(previousDate.getMonth() + 1).padStart(2, '0')}-${String(previousDate.getDate()).padStart(2, '0')}`;

            router.push({
                path: `/day/${formattedDate}`
            });
        };

        const goToNextDay = () => {
            const nextDate = new Date(currentDate.value);
            nextDate.setDate(nextDate.getDate() + 1);

            const formattedDate = `${nextDate.getFullYear()}-${String(nextDate.getMonth() + 1).padStart(2, '0')}-${String(nextDate.getDate()).padStart(2, '0')}`;

            router.push({
                path: `/day/${formattedDate}`
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

            if (newTicket.value.priority !== null) {
                const priorityValidationError = validatePriorityField(newTicket.value.priority);
                if (priorityValidationError) {
                    setErrorMessage(priorityErrorMessage, priorityValidationError);
                    return;
                }
            }

            // If the day does not exist yet in the database, create it so the ticket has a valid parent reference.
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

        // --------------
        // Helpers	    |
        // --------------
        const handleDayDeletion = () => {
            currentDayId.value = null;
        };

        // ---------------------------------
        // Lifecycle hooks		           |
        // ---------------------------------
        watch(() => route.params.date, async () => {
            await fetchDayId();
        });

        onMounted(() => {
            fetchDayId();
            fetchCalendarLists();
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
            handleDrop,
            priorityErrorMessage,
            openAddTicketModal,
            isHandlingDrop
        };
    },
};
</script>

<style scoped>
.nav-btn:disabled {
    background-color: #213A57;
    cursor: not-allowed;
    pointer-events: none;
    opacity: 1;
    transition: none;
}

.nav-btn:disabled:hover {
    background-color: #213A57;
}

.container {
    display: flex;
    height: 100%;
    width: 100%;
    box-sizing: border-box;
    padding-top: 20px;
    padding-bottom: 20px;
    min-width: 0;
}

.content {
    display: flex;
    flex-direction: column;
    flex: 1;
    width: 100%;
    height: 100%;
    align-items: center;
    justify-content: center;
}

.panels {
    display: flex;
    flex: 1;
    gap: 20px;
    width: 100%;
    height: 100%;
}

.dayPanel {
    flex: 1;
    min-width: 30%;
    border: 1px solid #ccc;
    border-radius: 5px;
    background: #e3f2fd;
}

.todoList {
    flex: 1;
    min-width: 30%;
    border: 1px solid #ccc;
    border-radius: 5px;
    background: #d5f9ea;
}

.nav-btn {
    background-color: #213A57;
    height: 90%;
    color: white;
    border: none;
    padding: 12px 18px;
    font-size: 16px;
    cursor: pointer;
    box-shadow: none;
    outline: none;
    transition: background-color 0.3s ease-in-out, transform 0.2s ease-in-out;
    align-self: center;
}

.nav-btn.left {
    border-radius: 0 10px 10px 0;
    margin-right: 10px;
}

.nav-btn.right {
    border-radius: 10px 0 0 10px;
    margin-left: 10px;
}

.nav-btn:hover {
    background-color: #3f5d80;
}

.add-ticket-btn {
    position: absolute;
    top: +65px;
    background-color: #213A57;
    color: white;
    border: none;
    padding: 12px 20px;
    font-size: 18px;
    border-radius: 50%;
    cursor: pointer;
    transition: all 0.3s ease-in-out;
    box-shadow: 0px 4px 10px rgba(0, 0, 0, 0.2);
    z-index: 10;
}

.add-ticket-btn:hover {
    background-color: #3f5d80;
    transform: scale(1.1);
}

.modal-content {
    display: flex;
    flex-direction: column;
    gap: 10px;
    align-items: center;
}

.error {
    color: red;
    text-align: center;
}

@media (max-width: 849px) {
    .panels {
        flex-direction: column;
    }

    .todoList {
        order: -1;
    }

    .dayPanel {
        order: 1;
    }
}
</style>
