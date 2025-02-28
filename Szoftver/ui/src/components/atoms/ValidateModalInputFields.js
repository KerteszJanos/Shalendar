export function validateNameField(name) {
    if (!name || name.trim() === "") {
        return "The name cannot be empty.";
    }
    if (name.length > 255) {
        return "The name cannot exceed 255 characters.";
    }
    return null; // No errors
}

export function validateTimeFieldsBothRequired(startTime, endTime) {
    // Ha mindkettő null, az is érvényes
    if (!startTime && !endTime) {
        return null; // No errors, valid case
    }

    // Ha csak az egyik null, az hiba
    if (!startTime || !endTime) {
        return "Both start and end times are required or both should be empty.";
    }

    // A type="time" input értéke 'HH:mm' formátumú, így közvetlenül feldolgozhatjuk
    const [startHours, startMinutes] = startTime.split(":").map(Number);
    const [endHours, endMinutes] = endTime.split(":").map(Number);

    if (
        isNaN(startHours) || isNaN(startMinutes) ||
        isNaN(endHours) || isNaN(endMinutes)
    ) {
        return "Invalid time format.";
    }

    // Start és End percekre átalakítva az egyszerűbb összehasonlítás érdekében
    const startTotalMinutes = startHours * 60 + startMinutes;
    const endTotalMinutes = endHours * 60 + endMinutes;

    if (startTotalMinutes >= endTotalMinutes - 10) {
        return "The start time must be at least 10 minutes earlier than the end time.";
    }

    return null; // No errors
};