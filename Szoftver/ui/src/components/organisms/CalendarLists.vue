<template>
<div class="lists-container">
    <div class="header">
        <h2>To be scheduled</h2>
        <button class="add-button" @click="openAddNewCalendarListModal">+</button>
    </div>

    <p v-if="loading">Loading...</p>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <div class="lists-content" v-if="calendarLists.length > 0">
        <div v-for="list in calendarLists" :key="list.id" class="list-item" :style="{ backgroundColor: list.color || '#CCCCCC' }">
            <p class="list-title">{{ list.name }}</p>
            <button class="edit-list-button" @click="openEditListModal(list)">Edit</button>
            <div class="ticket-list" v-if="list.tickets && list.tickets.length > 0">
                <draggable class="ticket" v-model="list.tickets" @end="onTicketDragEnd(list)" :group="{ name: 'tickets', pull: true, put: false }" itemKey="id">
                    <template #item="{ element }">
                        <div class="ticket-item" draggable="true" @dragstart="onTicketDragStart(element)" @click="openEditTicketModal(element)" :style="{ backgroundColor: list.color || '#CCCCCC' }">
                            <input type="checkbox" class="ticket-checkbox" :checked="element.isCompleted" @click.stop="toggleCompletion(element)" />
                            <p><strong>{{ element.name }}</strong></p>
                            <p v-if="element.description">{{ element.description }}</p>
                            <p v-if="element.priority">Priority: {{ element.priority }}</p>
                            <button @click.stop="openCopyTicketModal(element.id)" class="copy-btn">
                                Copy
                            </button>
                            <button @click.stop="handleDelete(element.id, list)" class="delete-btn">
                                Delete
                            </button>
                        </div>
                    </template>
                </draggable>
            </div>
            <p v-else>No tickets.</p>
            <button class="add-ticket-button" @click="openAddNewTicketModal(list.id)">+</button>
        </div>
    </div>
    <p v-else-if="!errorMessage">Add a list to start scheduling your stuff :)</p>

    <Modal :show="showAddNewCalendarListModal" title="Add New List" confirmText="Add" @close="showAddNewCalendarListModal = false" @confirm="handleAddNewCalendarList">
        <div class="modal-content">
            <label for="list-name">List Name</label>
            <input id="list-name" v-model="newList.name" placeholder="Enter list name" required />
            <p v-if="newListError" class="error">{{ newListError }}</p>

            <div class="color-picker">
                <input v-model="newList.color" type="color" />
                <div class="color-preview" :style="{ backgroundColor: newList.color }"></div>
            </div>
        </div>
    </Modal>

    <Modal :show="showAddNewTicketModal" title="Add New Ticket" confirmText="Add" @close="showAddNewTicketModal = false" @confirm="handleAddNewTicket">
        <div class="modal-content">
            <label for="ticket-name">Ticket Name</label>
            <input id="ticket-name" v-model="newTicket.name" placeholder="Enter ticket name" required />
            <p v-if="newTicketError" class="error">{{ newTicketError }}</p>

            <label for="ticket-description">Description (optional)</label>
            <textarea id="ticket-description" v-model="newTicket.description" placeholder="Enter description"></textarea>

            <label for="ticket-priority">Priority (optional)</label>
            <input id="ticket-priority" v-model="newTicket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />
        </div>
    </Modal>

    <Modal :show="showEditCalendarListModal" title="Edit List" confirmText="Save" @close="showEditCalendarListModal = false" @confirm="handleUpdateCalendarList">
        <div class="modal-content">
            <label for="edit-list-name">List Name</label>
            <input id="edit-list-name" v-model="editedList.name" placeholder="Enter list name" required />
            <p v-if="editListError" class="error">{{ editListError }}</p>

            <div class="color-picker">
                <input v-model="editedList.color" type="color" />
                <div class="color-preview" :style="{ backgroundColor: editedList.color }"></div>
            </div>

            <button class="delete-list-button" @click="confirmDeleteCalendarList">Delete List</button>
        </div>
    </Modal>

    <Modal :show="showEditTicketModal" title="Edit Ticket" confirmText="Save" @close="showEditTicketModal = false" @confirm="handleUpdateTicket">
        <div class="modal-content">
            <label for="edit-ticket-name">Ticket Name</label>
            <input id="edit-ticket-name" v-model="editedTicket.name" placeholder="Enter ticket name" required />
            <p v-if="editTicketError" class="error">{{ editTicketError }}</p>

            <label for="edit-ticket-description">Description</label>
            <textarea id="edit-ticket-description" v-model="editedTicket.description" placeholder="Enter description"></textarea>

            <label for="edit-ticket-priority">Priority</label>
            <input id="edit-ticket-priority" v-model="editedTicket.priority" type="number" min="1" max="10" placeholder="Enter priority (1-10)" />
        </div>
    </Modal>

    <CopyTicketModal :show="showCopyTicketModal" :ticketId="selectedTicketId" @update:show="showCopyTicketModal = $event" />
