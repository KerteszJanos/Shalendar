//Handles timed display of error messages by setting and auto-clearing them.
export const setErrorMessage = (() => {
    //A WeakMap allows associating data with object keys while automatically removing entries when the referenced objects are garbage-collected, thereby preventing memory leaks.
    const timeoutMap = new WeakMap();

    return (errorMessageRef, message, timeout = 5000) => {
        if (timeoutMap.has(errorMessageRef)) {
            clearTimeout(timeoutMap.get(errorMessageRef));
        }

        errorMessageRef.value = message;

        const timeoutId = setTimeout(() => {
            errorMessageRef.value = "";
            timeoutMap.delete(errorMessageRef);
        }, timeout);

        timeoutMap.set(errorMessageRef, timeoutId);
    };
})();
