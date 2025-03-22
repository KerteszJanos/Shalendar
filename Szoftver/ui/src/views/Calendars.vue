<!--
  This component displays the user's available calendars.

  Features:
  - Shows a list of all calendars the user has access to.
  - Allows calendar creation, deletion, and setting a default calendar.
  - Displays permissions and lets owners manage them (add, update, delete).
  - Navigates to the selected calendar’s dashboard.
  - Supports feedback messages (success and error handling).
-->

<template>
<div class="calendar-page">
    <h2>Your Calendars</h2>

    <p v-if="defaultCalendar" class="default-calendar" :title="defaultCalendar.name">
        Default Calendar: {{ defaultCalendar.name }}
    </p>

    <button @click="openModal" class="add-calendar-button">+ New Calendar</button>
    <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
    <p v-if="successMessage" class="success-message">{{ successMessage }}</p>
    <div v-if="calendars.length > 0" class="calendar-container">

        <div v-for="calendar in calendars" :key="calendar.id" class="calendar-box" @click="navigateToCalendar(calendar.id)">

            <div class="calendar-header">
                <svg class="calendar-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="currentColor">
                    <path d="M19 3h-1V1h-2v2H8V1H6v2H5c-1.1 0-2 .9-2 2v16c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 18H5V8h14v13zM7 10h5v5H7z" />
                </svg>
                <p class="calendar-name" :title="calendar.name">{{ calendar.name }}</p>
            </div>

            <p class="calendar-permission">{{ getPermission(calendar.id) }}</p>

            <div class="calendar-actions">
                <button @click.stop="openPermissionsModal(calendar.id)" class="btn permissions">
                    Manage
                </button>
                <button @click.stop="setDefaultCalendar(calendar.id)" class="btn default">
                    Set Default
                </button>
                <button @click.stop="confirmDeleteCalendar(calendar.id)" class="btn delete">
                    Delete
                </button>
            </div>
        </div>
    </div>

    <p v-else class="no-calendars">No calendars available</p>

    <Modal :show="showNewCalendarModal" title="Create New Calendar" confirmText="Create" @close="closeNewCalendarModal" @confirm="createCalendar">
        <input type="text" v-model="newCalendarName" placeholder="Enter calendar name" class="modal-input" />
        <p v-if="NewCalendarErrorMessage" class="error-message">{{ NewCalendarErrorMessage }}</p>
    </Modal>

    <Modal :show="showPermissionsModal" title="Calendar permissions" confirmText="Add" @confirm="addPermission" @close="closePermissionsModal">
        <p v-if="PermissionsErrorMessage" class="error-message">{{ PermissionsErrorMessage }}</p>
        <div v-if="sharedPermissions.length > 0">
            <ul class="permissions-list">
                <li v-for="permission in sharedPermissions" :key="permission.email" :title="permission.email" class="permission-item">
                    <span class="permission-email">{{ permission.email }}</span>
                    <div class="permission-actions">
                        <select v-if="permission.email !== currentUserEmail" v-model="permission.permissionType" @change="updatePermission(permission.email, permission.permissionType)" class="permission-dropdown">
                            <option value="read">Read</option>
                            <option value="write">Write</option>
                            <option value="owner">Owner</option>
                        </select>
                        <span v-else>{{ permission.permissionType }}</span>
                        <button v-if="permission.email !== currentUserEmail" @click="deletePermission(permission.email)" class="delete-permission-button">
                            Remove
                        </button>
                    </div>
                </li>

            </ul>
            <div class="permission-input modal-content">
                <input type="email" v-model="newPermissionEmail" placeholder="Enter email" class="modal-input" />
                <select v-model="newPermissionType" class="modal-select">
                    <option value="read">Read</option>
                    <option value="write">Write</option>
                    <option value="owner">Owner</option>
                </select>
            </div>
        </div>
        <p v-else>No permissions set for this calendar.</p>
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
        // ---------------------------------
        // Constants	  		           |
        // --------------------------------- 
        const defaultCalendarId = JSON.parse(localStorage.getItem("user"))?.defaultCalendarId || null;
        const currentUserEmail = JSON.parse(localStorage.getItem("user"))?.email || "";
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
        const router = useRouter();

        // ---------------------------------
        // Reactive state	        	   |
        // ---------------------------------
        const permissions = ref([]);
        const calendars = ref([]);
        const showNewCalendarModal = ref(false);
        const newCalendarName = ref("");
        const errorMessage = ref(null);
        const successMessage = ref("");
        const NewCalendarErrorMessage = ref(null);
        const PermissionsErrorMessage = ref(null);
        const defaultCalendar = ref(null);
        const showPermissionsModal = ref(false);
        const sharedPermissions = ref([]);
        const newPermissionEmail = ref("");
        const newPermissionType = ref("read");
        const selectedCalendarId = ref(null);

        // ---------------------------------
        // Methods			               |
        // ---------------------------------
        // --------------
        // Modals   	|
        // --------------
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
        
        const openModal = () => {
            showNewCalendarModal.value = true;
            newCalendarName.value = "";
            NewCalendarErrorMessage.value = "";
        };

        const closeNewCalendarModal = () => {
            showNewCalendarModal.value = false;
        };

        // --------------
        // Core actions	|
        // --------------
        const confirmDeleteCalendar = async (calendarId) => {
            if (!confirm("Warning: Deleting this calendar will remove your access. If you are the last owner, all associated content (e.g., tickets) will also be deleted. Do you want to proceed?")) {
                return;
            }
            await deleteCalendar(calendarId);
        };

        const deleteCalendar = async (calendarId) => {
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

        const updatePermission = async (email, newPermission) => {
            if (!selectedCalendarId.value || !email || !newPermission) {
                setErrorMessage(PermissionsErrorMessage, "Invalid input: Calendar ID, email, or permission is missing.");
                console.error("Invalid input: Calendar ID, email, or permission is missing.");
                return;
            }

            try {
                await api.post(`/api/Calendars/${selectedCalendarId.value}/permissions/${email}/${newPermission}`);
            } catch (error) {
                if (error.response) {
                    if (error.response.status === 403) {
                        setErrorMessage(PermissionsErrorMessage, `Access denied: ${error.response.data?.message || "You do not have permission."}`);
                        console.error(`Access denied: ${error.response.data?.message || "You do not have permission."}`);
                    } else {
                        setErrorMessage(PermissionsErrorMessage, "Error updating permission: " + (error.response.data?.message || "Unknown error."));
                        console.error("Error updating permission.", error);
                    }
                } else {
                    setErrorMessage(PermissionsErrorMessage, "Network error or server is unreachable.");
                    console.error("Network error or server is unreachable.", error);
                }
            }
        };
        
        // ---------------------------------
        // Lifecycle hooks		           |
        // ---------------------------------
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
            updatePermission
        };
    }
};
</script>

