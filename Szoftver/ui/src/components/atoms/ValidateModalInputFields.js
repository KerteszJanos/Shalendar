// Validates the ticket name field: not empty and max 255 characters.
export function validateNameField(name) {
    if (!name || name.trim() === "") {
        return "The name cannot be empty.";
    }
    if (name.length > 255) {
        return "The name cannot exceed 255 characters.";
    }
    return null;
}

// Validates that both times are set or empty, and start is at least 15 mins before end.
export function validateTimeFieldsBothRequiredOrEmpty(startTime, endTime) {
    if (!startTime && !endTime) {
        return null;
    }

    if (!startTime || !endTime) {
        return "Both start and end times are required or both should be empty.";
    }

    const [startHours, startMinutes] = startTime.split(":").map(Number);
    const [endHours, endMinutes] = endTime.split(":").map(Number);

    if (
        isNaN(startHours) || isNaN(startMinutes) ||
        isNaN(endHours) || isNaN(endMinutes)
    ) {
        return "Invalid time format.";
    }

    const startTotalMinutes = startHours * 60 + startMinutes;
    const endTotalMinutes = endHours * 60 + endMinutes;

    if (startTotalMinutes >= endTotalMinutes - 14) {
        return "The start time must be at least 15 minutes earlier than the end time.";
    }

    return null;
};

// Validates the priority value: must be between 0 and 9.
export function validatePriorityField(priority) {
    if (priority < 0) {
        return "The priority must be at least 1. (or 0 to leave it empty)";
    }
    if (priority > 9) {
        return "The priority cannot exceed 9.";
    }
    return null;
}