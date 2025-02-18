<template>
<div>
    <h1>Dashboard</h1>
    <p v-if="loading">Betöltés...</p>
    <p v-if="errorMessage" class="error">{{ errorMessage }}</p>

    <div v-if="calendar">
        <h2>Naptár neve: {{ calendar.name }}</h2>
        <p>Naptár ID: {{ calendar.id }}</p>
    </div>
</div>
</template>

<script>
import {
    ref,
    onMounted
} from "vue";
import api from "@/utils/config/axios-config";

export default {
    setup() {
        const calendar = ref(null);
        const loading = ref(true);
        const errorMessage = ref("");

        const fetchCalendar = async () => {
            try {
                const user = JSON.parse(localStorage.getItem("user"));
                if (!user || !user.defaultCalendarId) {
                    throw new Error("No default calendar found.");
                }

                const calendarId = user.defaultCalendarId;

                const response = await api.get(`/api/Calendars/${calendarId}`);
                calendar.value = response.data;
            } catch (error) {
                console.error("An error occurred while loading the calendar:", error);
                errorMessage.value = "Failed to load calendar data.";
            } finally {
                loading.value = false;
            }
        };

        onMounted(fetchCalendar);

        return {
            calendar,
            loading,
            errorMessage
        };
    }
};
</script>

<style scoped>
.error {
    color: red;
}
</style>