</div>
</template>

<script>
import {
    ref,
    onMounted,
    watch,
    onBeforeUnmount
} from "vue";
import api from "@/utils/config/axios-config";
import Modal from "@/components/molecules/Modal.vue";
import draggable from "vuedraggable";
import {
    emitter
} from "@/utils/eventBus";
import {
    updateTicketOrder
} from "@/components/atoms/updateTicketOrder";
import {
    deleteTicket
} from "@/components/atoms/deleteTicket";
import {
    addNewTicket
} from "@/components/atoms/AddNewTicket";
import {
    validateNameField
} from "@/components/atoms/ValidateModalInputFields";
import {
    setErrorMessage
} from "@/utils/errorHandler";
import {
    toggleTicketCompletion
} from "@/components/atoms/isCompletedCheckBox";
import
CopyTicketModal from "@/components/molecules/CopyTicketModal.vue";
import {
    connection,
    ensureConnected
} from "@/services/signalRService";

export default {
    components: {
        Modal,
        draggable,
        CopyTicketModal,
    },
    setup() {
        const calendarLists = ref([]);
        const loading = ref(true);
        const errorMessage = ref("");
        const showAddNewTicketModal = ref(false);
        const selectedListId = ref(null);
        const showAddNewCalendarListModal = ref(false);
        const showEditTicketModal = ref(false);
        const newTicketError = ref("");
        const newListError = ref("");
        const editListError = ref("");
        const editTicketError = ref("");
        const calendarId = ref(localStorage.getItem("calendarId"));
        const showCopyTicketModal = ref(false);
        const selectedTicketId = ref(null);
        const editedTicket = ref({
            id: null,
            name: "",
            description: "",
            priority: null
        });
        const newList = ref({
            name: "",
            color: "#CCCCCC",
        });
        const newTicket = ref({
            name: "",
            description: "",
            startTime: "",
            endTime: "",
            priority: null,
        });
        const showEditCalendarListModal = ref(false);
        const editedList = ref({
            id: null,
            name: "",
            color: "#CCCCCC"
        });

        const calendarListEvents = [
            "TicketCreatedInCalendarLists",
            "TicketScheduled",
            "TicketCopiedInCalendarLists",
            "TicketReorderedInCalendarLists",
            "TicketUpdatedInCalendarLists",
            "TicketCompletedUpdatedInCalendarLists",
            "TicketDeletedInCalendarLists",
            "TicketMovedBackToCalendar",
            "CalendarListCreated",
            "CalendarListUpdated",
            "CalendarListDeleted",
            "CalendarCopied"
        ];

        const openCopyTicketModal = (ticketId) => {
            selectedTicketId.value = ticketId;
            showCopyTicketModal.value = true;
        };

        const toggleCompletion = async (ticket) => {
            try {
                await toggleTicketCompletion(ticket.id, !ticket.isCompleted, errorMessage);
                ticket.isCompleted = !ticket.isCompleted;
            } catch (error) {
                console.error("Failed to update ticket status:", error);
            }
        };

        const openEditTicketModal = (ticket) => {
            editedTicket.value = {
                ...ticket
            };
            showEditTicketModal.value = true;
        };

        const handleUpdateTicket = async () => {
            editTicketError.value = "";

            const validationError = validateNameField(editedTicket.value.name);
            if (validationError) {
                setErrorMessage(editTicketError, validationError);
                return;
            }

            try {
                if (!editedTicket.value.id) {
                    console.error("Ticket ID is missing.");
                    return;
                }

                await api.put(`/api/Tickets/updateTicket`, {
                    id: editedTicket.value.id,
                    name: editedTicket.value.name,
                    description: editedTicket.value.description,
                    priority: editedTicket.value.priority || null,
                    startTime: null,
                    endTime: null,
                });

                await fetchCalendarLists();
                showEditTicketModal.value = false;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(editTicketError, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    console.error("Error updating ticket:", error);
                    setErrorMessage(editTicketError, "Failed to update ticket.");
                }
            }
        };

        const confirmDeleteCalendarList = () => {
            if (confirm("Are you sure you want to delete this list? All associated tickets will also be deleted.")) {
                deleteCalendarList();
            }
        };

        const openEditListModal = (list) => {
            editedList.value = {
                ...list
            };
            showEditCalendarListModal.value = true;
        };

        const handleUpdateCalendarList = async () => {
            editListError.value = "";

            const validationError = validateNameField(editedList.value.name);
            if (validationError) {
                setErrorMessage(editListError, validationError);
                return;
            }

            try {
                const existingList = calendarLists.value.find(l => l.id === editedList.value.id);
                const colorChanged = existingList && existingList.color !== editedList.value.color;

                await api.put(`/api/CalendarLists/${editedList.value.id}`, {
                    id: editedList.value.id,
                    name: editedList.value.name,
                    color: editedList.value.color,
                });

                await fetchCalendarLists();

                if (colorChanged) {
                    await emitter.emit("calendarUpdated");
                }

                showEditCalendarListModal.value = false;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(editListError, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    console.error("Error updating list:", error);
                    setErrorMessage(editListError, "Failed to update list.");
                }
            }
        };

        const deleteCalendarList = async () => {
            try {
                await api.delete(`/api/CalendarLists/${editedList.value.id}`);
                calendarLists.value = calendarLists.value.filter(l => l.id !== editedList.value.id);
                showEditCalendarListModal.value = false;
                await emitter.emit("calendarUpdated");
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(editListError, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    console.error("Error deleting list:", error);
                    setErrorMessage(editListError, "Failed to delete list.");
                }
            }
        };

        const fetchCalendarLists = async () => {
            try {
                const response = await api.get(`/api/CalendarLists/calendar/${calendarId.value}`);
                calendarLists.value = response.data.map(list => ({
                    ...list,
                    tickets: (list.tickets || []).sort(
                        (a, b) => Number(a.currentPosition) - Number(b.currentPosition)
                    ),
                }));
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(errorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(errorMessage, "Error loading calendar lists.");
                    console.error("Error loading calendar lists:", error);
                }
            } finally {
                loading.value = false;
            }
        };

        const openAddNewCalendarListModal = () => {
            newList.value = {
                name: "",
                color: "#CCCCCC"
            };
            showAddNewCalendarListModal.value = true;
        };

        const handleAddNewCalendarList = async () => {
            newListError.value = "";

            const validationError = validateNameField(newList.value.name);
            if (validationError) {
                setErrorMessage(newListError, validationError);
                return;
            }

            try {
                const response = await api.post("/api/CalendarLists", {
                    name: newList.value.name,
                    color: newList.value.color,
                    calendarId: calendarId.value,
                });

                const newListData = {
                    ...response.data,
                    tickets: []
                };
                calendarLists.value.push(newListData);
                showAddNewCalendarListModal.value = false;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(newListError, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    console.error("Error adding list:", error);
                    setErrorMessage(newListError, "Failed to add list.");
                }
            }
        };

        const openAddNewTicketModal = (listId) => {
            selectedListId.value = listId;
            newTicket.value = {
                name: "",
                description: "",
                startTime: "",
                endTime: "",
                priority: null,
            };
            showAddNewTicketModal.value = true;
        };

        const handleAddNewTicket = async () => {
            newTicketError.value = "";

            await addNewTicket({
                    name: newTicket.value.name,
                    description: newTicket.value.description || null,
                    startTime: null,
                    endTime: null,
                    priority: newTicket.value.priority || null,
                    calendarListId: selectedListId.value,
                    currentParentType: "CalendarList",
                    parentId: selectedListId.value,
                },
                selectedListId,
                calendarLists,
                showAddNewTicketModal,
                newTicketError
            );
        };

        const onTicketDragStart = (ticket) => {
            localStorage.setItem("draggedTicket", JSON.stringify(ticket));
        };

        const onTicketDragEnd = async (list) => {
            await updateTicketOrder(list, errorMessage);
        };

        const handleDelete = async (ticketId, list) => {
            await deleteTicket(ticketId, list, errorMessage);
        }

        const formatDate = (dateString) => {
            if (!dateString) return "";
            return new Date(dateString).toLocaleDateString();
        };

        onMounted(async () => {
            fetchCalendarLists();

            emitter.on("ticketScheduled", async (payload) => {
                for (const list of calendarLists.value) {
                    const originalLength = list.tickets.length;
                    list.tickets = list.tickets.filter(ticket => ticket.id !== payload.ticketId);
                    if (list.tickets.length < originalLength && list.tickets.length > 0) {
                        await updateTicketOrder(list, errorMessage);
                    }
                }
            });

            await ensureConnected();
            await connection.invoke("JoinGroup", calendarId.value);
            calendarListEvents.forEach(event => {
                connection.on(event, async () => {
                    fetchCalendarLists();
                });
            });
        });

        onBeforeUnmount(() => {
            if (calendarId.value) {
                connection.invoke("LeaveGroup", calendarId.value);
            }
        });

        watch(showAddNewCalendarListModal, (newValue) => {
            if (!newValue) newListError.value = "";
        });

        watch(showAddNewTicketModal, (newValue) => {
            if (!newValue) newTicketError.value = "";
        });

        watch(showEditCalendarListModal, (newValue) => {
            if (!newValue) editListError.value = "";
        });

        watch(showEditTicketModal, (newValue) => {
            if (!newValue) editTicketError.value = "";
        });

        return {
            calendarLists,
            loading,
            errorMessage,
            showAddNewCalendarListModal,
            newList,
            showAddNewTicketModal,
            newTicket,
            openAddNewCalendarListModal,
            handleAddNewCalendarList,
            openAddNewTicketModal,
            handleAddNewTicket,
            handleDelete,
            formatDate,
            onTicketDragEnd,
            onTicketDragStart,
            showEditCalendarListModal,
            editedList,
            openEditListModal,
            handleUpdateCalendarList,
            confirmDeleteCalendarList,
            deleteCalendarList,
            showEditTicketModal,
            editedTicket,
            openEditTicketModal,
            handleUpdateTicket,
            newTicketError,
            newListError,
            editListError,
            editTicketError,
            toggleCompletion,
            openCopyTicketModal,
            showCopyTicketModal,
            selectedTicketId,
        };
    },
};
</script>

