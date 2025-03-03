<template>
<div>
    <h2>Your Calendars</h2>

    <!-- Új naptár létrehozása gomb -->
    <button @click="openModal" class="add-calendar-button">+ New Calendar</button>

    <div v-if="calendars.length > 0" class="calendar-container">
        <div v-for="calendar in calendars" :key="calendar.id" class="calendar-box" @click="navigateToCalendar(calendar.id)">
            <svg class="calendar-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M19 3h-1V1h-2v2H8V1H6v2H5c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 18H5V8h14v13zM7 10h5v5H7z" />
            </svg>
            <p class="calendar-name">{{ calendar.name }}</p>
            <p class="calendar-permission">{{ getPermission(calendar.id) }}</p>
        </div>
    </div>
    <p v-else>No calendars available</p>

    <!-- Modal a naptár létrehozásához -->
    <Modal :show="showModal" title="Create New Calendar" confirmText="Create" @close="closeModal" @confirm="createCalendar">
        <input type="text" v-model="newCalendarName" placeholder="Enter calendar name" class="modal-input" />
        <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
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

export default {
    components: {
        Modal
    },
    setup() {
        // Reaktív változók
        const permissions = ref([]);
        const calendars = ref([]);
        const showModal = ref(false);
        const newCalendarName = ref("");
        const errorMessage = ref(null);
        const router = useRouter();
        const userId = (() => {
            const userData = localStorage.getItem("user");
            if (!userData) {
                console.error("User data not found in localStorage.");
                return null;
            }

            const user = JSON.parse(userData);
            return user.userId || null;
        })();

        const navigateToCalendar = (calendarId) => {
            const permission = getPermission(calendarId);
            if (!permission) {
                console.error("Permission not found for calendar", calendarId);
                return;
            }

            localStorage.setItem("calendarId", calendarId);
            localStorage.setItem("calendarPermission", permission);
            
            router.push("/Dashboard");
        };

        // API-hívás a felhasználó naptár engedélyeinek lekérdezésére
        const fetchCalendars = async () => {
            if (!userId) {
                console.error("User ID is missing.");
                return;
            }

            try {
                // Lekérjük a felhasználó naptár engedélyeit
                const permissionsResponse = await api.get(`/api/Calendars/user/${userId}`);
                permissions.value = permissionsResponse.data;

                // Lekérjük az egyes naptárakat a permission lista alapján
                const calendarRequests = permissions.value.map(permission =>
                    api.get(`/api/Calendars/${permission.calendarId}`)
                );

                const calendarResponses = await Promise.all(calendarRequests);
                calendars.value = calendarResponses.map(response => response.data);
            } catch (error) {
                console.error("Error fetching calendar data:", error);
            }
        };

        // Engedélyek kinyerése az adott naptárhoz
        const getPermission = (calendarId) => {
            const permission = permissions.value.find(p => p.calendarId === calendarId);
            return permission ? permission.permissionType : "Unknown";
        };

        // Modal megnyitása
        const openModal = () => {
            showModal.value = true;
            newCalendarName.value = "";
            errorMessage.value = null;
        };

        // Modal bezárása
        const closeModal = () => {
            showModal.value = false;
        };

        // Új naptár létrehozása
        const createCalendar = async () => {
            // Validáció
            errorMessage.value = validateNameField(newCalendarName.value);
            if (errorMessage.value) {
                return;
            }

            if (!userId) {
                console.error("User ID is missing.");
                return;
            }

            try {
                const response = await api.post(`/api/Calendars?userId=${userId}`, {
                    name: newCalendarName.value
                });

                // Ha sikeres, újra lekérjük a teljes naptárlistát, hogy frissüljenek az engedélyek is
                await fetchCalendars();

                // Modal bezárása
                closeModal();
            } catch (error) {
                console.error("Error creating calendar:", error);
                errorMessage.value = "Failed to create calendar.";
            }
        };

        // API-hívás az oldal betöltésekor
        onMounted(fetchCalendars);

        // Visszaadjuk a változókat és függvényeket
        return {
            permissions,
            calendars,
            showModal,
            newCalendarName,
            errorMessage,
            getPermission,
            openModal,
            closeModal,
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
