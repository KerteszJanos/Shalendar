<template>
<div>
    <h2>Your Calendars</h2>

    <p v-if="defaultCalendar" class="default-calendar">Default Calendar: {{ defaultCalendar.name }}</p>

    <!-- Új naptár létrehozása gomb -->
    <button @click="openModal" class="add-calendar-button">+ New Calendar</button>

    <div v-if="calendars.length > 0" class="calendar-container">
        <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
        <p v-if="successMessage" class="success-message">{{ successMessage }}</p>

        <div v-for="calendar in calendars" :key="calendar.id" class="calendar-box" @click="navigateToCalendar(calendar.id)">
            <svg class="calendar-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                <path d="M19 3h-1V1h-2v2H8V1H6v2H5c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 18H5V8h14v13zM7 10h5v5H7z" />
            </svg>
            <p class="calendar-name">{{ calendar.name }}</p>
            <p class="calendar-permission">{{ getPermission(calendar.id) }}</p>

            <button @click.stop="openPermissionsModal(calendar.id)" class="show-permissions-button">Calendar permissions</button>
            <button @click.stop="setDefaultCalendar(calendar.id)" class="set-default-button">Set as Default</button>
            <button @click.stop="confirmDeleteCalendar(calendar.id)" class="delete-calendar-button">Delete</button>
        </div>
    </div>
    <p v-else>No calendars available</p>

    <Modal :show="showNewCalendarModal" title="Create New Calendar" confirmText="Create" @close="closeNewCalendarModal" @confirm="createCalendar">
        <input type="text" v-model="newCalendarName" placeholder="Enter calendar name" class="modal-input" />
        <p v-if="NewCalendarErrorMessage" class="error-message">{{ NewCalendarErrorMessage }}</p>
    </Modal>

    <Modal :show="showPermissionsModal" title="Calendar permissions" confirmText="Add" @confirm="addPermission" @close="closePermissionsModal">
        <p v-if="PermissionsErrorMessage" class="error-message">{{ PermissionsErrorMessage }}</p>
        <div v-if="sharedPermissions.length > 0">
            <ul>
                <li v-for="permission in sharedPermissions" :key="permission.email">
                    {{ permission.email }} - {{ permission.permissionType }}
                    <button v-if="permission.email !== currentUserEmail" @click="deletePermission(permission.email)" class="delete-permission-button">Delete</button>
                </li>
            </ul>
            <div class="permission-input">
                <input type="email" v-model="newPermissionEmail" placeholder="Enter email" class="modal-input" />
                <select v-model="newPermissionType" class="modal-select">
                    <option value="read">Read</option>
                    <option value="write">Write</option>
                    <option value="owner">Owner</option>
                </select>
            </div>
        </div>
        <p v-else-if="!PermissionsErrorMessage">No permissions set for this calendar.</p>
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
        const successMessage = ref("");
        const NewCalendarErrorMessage = ref(null);
        const PermissionsErrorMessage = ref(null);
        const defaultCalendar = ref(null);
        const defaultCalendarId = JSON.parse(localStorage.getItem("user"))?.defaultCalendarId || null;
        const showPermissionsModal = ref(false);
        const sharedPermissions = ref([]);
        const newPermissionEmail = ref("");
        const newPermissionType = ref("read");
        const selectedCalendarId = ref(null);
        const currentUserEmail = JSON.parse(localStorage.getItem("user"))?.email || "";
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

        const confirmDeleteCalendar = async (calendarId) => { // gpt generated
            if (!confirm("Warning: Deleting this calendar will remove your access. If you are the last owner, all associated content (e.g., tickets) will also be deleted. Do you want to proceed?")) {
                return;
            }
            await deleteCalendar(calendarId);
        };

        const deleteCalendar = async (calendarId) => { // gpt generated
            try {
                const response = await api.delete(`/api/Calendars/${calendarId}`);
                setErrorMessage(successMessage, response.data?.message);
                fetchCalendars();
            } catch (error) {
                setErrorMessage(errorMessage, "Error deleting calendar.");
                console.error("Error deleting calendar:", error);
            }
        };

        const deletePermission = async (email) => {
            try {
                await api.delete(`/api/Calendars/${selectedCalendarId.value}/permissions/${email}`);
                await fetchPermissions(selectedCalendarId.value);
            } catch (error) {
                if (error.response) {
                    if (error.response.status === 403) {
                        setErrorMessage(PermissionsErrorMessage, "Access denied: You do not have permission to delete this.");
                    } else if (error.response.status === 404) {
                        setErrorMessage(PermissionsErrorMessage, "User or permission not found.");
                    } else {
                        setErrorMessage(PermissionsErrorMessage, "Error deleting permission: " + (error.response.data?.message || "Unknown error."));
                    }
                } else {
                    setErrorMessage(PermissionsErrorMessage, "Network error or server is unreachable.");
                }
                console.error("Error deleting permission:", error);
            }
        };

        const setDefaultCalendar = async (calendarId) => {
            try {
                await api.put(`/api/Users/set-default-calendar/${calendarId}`);
                const userData = JSON.parse(localStorage.getItem("user")) || {};
                userData.defaultCalendarId = calendarId;
                localStorage.setItem("user", JSON.stringify(userData));
                setErrorMessage(successMessage, "Default calendar updated successfully.");

                defaultCalendar.value = calendars.value.find(cal => cal.id === calendarId) || null;
            } catch (error) {
                setErrorMessage(PermissionsErrorMessage, "Error setting default calendar.");
                console.error("Error setting default calendar:", error);
            }
        };

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
                    api.get(`/api/Calendars/noPermissionNeeded/${permission.calendarId}`)
                );

                const calendarResponses = await Promise.all(calendarRequests);
                calendars.value = calendarResponses.map(response => response.data);

                defaultCalendar.value = calendars.value.find(cal => cal.id === defaultCalendarId) || null;
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
            NewCalendarErrorMessage.value = "";
        };

        const closeNewCalendarModal = () => {
            showNewCalendarModal.value = false;
        };

        const createCalendar = async () => {
            setErrorMessage(NewCalendarErrorMessage, validateNameField(newCalendarName.value));
            if (NewCalendarErrorMessage.value) {
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
                setErrorMessage(NewCalendarErrorMessage, "Failed to create calendar.");
                console.error("Error creating calendar:", error);
            }
        };

        const openPermissionsModal = async (calendarId) => {
            PermissionsErrorMessage.value = "";
            selectedCalendarId.value = calendarId;
            showPermissionsModal.value = true;
            await fetchPermissions(calendarId);
        };

        const closePermissionsModal = () => {
            showPermissionsModal.value = false;
            sharedPermissions.value = [];
        };

        const fetchPermissions = async (calendarId) => {
            try {
                const response = await api.get(`/api/Calendars/${calendarId}/permissions`);
                sharedPermissions.value = response.data;
            } catch (error) {
                if (error.response && error.response.status === 403) {
                    setErrorMessage(PermissionsErrorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                } else {
                    setErrorMessage(PermissionsErrorMessage, "Error fetching calendar permissions.");
                    console.error("Error fetching calendar permissions:", error);
                }
            }
        };

        const addPermission = async () => {
            if (sharedPermissions?.value?.length === 0) {
                return;
            }

            const userEmail = JSON.parse(localStorage.getItem("user"))?.email || "";
            if (newPermissionEmail.value === userEmail) {
                setErrorMessage(PermissionsErrorMessage, "You cannot grant permissions to yourself.");
                return;
            }

            try {
                await api.post(`/api/Calendars/${selectedCalendarId.value}/permissions/${newPermissionEmail.value}/${newPermissionType.value}`);
                await fetchPermissions(selectedCalendarId.value);
                newPermissionEmail.value = "";
                newPermissionType.value = "read";
            } catch (error) {
                if (error.response) {
                    if (error.response.status === 403) {
                        setErrorMessage(PermissionsErrorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                        console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    } else if (error.response.status === 404) {
                        setErrorMessage(PermissionsErrorMessage, "User not found. Please check the email address.");
                        console.error("User not found. Please check the email address.", error);
                    } else {
                        setErrorMessage(PermissionsErrorMessage, "Error adding permission: " + (error.response.data?.message || "Unknown error."));
                        console.error("Error adding permission.", error);
                    }
                } else {
                    setErrorMessage(PermissionsErrorMessage, "Network error or server is unreachable.");
                    console.error("Network error or server is unreachable.", error);
                }
            }
        };

        onMounted(fetchCalendars);

        return {
            permissions,
            calendars,
            showNewCalendarModal,
            newCalendarName,
            errorMessage,
            NewCalendarErrorMessage,
            PermissionsErrorMessage,
            getPermission,
            openModal,
            closeNewCalendarModal,
            createCalendar,
            navigateToCalendar,
            showPermissionsModal,
            openPermissionsModal,
            closePermissionsModal,
            sharedPermissions,
            addPermission,
            newPermissionEmail,
            newPermissionType,
            deletePermission,
            defaultCalendar,
            currentUserEmail,
            setDefaultCalendar,
            successMessage,
            confirmDeleteCalendar,
        };
    }
};
</script>

<style scoped>
.delete-calendar-button {
    /* gpt generated */
    margin-left: 10px;
    padding: 5px 10px;
    background: red;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
    font-size: 14px;
}

.delete-calendar-button:hover {
    /* gpt generated */
    background: darkred;
}

.permission-input {
    display: flex;
    gap: 10px;
    margin-top: 10px;
}

.modal-select {
    width: 35%;
    padding: 10px;
    border: 1px solid #ccc;
    border-radius: 5px;
}

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

.success-message {
    color: green;
    margin-top: 5px;
    font-size: 0.9em;
}

.delete-permission-button {
    margin-left: 10px;
    padding: 5px 10px;
    background: red;
    color: white;
    border: none;
    border-radius: 5px;
    cursor: pointer;
    font-size: 14px;
}

.delete-permission-button:hover {
    background: darkred;
}
</style>
