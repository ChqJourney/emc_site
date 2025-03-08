<script lang="ts">
  import { hideModal } from './modalStore';
  import { fade, scale } from 'svelte/transition';
  import { quintOut } from 'svelte/easing';
  import type{ Component } from 'svelte';
  let { item, show = false, onNegative } = $props();
  function handleOverlayClick(event: MouseEvent) {
      if (event.target === event.currentTarget) {
          hideModal()
      }
  }
  function handleClose() {
      show = false;
      onNegative()
  }
</script>

{#if show}
<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="modal-overlay" 
  onclick={handleOverlayClick}
  transition:fade={{ duration: 400 }}>
  <div class="modal" 
      transition:scale={{ duration: 400, easing: quintOut }}>
      <div class="flex w-full items-center justify-end px-6 py-4">
          <!-- svelte-ignore a11y_consider_explicit_label -->
          <button title="close"
              class="text-gray-400 hover:text-gray-500 dark:hover:text-gray-300"
              onclick={handleClose}
          >
              <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M6 18L18 6M6 6l12 12"
                  />
              </svg>
          </button>
      </div>
      <div>
          <div class="px-3 md:px-6">
              
              {@render slot?.(item)}
          </div>
      </div>
      
  </div>
</div>
{/if}

{#snippet slot(item:{component:Component,props:Record<string,unknown>})}
  <item.component {...item.props}/>
{/snippet}

<style>
  .modal {
position: fixed;
top: 50%;
left: 50%;
transform: translate(-50%, -50%);
width: 50%; /* 默认宽度设置为50% */
max-width: 70%;
max-height: 90vh;
overflow-y: auto;
background: white;
border-radius: 8px;
z-index: 1000;
}

.modal-overlay {
position: fixed;
top: 0;
left: 0;
right: 0;
bottom: 0;
background: rgba(0, 0, 0, 0.5);
z-index: 999;
}
@media (max-width: 768px) {
.modal {
  width: 90%;
  max-width: 90%;
  padding: 15px;
}
}
</style>