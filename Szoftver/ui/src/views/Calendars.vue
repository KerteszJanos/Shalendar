<template>
<div>
    <h2>Your Calendars</h2>

    <!-- Új naptár létrehozása gomb -->
    <button @click="openModal" class="add-calendar-button">+ New Calendar</button>

    <div v-if="calendars.length > 0" class="calendar-container">
        <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>

        <div v-for="calendar in calendars" :key="calendar.id" class="calendar-box" @click="navigateToCalendar(calendar.id)">
            <svg class="calendar-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M19 3h-1V1h-2v2H8V1H6v2H5c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 18H5V8h14v13zM7 10h5v5H7z" />
            </svg>
            <p class="calendar-name">{{ calendar.name }}</p>
            <p class="calendar-permission">{{ getPermission(calendar.id) }}</p>
        </div>
    </div>
    <p v-else>No calendars available</p>

    <Modal :show="showNewCalendarModal" title="Create New Calendar" confirmText="Create" @close="closeNewCalendarModal" @confirm="createCalendar">
        <input type="text" v-model="newCalendarName" placeholder="Enter calendar name" class="modal-input" />
        <p v-if="NewCalendarerrorMessage" class="error-message">{{ NewCalendarerrorMessage }}</p>
    </Modal>
</div>
</template>

<script>
import {
    ref,
    onMounted
} from "vue";
import api from "@/utils/config/axios-config";
import Modal from "@/components/molecules/Modal.vue";
import {
    validateNameField
} from "@/components/atoms/ValidateModalInputFields.js";
import {
    useRouter
} from 'vue-router';
import {
    setErrorMessage
} from "@/utils/errorHandler";

export default {
    components: {
        Modal
    },
    setup() {
        const permissions = ref([]);
        const calendars = ref([]);
        const showNewCalendarModal = ref(false);
        const newCalendarName = ref("");
        const errorMessage = ref(null);
        const NewCalendarerrorMessage = ref(null);
        const router = useRouter();
        const userId = (() => {
            const userData = localStorage.getItem("user");
            if (!userData) {
                setErrorMessage(errorMessage, "User data not found in localStorage.");
                console.error("User data not found in localStorage.");
                return null;
            }

            const user = JSON.parse(userData);
            return user.userId || null;
        })();

        const navigateToCalendar = (calendarId) => {
            const permission = getPermission(calendarId);
            if (!permission) {
                setErrorMessage(errorMessage, "Permission not found for calendar");
                console.error("Permission not found for calendar", calendarId);
                return;
            }

            localStorage.setItem("calendarId", calendarId);

            router.push("/Dashboard");
        };

        const fetchCalendars = async () => {
            if (!userId) {
                errorMessage(errorMessage);
                console.error("User ID is missing.");
                return;
            }

            try {
                const permissionsResponse = await api.get(`/api/Calendars/user/${userId}`);
                permissions.value = permissionsResponse.data;

                const calendarRequests = permissions.value.map(permission =>
                    api.get(`/api/Calendars/${permission.calendarId}`)
                );

                const calendarResponses = await Promise.all(calendarRequests);
                calendars.value = calendarResponses.map(response => response.data);
            } catch (error) {
                setErrorMessage(errorMessage, "Error fetching calendar data.");
                console.error("Error fetching calendar data:", error);
            }
        };

        const getPermission = (calendarId) => {
            const permission = permissions.value.find(p => p.calendarId === calendarId);
            return permission ? permission.permissionType : "Unknown";
        };

        const openModal = () => {
            showNewCalendarModal.value = true;
            newCalendarName.value = "";
            NewCalendarerrorMessage.value = "";
        };

        const closeNewCalendarModal = () => {
            showNewCalendarModal.value = false;
        };

        const createCalendar = async () => {
            setErrorMessage(NewCalendarerrorMessage, validateNameField(newCalendarName.value));
            if (NewCalendarerrorMessage.value) {
                return;
            }

            if (!userId) {
                setErrorMessage(errorMessage, "User ID is missing.");
                console.error("User ID is missing.");
                return;
            }

            try {
                const response = await api.post(`/api/Calendars?userId=${userId}`, {
                    name: newCalendarName.value
                });

                await fetchCalendars();

                closeNewCalendarModal();
            } catch (error) {
                setErrorMessage(NewCalendarerrorMessage, "Failed to create calendar.");
                console.error("Error creating calendar:", error);
            }
        };

        onMounted(fetchCalendars);

        return {
            permissions,
            calendars,
            showNewCalendarModal,
            newCalendarName,
            errorMessage,
            NewCalendarerrorMessage,
            getPermission,
            openModal,
            closeNewCalendarModal,
            createCalendar,
            navigateToCalendar
        };
    }
};
</script>

<style scoped>
.calendar-container {
    display: flex;
    flex-wrap: wrap;
    gap: 20px;
    justify-content: center;
}

.calendar-box {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    width: 120px;
    height: 140px;
    background: #f3f3f3;
    border-radius: 8px;
    padding: 10px;
    box-shadow: 2px 2px 10px rgba(0, 0, 0, 0.1);
    text-align: center;
    cursor: pointer;
}

.calendar-icon {
    width: 50px;
    height: 50px;
    fill: #4A90E2;
}

.calendar-name {
    margin-top: 5px;
    font-size: 1em;
    font-weight: bold;
    color: #333;
}

.calendar-permission {
    margin-top: 3px;
    font-size: 0.9em;
    color: #666;
    font-style: italic;
}

.add-calendar-button {
    margin-bottom: 10px;
    padding: 10px 15px;
    background: #4A90E2;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
    font-size: 16px;
}

.add-calendar-button:hover {
    background: #357ABD;
}

.modal-input {
    width: 100%;
    padding: 10px;
    margin-top: 10px;
    border: 1px solid #ccc;
    border-radius: 5px;
}

.error-message {
    color: red;
    margin-top: 5px;
    font-size: 0.9em;
}
</style>
