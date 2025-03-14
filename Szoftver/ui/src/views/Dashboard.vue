<template>
<div class="dashboard" v-if="isMounted">
    <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
    <div class="calendar-container">
        <CalendarView :style="{ flex: calendarViewSize }" />
        <div class="resizer" @mousedown="startResizing"></div>
        <CalendarLists :style="{ flex: calendarListsSize }" />
    </div>
</div>
</template>

<script>
import {
    ref,
    onMounted
} from "vue";
import CalendarView from "@/components/organisms/CalendarView.vue";
import CalendarLists from "@/components/organisms/CalendarLists.vue";
import {
    setErrorMessage
} from "@/utils/errorHandler";

export default {
    components: {
        CalendarView,
        CalendarLists,
    },
    setup() {
        const errorMessage = ref("");
        const isMounted = ref(false);
        const calendarViewSize = ref(1.3);
        const calendarListsSize = ref(0.7);
        let isResizing = false;

        const startResizing = (event) => {
            document.body.style.userSelect = "none";
            isResizing = true;
            const startX = event.clientX;
            const startViewSize = calendarViewSize.value;
            const startListSize = calendarListsSize.value;

            const onMouseMove = (moveEvent) => {
                if (!isResizing) return;
                const delta = (moveEvent.clientX - startX) / window.innerWidth * 2;
                calendarViewSize.value = Math.max(0.2, startViewSize + delta);
                calendarListsSize.value = Math.max(0.2, startListSize - delta);
            };

            const onMouseUp = () => {
                document.body.style.userSelect = "";
                isResizing = false;
                window.removeEventListener("mousemove", onMouseMove);
                window.removeEventListener("mouseup", onMouseUp);
            };

            window.addEventListener("mousemove", onMouseMove);
            window.addEventListener("mouseup", onMouseUp);
        };

        onMounted(() => {
            try {
                const user = JSON.parse(localStorage.getItem("user"));
                if (!user || !user.defaultCalendarId) {
                    setErrorMessage(errorMessage, "Default calendar has been deleted.");
                    console.error("Default calendar has been deleted.");
                }

                const existingCalendarId = localStorage.getItem("calendarId");
                if (!existingCalendarId) {
                    localStorage.setItem("calendarId", user.defaultCalendarId);
                }
            } catch (error) {
                setErrorMessage(errorMessage, "Error loading calendar data.");
                console.error("Error loading calendar data:", error.message);
            } finally {
                isMounted.value = true;
            }
        });

        return {
            isMounted,
            errorMessage,
            calendarViewSize,
            calendarListsSize,
            startResizing,
        };
    },
};
</script>

<style scoped>
.dashboard {
    display: flex;
    flex-direction: column;
    gap: 5px;
    padding: 0 15px 0px 15px;
    height: 100%;
}

.error-message {
    color: red;
    margin-top: 5px;
    font-size: 0.9em;
}

.calendar-container {
    display: flex;
    width: 100%;
    height: 100%;
}

.resizer {
    width: 5px;
    cursor: ew-resize;
    background-color: #ccc;
    transition: background-color 0.2s;
    flex-shrink: 0;
}

.resizer:hover {
    background-color: #999;
}

@media (max-width: 700px) {
    .calendar-container {
        flex-direction: column;
    }

    .resizer {
        width: 100%;
        height: 5px;
        cursor: ns-resize;
        flex-shrink: 0;
        display: none
    }
}
</style>