<style scoped>
.copy-btn {
    background: #ffc107;
    color: white;
    border: none;
    padding: 5px;
    margin-top: 5px;
    border-radius: 5px;
    cursor: pointer;
}

.ticket {
    cursor: pointer;
}

.lists-container {
    width: 100%;
    height: 100%;
    background: #c8e6c9;
    padding: 15px;
    border-radius: 0px 10px 10px 0px;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    min-width: 300px;
}

.header {
    display: flex;
    justify-content: space-between;
    align-items: center;
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

.ticket-checkbox {
    position: absolute;
    top: 5px;
    left: 5px;
    width: 18px;
    height: 18px;
}

.lists-content {
    display: flex;
    flex-direction: row;
    gap: 10px;
    overflow-x: auto;
    white-space: nowrap;
    padding-bottom: 10px;
}

.list-item {
    padding: 10px;
    border-radius: 5px;
    box-shadow: 2px 2px 5px rgba(0, 0, 0, 0.1);
    min-width: 200px;
    flex-shrink: 0;
    text-align: center;
}

.ticket-list {
    margin-top: 10px;
}

.ticket-item {
    position: relative;
    padding: 5px;
    border: 2px solid black;
    margin-bottom: 5px;
}

.ticket-item:last-child {
    margin-bottom: 0;
}

.add-ticket-button {
    background: #2196f3;
    color: white;
    border: none;
    padding: 5px;
    margin-top: 10px;
    border-radius: 5px;
    cursor: pointer;
}

.modal-content {
    display: flex;
    flex-direction: column;
    gap: 10px;
    align-items: center;
}

.color-picker {
    display: flex;
    align-items: center;
    gap: 10px;
}

.color-preview {
    width: 30px;
    height: 30px;
    border-radius: 5px;
    border: 1px solid #ccc;
}

label {
    font-weight: bold;
}

.error {
    color: red;
}

.edit-list-button {
    background: #ff9800;
    color: white;
    border: none;
    padding: 5px;
    margin-top: 5px;
    border-radius: 5px;
    cursor: pointer;
}

.delete-list-button {
    background: #f44336;
    color: white;
    border: none;
    padding: 5px;
    margin-top: 10px;
    border-radius: 5px;
    cursor: pointer;
}
</style>
