<!-- SingleSelect.svelte -->
<script lang="ts">
    let {
        options = [],
        value = "",
        placeholder = "",
        onSelect = (val: string) => {}
    } = $props();

    let showDropdown = $state(false);
    let selectedValue = $state(value);
    let componentRef: HTMLDivElement;

    $effect(() => {
        selectedValue = value;
    });

    function handleSelect(option: string) {
        selectedValue = option;
        showDropdown = false;
        onSelect(option);
    }

    function toggleDropdown() {
        showDropdown = !showDropdown;
    }

    // 点击外部关闭下拉框
    function handleClickOutside(event: MouseEvent) {
        if (componentRef && !componentRef.contains(event.target as Node)) {
            showDropdown = false;
        }
    }

    $effect(() => {
        if (showDropdown) {
            document.addEventListener('click', handleClickOutside);
        } else {
            document.removeEventListener('click', handleClickOutside);
        }
        return () => {
            document.removeEventListener('click', handleClickOutside);
        };
    });
</script>

<div class="select-container" bind:this={componentRef}>
    <!-- svelte-ignore a11y_click_events_have_key_events -->
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <div 
        class="select-input" 
        onclick={toggleDropdown}
        class:active={showDropdown}
    >
        <span class:placeholder={!selectedValue}>
            {selectedValue || placeholder}
        </span>
        <svg 
            class="arrow"
            class:up={showDropdown} 
            viewBox="0 0 24 24"
            width="16"
            height="16"
        >
            <path d="M7 10l5 5 5-5H7z"/>
        </svg>
    </div>
    
    {#if showDropdown}
        <div class="options-container">
            {#each options as option}
                <!-- svelte-ignore a11y_click_events_have_key_events -->
                <!-- svelte-ignore a11y_no_static_element_interactions -->
                <div 
                    class="option"
                    class:selected={option === selectedValue}
                    onclick={() => handleSelect(option)}
                >
                    {option}
                </div>
            {/each}
        </div>
    {/if}
</div>

<style>
    .select-container {
        position: relative;
        box-sizing: border-box;
    }

    .select-input {
        padding: 8px 12px;
        border: 1px solid #e2e8f0;
        border-radius: 6px;
        background: white;
        cursor: pointer;
        display: flex;
        justify-content: space-between;
        align-items: center;
        transition: all 0.2s;
        font-size: 0.9rem;
        box-sizing: border-box;
    }

    .select-input:hover {
        border-color: #cbd5e1;
    }

    .select-input.active {
        border-color: #3b82f6;
        box-shadow: 0 0 0 1px #3b82f6;
    }

    .placeholder {
        color: #94a3b8;
    }

    .arrow {
        fill: #64748b;
        transition: transform 0.2s;
    }

    .arrow.up {
        transform: rotate(180deg);
    }

    .options-container {
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        margin-top: 4px;
        background: white;
        box-sizing: border-box;
        border: 1px solid #e2e8f0;
        border-radius: 6px;
        box-shadow: 0 4px 6px -1px rgb(0 0 0 / 0.1);
        max-height: 200px;
        overflow-y: auto;
        z-index: 10;
    }

    .option {
        padding: 8px 12px;
        font-size: 0.9rem;
        font-weight: 400;
        cursor: pointer;
        transition: background 0.2s;
    }

    .option:hover {
        background: #f1f5f9;
    }

    .option.selected {
        background: #e2e8f0;
    }
</style>