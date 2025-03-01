export function validateNameField(name) {
    if (!name || name.trim() === "") {
        return "The name cannot be empty.";
    }
    if (name.length > 255) {
        return "The name cannot exceed 255 characters.";
    }
    return null;
}

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

    if (startTotalMinutes >= endTotalMinutes - 10) {
        return "The start time must be at least 10 minutes earlier than the end time.";
    }

    return null;
};