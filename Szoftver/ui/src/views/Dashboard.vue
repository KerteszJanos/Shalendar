<!--
  This component is the main container for the dashboard view.

  It includes:
  - The CalendarView (left) and CalendarLists (right) components.
  - A draggable resizer for adjusting the width (or height on small screens) of the two panels.
  - Error handling for missing default calendar setup.
  - Responsive layout: switches to vertical layout on small screens (< 700px).
-->

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
        // ---------------------------------
        // Reactive state       		   |
        // ---------------------------------
        const errorMessage = ref("");
        const isMounted = ref(false);
        const calendarViewSize = ref(1.3);
        const calendarListsSize = ref(0.7);
        // Non-reactive internal variable
        let isResizing = false;

        // ---------------------------------
        // Methods		               |
        // ---------------------------------
        // --------------
        // Core actions	|
        // --------------
        // Handles drag-based resizing between calendar view and list panel, adjusting their flex sizes dynamically depending on screen orientation.
        const startResizing = (event) => {
            document.body.style.userSelect = "none"; // Disable text selection during drag
            isResizing = true;

            const isHorizontal = window.innerWidth > 700; // Check if layout is horizontal (desktop) or vertical (mobile)

            // Store initial positions and sizes
            const startX = event.clientX;
            const startY = event.clientY;
            const startViewSize = calendarViewSize.value;
            const startListSize = calendarListsSize.value;

            // Define minimum flex ratios to prevent collapsing panels
            const minHeightViewRatio = (400 / window.innerHeight) * 2;
            const minHeightListsRatio = (250 / window.innerHeight) * 2;

            const onMouseMove = (moveEvent) => {
                if (!isResizing) return;

                if (isHorizontal) {
                    // Calculate horizontal delta and update sizes accordingly
                    const deltaX = ((moveEvent.clientX - startX) / window.innerWidth) * 2;
                    const newViewSize = startViewSize + deltaX;
                    const newListSize = startListSize - deltaX;

                    // Prevent shrinking below minimum threshold
                    if (newViewSize >= 0.2 && newListSize >= 0.2) {
                        calendarViewSize.value = newViewSize;
                        calendarListsSize.value = newListSize;
                    }
                } else {
                    // Calculate vertical delta and update sizes accordingly
                    const deltaY = ((moveEvent.clientY - startY) / window.innerHeight) * 2;
                    const newViewSize = startViewSize + deltaY;
                    const newListSize = startListSize - deltaY;

                    // Prevent shrinking below calculated min height ratios
                    if (newViewSize >= minHeightViewRatio && newListSize >= minHeightListsRatio) {
                        calendarViewSize.value = newViewSize;
                        calendarListsSize.value = newListSize;
                    }
                }
            };

            const onMouseUp = () => {
                document.body.style.userSelect = ""; // Re-enable text selection
                isResizing = false;
                window.removeEventListener("mousemove", onMouseMove);
                window.removeEventListener("mouseup", onMouseUp);
            };

            // Listen to mouse move and release globally during drag
            window.addEventListener("mousemove", onMouseMove);
            window.addEventListener("mouseup", onMouseUp);
        };

        // ---------------------------------
        // Lifecycle hooks		   |
        // ---------------------------------
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
    width: 3px;
    cursor: ew-resize;
    background-color: #A0A0A0;
    transition: background-color 0.2s;
    flex-shrink: 0;
}

.resizer:hover {
    background-color: #6e6e6e;
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
