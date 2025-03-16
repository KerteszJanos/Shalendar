<template>
<div class="dashboard" v-if="isMounted">
    <p v-if="errorMessage" class="error-message">{{ errorMessage }}</p>
    <div class="calendar-container">
        <CalendarView :style="{ flex: calendarViewSize }" class="calendarView" />
        <div class="resizer" @mousedown="startResizing"></div>
        <CalendarLists :style="{ flex: calendarListsSize }" class="calendarLists" />
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

            const isHorizontal = window.innerWidth > 700; // 700px felett vízszintes, alatta függőleges

            const startX = event.clientX;
            const startY = event.clientY;
            const startViewSize = calendarViewSize.value;
            const startListSize = calendarListsSize.value;

            // ✨ Külön min-height értékek százalékban (ha az ablak méretéhez igazítjuk)
            const minHeightViewRatio = 400 / window.innerHeight * 2; // Pl. min-height: 350px
            const minHeightListsRatio = 300 / window.innerHeight * 2; // Pl. min-height: 250px

            const onMouseMove = (moveEvent) => {
                if (!isResizing) return;

                if (isHorizontal) {
                    // 📌 Vízszintes mozgatás (X tengely)
                    const deltaX = (moveEvent.clientX - startX) / window.innerWidth * 2;
                    const newViewSize = startViewSize + deltaX;
                    const newListSize = startListSize - deltaX;

                    // ✨ Külön min értékek figyelembevétele
                    if (newViewSize >= 0.2 && newListSize >= 0.2) {
                        calendarViewSize.value = newViewSize;
                        calendarListsSize.value = newListSize;
                    }
                } else {
                    // 📌 Függőleges mozgatás (Y tengely) külön min-height értékekkel
                    const deltaY = (moveEvent.clientY - startY) / window.innerHeight * 2;
                    const newViewSize = startViewSize + deltaY;
                    const newListSize = startListSize - deltaY;

                    // **Ha a CalendarView mérete kisebb lenne a saját min-height-jánál, ne engedjük tovább csökkenteni**
                    if (newViewSize >= minHeightViewRatio && newListSize >= minHeightListsRatio) {
                        calendarViewSize.value = newViewSize;
                        calendarListsSize.value = newListSize;
                    }
                }
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

.calendarView {
    min-height: 300px;
}

.calendarLists {
    min-height: 300px;
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
        flex: 1;
    }

    .calendar-container>* {
        flex-grow: 1;
        min-height: 0;
    }

    .resizer {
        width: 100%;
        height: 5px;
        cursor: ns-resize;
        flex-shrink: 0;
        max-height: 5px;
    }
}
</style>
