<script lang="ts">
  import { fade } from 'svelte/transition';
  import { modalStore } from './modalStore';
  let {show, onClose}=$props();

  function handleOverlayClick(event: MouseEvent) {
    // 确保点击的是遮罩层而不是模态框内容
    if (event.target === event.currentTarget) {
      onClose();
    }
  }
</script>

{#if show}
  
  <div 
    class="modal-overlay" 
    transition:fade 
    onclick={handleOverlayClick}
  >
    <div class="modal">

      {@render $modalStore.component?.()}
    </div>
  </div>
{/if}

<style>
  .modal-overlay {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(0, 0, 0, 0.5);
    display: flex;
    justify-content: center;
    align-items: center;
    z-index: 10000;
  }

  .modal {
    background: white;
    padding: 0rem 2rem;
    border-radius: 8px;
    width: 90%;
    max-width: 500px;
    max-height: 90vh;
    overflow-y: auto;
  }
</style>