<style scoped>
.permission-item {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 0;
    border-bottom: 1px solid #ddd;
}

.permission-email {
    flex-grow: 1;
    text-align: left;
    font-weight: 500;
    color: #333;
}

.permission-actions {
    display: flex;
    align-items: center;
    gap: 10px;
}

.permission-email {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    text-align: left;
}

.permission-dropdown {
    margin-left: 10px;
    padding: 5px;
    border-radius: 5px;
    border: 1px solid #ccc;
    cursor: pointer;
}

.calendar-page {
    max-width: 1200px;
    margin: auto;
    text-align: center;
    margin-top: 0px;
}

.default-calendar {
    font-size: 1.1em;
    font-weight: bold;
    margin-bottom: 10px;
    color: #555;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.add-calendar-button {
    margin-bottom: 15px;
    padding: 12px 18px;
    background: #213A57;
    color: white;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    font-size: 16px;
    transition: all 0.3s ease-in-out;
}

.add-calendar-button:hover {
    background: #39608b;
    transform: scale(1.1);
}

.calendar-container {
    display: grid;
    grid-template-columns: repeat(4, minmax(250px, 1fr));
    gap: 20px;
    justify-content: center;
    padding: 10px;
}

.calendar-box {
    background: #f8f8f8;
    border-radius: 12px;
    padding: 20px;
    box-shadow: 4px 4px 14px rgba(0, 0, 0, 0.1);
    text-align: center;
    transition: transform 0.2s, box-shadow 0.2s;
    width: 100%;
    min-height: 220px;
    display: flex;
    flex-direction: column;
    justify-content: space-between;
}

.permission-input {
    display: flex;
    flex-direction: column;
    align-items: end;
}

.calendar-box:hover {
    transform: scale(1.01);
    cursor: pointer;
    box-shadow: 4px 4px 16px rgba(0, 0, 0, 0.15);
}

.calendar-header {
    display: flex;
    flex-direction: column;
    align-items: center;
}

.calendar-icon {
    width: 70px;
    height: 70px;
    fill: #0AD1C8;
}

.calendar-name {
    font-size: 16px;
    font-weight: bold;
    color: #333;
    max-width: 100%;
    margin: 0;
    cursor: pointer;
    flex-grow: 1;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    text-align: left;
}

.calendar-name:hover::after {
    content: attr(data-fulltext);
    position: absolute;
    top: 100%;
    left: 0;
    color: white;
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 12px;
    z-index: 100;
    white-space: normal;
    width: max-content;
    max-width: 250px;
}

.calendar-permission {
    font-size: 1em;
    color: #555;
    margin-top: 5px;
    font-style: italic;
}

.calendar-actions {
    margin-top: auto;
    display: flex;
    flex-direction: row;
    justify-content: space-between;
    gap: 8px;
    padding-top: 12px;
}

.btn {
    padding: 8px 12px;
    border: none;
    border-radius: 6px;
    cursor: pointer;
    font-size: 13px;
    flex: 1;
    transition: all 0.3s ease-in-out;
}

.permissions {
    background: #0B6477;
    color: white;
}

.permissions:hover {
    background: #10849e;
}

.default {
    background: #14919B;
    color: white;
}

.default:hover {
    background: #19b0bb;
}

.delete {
    background: red;
    color: white;
}

.delete:hover {
    background: darkred;
}

.modal-input {
    width: 100%;
    padding: 10px;
    border-radius: 5px;
    margin-top: 10px;
}

.modal-select {
    width: 25%;
    padding: 10px;
    border-radius: 5px;
    margin-top: 5px;
}

.permissions-list {
    list-style-type: none;
    padding: 0;
}

.permissions-list li {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 0;
    border-bottom: 1px solid #ddd;
}

.delete-permission-button {
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

.no-calendars {
    color: #777;
    font-style: italic;
    margin-top: 15px;
}

.error-message {
    color: red;
}

.success-message {
    color: green;
}

@media (max-width: 1024px) {
    .calendar-container {
        grid-template-columns: repeat(2, minmax(250px, 1fr));
    }
}

@media (max-width: 600px) {
    .calendar-container {
        grid-template-columns: repeat(1, minmax(250px, 1fr));
    }
}
</style>
