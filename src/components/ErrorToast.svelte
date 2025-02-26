<script lang="ts">
    import { errorStore } from '../biz/errorHandler';
</script>

{#if $errorStore.show}
    <div class="toast-container">
        <div class="toast {$errorStore.type}">
            {$errorStore.message}
        </div>
    </div>
{/if}

<style>
    .toast-container {
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
    }

    .toast {
        padding: 12px 24px;
        border-radius: 4px;
        color: white;
        font-size: 14px;
        margin-bottom: 10px;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
        animation: slideIn 0.3s ease-out;
    }

    .error {
        background-color: #ff4444;
    }

    .warning {
        background-color: #ffbb33;
    }

    .info {
        background-color: #33b5e5;
    }

    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
</style>
