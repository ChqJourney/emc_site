<script lang="ts">
    let {
      options = [],
      selected = [],
      onChange,
      placeholder = "请选择项目",
      isCreated = true,
    } = $props<{
      options: { name: string, value: string, most_used?: boolean,isOccupied?: boolean }[];
      selected: { name: string }[];
      placeholder?: string;
      isCreated?: boolean;
      onChange: (selected: { name: string }[]) => void;
    }>();
    let selectedValues = $state<Set<string>>(new Set());
    let isOpen = $state(false);
    let dropdownRef: HTMLDivElement;
  
    // Initialize selectedIds from props
    $effect(() => {
      selectedValues = new Set(selected.map((s: { value: string; }) => s.value));
    });
  
    function toggleOption(value: string, event: Event) {
      if (!isCreated) return;
      const option = options.find((opt: any) => opt.value === value);
      if (option?.isOccupied) return;
  
      event.stopPropagation();
      if (selectedValues.has(value)) {
        selectedValues.delete(value);
      } else {
        selectedValues.add(value);
      }
      const newSelected = Array.from(selectedValues).map(value => ({ value }));
      onChange(newSelected);
    }
  
    function toggleDropdown(event: Event) {
      if (!isCreated) return;
      event.stopPropagation();
      isOpen = !isOpen;
    }
  
    // Close dropdown when clicking outside
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef && !dropdownRef.contains(event.target as Node)) {
        isOpen = false;
      }
    }
  
    $effect(() => {
      if (isOpen) {
        document.addEventListener('click', handleClickOutside);
      } else {
        document.removeEventListener('click', handleClickOutside);
      }
      return () => {
        document.removeEventListener('click', handleClickOutside);
      };
    });
    $effect(() => {
      console.log('Selected values:', Array.from(selectedValues));
      console.log('Selected tags:', options.filter((opt: { name: string,value:string; }) => selectedValues.has(opt.value)));
    });
  </script>
  
  <div class="multi-select" bind:this={dropdownRef} class:disabled={!isCreated}>
    <div class="select-input" onclick={toggleDropdown} class:disabled={!isCreated}>
      <div class="selected-items">
        {#if selectedValues.size === 0}
          <span class="placeholder">{placeholder}</span>
        {:else}
          <div class="tags">
            {#each options.filter((opt: { name: string,value:string; }) => selectedValues.has(opt.value)) as selected}
              <span class="tag">
                {selected.name}
                <button
                  class="remove-tag"
                  onclick={(e) => toggleOption(selected.value, e)}
                  aria-label="移除 {selected.name}"
                >
                  ×
                </button>
              </span>
            {/each}
          </div>
        {/if}
      </div>
      <div class="arrow" class:open={isOpen}>▼</div>
    </div>
  
    {#if isOpen}
      <div class="dropdown-menu" 
        class:above={false} 
        onclick={(e) => e.stopPropagation()}
      >
        {#each options as option}
          <label class="option" class:occupied={option.isOccupied}>
            <input
              type="checkbox"
              checked={selectedValues.has(option.value)}
              onclick={(e) => toggleOption(option.value, e)}
              disabled={option.isOccupied}
            />
            <span class="option-text">{option.name}</span>
            {#if option.most_used}
              <span class="suggested-text">most used</span>
            {/if}
            {#if option.isOccupied}
              <span class="occupied-text">occupied</span>
            {/if}
          </label>
        {/each}
      </div>
    {/if}
  </div>
  
  <style>
    .suggested-text {
      display: inline-flex;
      align-items: center;
      padding: 2px 8px;
      margin-left: 8px;
      font-size: 0.75rem;
      font-weight: 500;
      color: #059669;
      background-color: #ecfdf5;
      border-radius: 9999px;
      border: 1px solid #10b981;
      text-transform: uppercase;
      letter-spacing: 0.05em;
  }
  
  .suggested-text::before {
      content: "★";  /* 添加一个星星图标 */
      margin-right: 4px;
      font-size: 0.8em;
  }
    .multi-select {
      position: relative;
      width: 100%;
      font-size: 14px;
    }
  
    .multi-select.disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }
  
    .select-input {
      border: 1px solid #ddd;
      border-radius: 4px;
      padding: 10px 6px;
      min-height: 36px;
      display: flex;
      align-items: center;
      justify-content: space-between;
      cursor: pointer;
      background: white;
      transition: border-color 0.2s;
    }
  
    .select-input:hover {
      border-color: #bbb;
    }
  
    .select-input.disabled {
      background-color: #f5f5f5;
      cursor: not-allowed;
    }
  
    .placeholder {
      color: #999;
    }
  
    .tags {
      display: flex;
      flex-wrap: wrap;
      gap: 4px;
      min-height: 24px;
    }
  
    .tag {
      background: #e6f3ff;
      border: 1px solid #91caff;
      color: #0958d9;
      border-radius: 4px;
      padding: 2px 8px;
      font-size: 12px;
      display: flex;
      align-items: center;
      gap: 4px;
    }
  
    .disabled .remove-tag {
      display: none;
    }
  
    .remove-tag {
      background: none;
      border: none;
      color: #0958d9;
      padding: 0;
      cursor: pointer;
      font-size: 14px;
      line-height: 1;
      display: flex;
      align-items: center;
    }
  
    .remove-tag:hover {
      color: #003eb3;
    }
  
    .arrow {
      font-size: 10px;
      color: #666;
      transition: transform 0.2s;
    }
  
    .arrow.open {
      transform: rotate(180deg);
    }
  
    .dropdown-menu {
      position: absolute;
      top: 100%;
      left: 0;
      right: 0;
      margin-top: 4px;
      background: white;
      border: 1px solid #ddd;
      border-radius: 4px;
      box-shadow: 0 2px 8px rgba(0,0,0,0.1);
      z-index: 1000;
      max-height: 200px;
      overflow-y: auto;
    }
  
    .dropdown-menu.above {
      bottom: 100%;
      top: auto;
      margin-bottom: 4px;
      margin-top: 0;
    }
  
    .option {
      display: flex;
      align-items: center;
      padding: 8px 12px;
      cursor: pointer;
      transition: background-color 0.2s;
    }
  
    .option:hover {
      background: #f5f5f5;
    }
  
    .option input[type="checkbox"] {
      margin-right: 8px;
    }
  
    .option-text {
      user-select: none;
    }
  
    .option.occupied {
      opacity: 0.6;
      cursor: not-allowed;
      background-color: #f5f5f5;
    }
  
    .occupied-text {
      color: #ff4d4f;
      font-size: 12px;
      margin-left: 8px;
    }
  
    .option.occupied input[type="checkbox"] {
      cursor: not-allowed;
    }
  
    /* Scrollbar Styling */
    .dropdown-menu::-webkit-scrollbar {
      width: 6px;
    }
  
    .dropdown-menu::-webkit-scrollbar-track {
      background: #f1f1f1;
      border-radius: 3px;
    }
  
    .dropdown-menu::-webkit-scrollbar-thumb {
      background: #ccc;
      border-radius: 3px;
    }
  
    .dropdown-menu::-webkit-scrollbar-thumb:hover {
      background: #999;
    }
  </style>