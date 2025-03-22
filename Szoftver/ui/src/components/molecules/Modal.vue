<!--
  Parent modal component used by all modals; provides title, confirm/cancel buttons, and click-outside-to-close behavior.
-->

<template>
<div v-if="show" class="modal-overlay" @mousedown="startClick" @mouseup="endClick">
    <div class="modal" ref="modalRef" @click.stop>
        <h2>{{ title }}</h2>
        <slot></slot>
        <div class="modal-actions">
            <button class="modal-button" @click="closeModal">Cancel</button>
            <button class="modal-button" @click="confirmAction">{{ confirmText }}</button>
        </div>
    </div>
</div>
</template>

<script>
import {
    defineComponent,
    ref
} from "vue";

export default defineComponent({
    props: {
        show: Boolean,
        title: String,
        confirmText: {
            type: String,
            default: "Save"
        }
    },
    emits: ["close", "confirm"],
    setup(props, {
        emit
    }) {

        // ---------------------------------
        // Reactive state                  |
        // ---------------------------------
        const modalRef = ref(null);
        const isClickInside = ref(false);

        // ---------------------------------
        // Methods                         |
        // ---------------------------------
        const startClick = (event) => {
            if (modalRef.value && modalRef.value.contains(event.target)) {
                isClickInside.value = true;
            } else {
                isClickInside.value = false;
            }
        };

        const endClick = () => {
            if (!isClickInside.value) {
                emit("close");
            }
        };

        const closeModal = () => {
            emit("close");
        };

        const confirmAction = () => {
            emit("confirm");
        };

        return {
            closeModal,
            confirmAction,
            modalRef,
            startClick,
            endClick
        };
    }
});
</script>

<style>
.modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.7);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 9999;
}

.modal {
    background: #f4f7fc;
    padding: 20px;
    border-radius: 10px;
    border: 3px solid #213A57;
    width: 400px;
    text-align: center;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.2);
    z-index: 10000;
}

.modal-actions {
    margin-top: 20px;
    display: flex;
    justify-content: space-between;
}

.modal-button {
    background-color: #213A57;
    color: white;
    border: none;
    padding: 5px 10px;
    border-radius: 5px;
    cursor: pointer;
    font-size: 16px;
    transition: all 0.3s ease-in-out;
}

.modal-button:hover {
    background-color: #172738;
    transform: scale(1.1);
}

.modal-content select,
.modal-content input,
.modal-content textarea {
    width: 100%;
    padding: 8px;
    border: 2px solid #0B6477;
    border-radius: 5px;
    font-size: 16px;
    outline: none;
    transition: border-color 0.3s ease-in-out;
}

.modal-content select:focus,
.modal-content input:focus,
.modal-content textarea:focus {
    border-color: #213A57;
}
</style>
