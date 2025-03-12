<script lang="ts">
    import { hideModal } from "./modalStore";
    import { fade, fly, scale } from "svelte/transition";
    import { quintOut, elasticOut } from "svelte/easing";
    import type { Component } from "svelte";
    let { item, show = false, onNegative } = $props();
    let isVisible = $state(false);
    let isRendered = $state(false);
    
    // 使用$effect来处理show属性变化
    $effect(() => {
        if (show) {
            // 当show变为true时，先设置isRendered为true以开始渲染
            isRendered = true;
            // 稍微延迟设置isVisible，让DOM有时间先渲染
            setTimeout(() => {
                isVisible = true;
            }, 50);
        } else {
            // 先隐藏，然后在过渡结束后停止渲染
            isVisible = false;
            setTimeout(() => {
                isRendered = false;
            }, 400); // 与过渡动画时长匹配
        }
    });
    
    function handleOverlayClick(event: MouseEvent) {
        if (event.target === event.currentTarget) {
            hideModal();
        }
    }
    
    function handleClose() {
        show = false;
        onNegative();
    }
</script>

{#if isRendered}
    <!-- svelte-ignore a11y_click_events_have_key_events -->
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <div
        class="modal-overlay {isVisible ? 'visible' : ''}"
        onclick={handleOverlayClick}
    >
        <div
            class="modal {isVisible ? 'visible' : ''}"
        >
            <div class="flex w-full items-center justify-end px-6 py-4">
                <!-- svelte-ignore a11y_consider_explicit_label -->
                <button
                    title="close"
                    class="text-gray-400 hover:text-gray-500 dark:hover:text-gray-300"
                    onclick={handleClose}
                >
                    <svg
                        class="h-6 w-6"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                    >
                        <path
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            stroke-width="2"
                            d="M6 18L18 6M6 6l12 12"
                        />
                    </svg>
                </button>
            </div>
                <div class="px-3 md:px-6 w-full">
                    {@render slot?.(item)}
                </div>
        </div>
    </div>
{/if}

{#snippet slot(item: { component: Component; props: Record<string, unknown> })}
    <item.component {...item.props} />
{/snippet}

<style>
    .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0);
        z-index: 999;
        opacity: 0;
        transition: opacity 0.3s ease, background 0.3s ease;
        pointer-events: none;
    }
    
    .modal-overlay.visible {
        background: rgba(0, 0, 0, 0.5);
        opacity: 1;
        pointer-events: auto;
    }

    .modal {
        position: fixed;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -48%);
        width: auto;
        min-width:40%;
        height: 90%;
        background: white;
        border-radius: 8px;
        z-index: 1000;
        overflow: hidden;
        opacity: 0;
        transition: all 0.4s cubic-bezier(0.19, 1, 0.22, 1);
        box-shadow: 0 10px 25px rgba(0, 0, 0, 0);
    }
    
    .modal.visible {
        opacity: 1;
        transform: translate(-50%, -50%);
        box-shadow: 0 10px 25px rgba(0, 0, 0, 0.1);
    }
    
</style>
