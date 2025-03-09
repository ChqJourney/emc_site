<script lang="ts">
    import { onMount } from 'svelte';
    let {
      data = [],
      isIdxShow = true,
      columns = [],
      actions = [],
      rowSelect = (row: any, idx: number) => {},
      draggable = false, // 新增draggable参数
      onRowReorder = (order:{id:number,seq:number}[]) => {} // 新增回调函数
    }: {
      data: any;
      isIdxShow?: boolean;
      columns: {
        key: string;
        label: string;
        sortable?: boolean;
        maxWidth?: string;
        formatter?: (value: any) => string | Promise<string>;
      }[];
      actions: { label: string; handler: (item: any) => void,class:string }[];
      rowSelect?: (row: any, idx: number) => void;
      draggable?: boolean;
      onRowReorder?: (order:{id:number,seq:number}[]) => void;
    } = $props();
  
    let sortKey: string | null = $state(null);
    let sortDirection: "asc" | "desc" = $state("asc");
    let selectedRow: any = $state(null);
    let openDropdownIndex = $state(-1); // Add state for tracking which dropdown is open
  
    let sortedData = $state([...data]);
    $effect(() => {
      sortedData = [...data].sort((a, b) => {
        if (!sortKey) return 0;
  
        const aVal = a[sortKey];
        const bVal = b[sortKey];
  
        if (aVal === bVal) return 0;
        const direction = sortDirection === "asc" ? 1 : -1;
        return aVal > bVal ? direction : -direction;
      });
    });
  
    function handleSort(key: string) {
      if (sortKey === key) {
        sortDirection = sortDirection === "asc" ? "desc" : "asc";
      } else {
        sortKey = key;
        sortDirection = "asc";
      }
    }
  
    // 添加拖拽相关的状态和函数
    let draggedIndex = $state<number | null>(null);
    let dragOverIndex = $state<number | null>(null);
  
    function handleDragStart(e: DragEvent, index: number) {
      draggedIndex = index;
      e.dataTransfer?.setData('text/plain', index.toString());
      (e.target as HTMLElement).classList.add('dragging');
    }
  
    function handleDragOver(e: DragEvent, index: number) {
      e.preventDefault();
      dragOverIndex = index;
    }
    $effect(()=>{$inspect(sortedData)})
  
    function handleDrop(e: DragEvent, index: number) {
      e.preventDefault();
      if (draggedIndex === null || draggedIndex === index) return;
  
      const newData = [...sortedData];
      const [movedItem] = newData.splice(draggedIndex, 1);
      newData.splice(index, 0, movedItem);
      
      sortedData = newData;
      
      onRowReorder(sortedData.map((v,idx)=>{
        return {id:v.id,seq:idx}
      }));
      
      draggedIndex = null;
      dragOverIndex = null;
    }
  
    function handleDragEnd(e: DragEvent) {
      draggedIndex = null;
      dragOverIndex = null;
      (e.target as HTMLElement).classList.remove('dragging');
    }
  
    // 处理点击外部关闭下拉菜单
    function handleClickOutside(event: MouseEvent) {
      const target = event.target as HTMLElement;
      // 如果点击的不是下拉菜单内的元素，则关闭下拉菜单
      if (!target.closest('.dropdown')) {
        openDropdownIndex = -1;
      }
    }
  
    onMount(() => {
      // 添加全局点击事件监听
      document.addEventListener('click', handleClickOutside);
      return () => {
        // 清理事件监听
        document.removeEventListener('click', handleClickOutside);
      };
    });
  </script>
  
  <div class="table-container">
    <table>
      <thead>
        <tr>
          {#if isIdxShow}
          <th>No.</th>
          {/if}
          {#each columns as column}
            <th style={column.maxWidth ? `max-width: ${column.maxWidth}` : ""}>
              <div class="th-content">
                <div class="th-content">
                  {column.label}
                  {#if column.sortable}
                    <button
                      class="sort-button"
                      onclick={() => handleSort(column.key)}
                      aria-label={`排序 ${column.label}`}
                    >
                      <svg
                        class="sort-icon"
                        class:active={sortKey === column.key}
                        class:asc={sortKey === column.key &&
                          sortDirection === "asc"}
                        viewBox="0 0 24 24"
                        width="16"
                        height="16"
                      >
                        <path d="M7 14l5-5 5 5z" />
                      </svg>
                    </button>
                  {/if}
                </div>
              </div>
            </th>
          {/each}
          {#if actions.length > 0}
            <th>操作</th>
          {/if}
        </tr>
      </thead>
      <tbody>
        {#each sortedData as row, idx}
          <tr
          class:selected={selectedRow === row}
          class:dragging={draggedIndex === idx}
          class:drag-over={dragOverIndex === idx}
          draggable={draggable}
          ondragstart={(e) => handleDragStart(e, idx)}
          ondragover={(e) => handleDragOver(e, idx)}
          ondrop={(e) => handleDrop(e, idx)}
          ondragend={handleDragEnd}
            onclick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              selectedRow = row;
              rowSelect?.(row, idx);
            }}
            ondblclick={(e) => {
              e.preventDefault();
              e.stopPropagation();
              const editAction = actions.find((a) => a.class === "edit");
              if (editAction) editAction.handler(row);
            }}
          >
            {#if isIdxShow}
            <td>{idx + 1}</td>
            {/if}
            {#each columns as column}
              <td style={column.maxWidth ? `max-width: ${column.maxWidth}` : ""}>
                <div class="td-content">
                  {#if column.formatter}
                    {#await column.formatter(row[column.key])}
                      <span>加载中...</span>
                    {:then formattedValue}
                      {formattedValue}
                    {/await}
                  {:else}
                    {row[column.key]}
                  {/if}
                </div>
              </td>
            {/each}
            {#if actions.length > 0}
              <td>
                <div class="action-content">
                  <div class="dropdown" data-dropdown-index={idx}>
                    <button 
                      class="dropdown-button"
                      onclick={(e) => {
                        e.stopPropagation();
                        openDropdownIndex = openDropdownIndex === idx ? -1 : idx;
                      }}
                    >
                      操作
                      <svg 
                        class="dropdown-icon" 
                        viewBox="0 0 24 24" 
                        width="16" 
                        height="16"
                      >
                        <path d="M7 10l5 5 5-5z" />
                      </svg>
                    </button>
                    {#if openDropdownIndex === idx}
                      <div class="dropdown-menu">
                        {#each actions as action}
                          <button
                            class={action.class}
                            onclick={(e) => {
                              e.stopPropagation();
                              action.handler(row);
                              openDropdownIndex = -1;
                            }}
                          >
                            {action.label}
                          </button>
                        {/each}
                      </div>
                    {/if}
                  </div>
                </div>
              </td>
            {/if}
          </tr>
        {/each}
      </tbody>
    </table>
  </div>
  
  <style>
    .table-container {
      width: 100%;
      height: calc(100vh - 280px);
      overflow-y: auto;
      overflow-x: auto;
    }
  
    tr.selected {
      background-color: #e6f3ff;
    }
  
    tr:hover {
      background-color: #f5f5f5;
    }
  
    tr.selected:hover {
      background-color: #d9edff;
    }
    table {
      min-width: 1000px;
      width: 100%;
      border-collapse: collapse;
      border-spacing: 0;
    }
  
    th {
      position: sticky;
      top: 0;
      background: white;
      z-index: 1;
      padding: 12px;
      text-align: center;
      border-bottom: 2px solid #ddd;
      font-size: 0.8rem;
    }
    th,
    td {
      padding: 12px;
      border-bottom: 1px solid #ddd;
      white-space: normal; /* 允许文本换行 */
      word-wrap: break-word; /* 长单词换行 */
    }
    .th-content {
      display: flex;
      align-items: center;
      gap: 8px;
      justify-items: center;
    }
   
  
    .sort-button {
      background: none;
      border: none;
      padding: 4px;
      cursor: pointer;
      display: flex;
      align-items: center;
    }
  
    .sort-icon {
      fill: #ccc;
      transition: transform 0.2s;
    }
  
    .sort-icon.active {
      fill: #4a90e2;
    }
  
    .sort-icon.asc {
      transform: rotate(180deg);
    }
  
    td {
      font-size: 0.8rem;
    }
    .td-content {
      width: 100%;
      overflow-wrap: break-word; /* 确保长文本会换行 */
    }
    .action-content {
      height: 100%;
      width: 100%;
      padding: 0px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  
    .dropdown {
      position: relative;
    }
  
    .dropdown-button {
      padding: 6px;
      background-color: #ffffff;
      border: 1px solid #d9d9d9;
      border-radius: 4px;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 4px;
      width: 4rem;
      height: 100%;
    }
  
    .dropdown-button:hover {
      background-color: #f5f5f5;
    }
  
    .dropdown-menu {
      position: absolute;
      right: 0;
      top: 100%;
      margin-top: 0px;
      background-color: white;
      border: 1px solid #d9d9d9;
      border-radius: 4px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      z-index: 1000;
      min-width: 120px;
    }
  
    .dropdown-menu button {
      display: block;
      width: 100%;
      padding: 8px 12px;
      text-align: left;
      border: none;
      background: none;
      cursor: pointer;
    }
  
    .dropdown-menu button:hover {
      background-color: #f5f5f5;
    }
  
    .dropdown-icon {
      fill: currentColor;
    }
    /* 添加拖拽相关样式 */
    tr.dragging {
      opacity: 0.5;
      background-color: #f0f0f0;
    }
  
    tr.drag-over {
      border-top: 2px solid #4a90e2;
    }
  </style>
  