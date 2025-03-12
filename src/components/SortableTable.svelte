<script lang="ts">
    import { onMount } from 'svelte';
    let {
      data = [],
      isIdxShow = true,
      columns = [],
      actions = [],
      rowSelect = (row: any, idx: number) => {},
      draggable = false, // 新增draggable参数
      onRowReorder = (order:{id:number,seq:number}[]) => {}, // 新增回调函数
      // 分页相关参数
      pagination = false, 
      currentPage = 1, 
      pageSize = 10, 
      totalItems = 0, 
      pageSizeOptions = [10, 20, 50, 100], 
      onPageChange = (page: number) => {}, 
      onPageSizeChange = (size: number) => {}, 
      loading = false, 
      emptyText = "暂无数据" 
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
      pagination?: boolean;
      currentPage?: number;
      pageSize?: number;
      totalItems?: number;
      pageSizeOptions?: number[];
      onPageChange?: (page: number) => void;
      onPageSizeChange?: (size: number) => void;
      loading?: boolean;
      emptyText?: string;
    } = $props();
  
    let sortKey: string | null = $state(null);
    let sortDirection: "asc" | "desc" = $state("asc");
    let selectedRow: any = $state(null);
    let openDropdownIndex = $state(-1); // Add state for tracking which dropdown is open
  
    let sortedData = $state<any[]>([...data]);
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
  
    // 计算总页数
    let totalPages = $state(Math.max(1, Math.ceil(totalItems / pageSize)));
    $effect(() => {
      totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
      // 修正当前页码
      if (currentPage > totalPages) {
        handlePageChange(totalPages);
      }
    });
  
    // 计算分页数据
    let paginatedData = $state<any[]>([]);
    $effect(() => {
      if (pagination && !loading) {
        // 服务端分页时，数据由外部传入
        paginatedData = [...sortedData];
      } else {
        // 前端分页
        paginatedData = sortedData;
      }
    });
  
    // 分页相关变量
    let pageSizeValue = $state(pageSize);
    
    // 生成页码数组
    let pageNumbers = $state<number[]>([]);
    $effect(() => {
      const current = currentPage;
      const total = totalPages;
      const maxDisplayed = 5; // 最多显示的页码数
      
      let pages: number[] = [];
      if (total <= maxDisplayed) {
        // 总页数较少，全部显示
        pages = Array.from({length: total}, (_, i) => i + 1);
      } else {
        // 总页数较多，显示部分
        if (current <= 3) {
          // 当前页靠前
          pages = [1, 2, 3, 4, 5];
        } else if (current >= total - 2) {
          // 当前页靠后
          pages = [total - 4, total - 3, total - 2, total - 1, total];
        } else {
          // 当前页在中间
          pages = [current - 2, current - 1, current, current + 1, current + 2];
        }
      }
      
      pageNumbers = pages;
    });
  
    // 处理页码变化
    function handlePageChange(page: number) {
      if (page !== currentPage && page >= 1 && page <= totalPages) {
        currentPage = page;
        onPageChange(page);
      }
    }
  
    // 处理每页数量变化
    function handlePageSizeChange(event: Event) {
      const select = event.target as HTMLSelectElement;
      const newSize = parseInt(select.value, 10);
      if (newSize !== pageSize) {
        pageSizeValue = newSize;
        onPageSizeChange(newSize);
      }
    }
  
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
      {#if loading}
        <div class="loading-overlay">
          <div class="spinner"></div>
          <span>加载中...</span>
        </div>
      {/if}
      
      <table class:loading>
        <thead>
          <tr>
            {#if isIdxShow}
            <th>No.</th>
            {/if}
            {#each columns as column}
              <th style={column.maxWidth ? `max-width: ${column.maxWidth}` : ""}>
                <div class="th-content">
                  <span>{column.label}</span>
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
              </th>
            {/each}
            {#if actions.length > 0}
              <th>操作</th>
            {/if}
          </tr>
        </thead>
        <tbody>
          {#if paginatedData.length === 0}
            <tr class="empty-row">
              <td colspan={columns.length + (isIdxShow ? 1 : 0) + (actions.length > 0 ? 1 : 0)}>
                <div class="empty-placeholder">
                  {emptyText}
                </div>
              </td>
            </tr>
          {:else}
            {#each paginatedData as row, idx}
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
                <td>{pagination ? (currentPage - 1) * pageSize + idx + 1 : idx + 1}</td>
                {/if}
                {#each columns as column}
                  <td style={column.maxWidth ? `max-width: ${column.maxWidth}` : ""}>
                    <div class="td-content">
                      {#if column.formatter}
                        {#await column.formatter(row[column.key])}
                          <span class="loading-text">加载中...</span>
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
                            class:active={openDropdownIndex === idx}
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
          {/if}
        </tbody>
      </table>
      
      {#if pagination && totalItems > 0}
        <div class="pagination-container">
          <div class="pagination-info">
            共 <span class="pagination-total">{totalItems}</span> 条记录，每页
            <select class="page-size-select" value={pageSizeValue} onchange={handlePageSizeChange}>
              {#each pageSizeOptions as option}
                <option value={option}>{option}</option>
              {/each}
            </select>
            条
          </div>
          
          <div class="pagination-controls">
            <button 
              class="pagination-button" 
              disabled={currentPage === 1}
              onclick={() => handlePageChange(1)}
            >
              «
            </button>
            
            <button 
              class="pagination-button" 
              disabled={currentPage === 1}
              onclick={() => handlePageChange(currentPage - 1)}
            >
              ‹
            </button>
            
            {#each pageNumbers as pageNumber}
              <button 
                class="pagination-button" 
                class:active={pageNumber === currentPage}
                onclick={() => handlePageChange(pageNumber)}
              >
                {pageNumber}
              </button>
            {/each}
            
            <button 
              class="pagination-button" 
              disabled={currentPage === totalPages}
              onclick={() => handlePageChange(currentPage + 1)}
            >
              ›
            </button>
            
            <button 
              class="pagination-button" 
              disabled={currentPage === totalPages}
              onclick={() => handlePageChange(totalPages)}
            >
              »
            </button>
          </div>
          
          <div class="pagination-jumper">
            跳至
            <input 
              type="number" 
              class="pagination-input"
              min="1"
              max={totalPages}
              value={currentPage}
              onkeydown={(e) => {
                if (e.key === 'Enter') {
                  const input = e.target as HTMLInputElement;
                  const page = Math.min(Math.max(1, parseInt(input.value, 10) || 1), totalPages);
                  handlePageChange(page);
                }
              }}
            />
            页
          </div>
        </div>
      {/if}
    </div>
  
  <style>
    .table-container {
      position: relative;
      width: 100%;
      overflow-x: auto;
      background: white;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
    }
    
    .loading-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(255, 255, 255, 0.7);
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      z-index: 10;
    }
    
    .spinner {
      width: 40px;
      height: 40px;
      border: 3px solid #f3f3f3;
      border-top: 3px solid #4a90e2;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 8px;
    }
    
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
    
    table {
      min-width: 1000px;
      width: 100%;
      border-collapse: collapse;
      border-spacing: 0;
      transition: opacity 0.3s;
    }
    
    table.loading {
      opacity: 0.6;
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

    th {
      position: sticky;
      top: 0;
      background: #f7f7f7;
      z-index: 1;
      padding: 14px 12px;
      text-align: center;
      border-bottom: 2px solid #ddd;
      font-size: 0.85rem;
      font-weight: 600;
      color: #333;
      transition: background-color 0.2s;
    }
    
    th:hover {
      background-color: #eaeaea;
    }
    
    th,
    td {
      padding: 12px;
      border-bottom: 1px solid #e8e8e8;
      white-space: normal; /* 允许文本换行 */
      word-wrap: break-word; /* 长单词换行 */
    }
    
    .th-content {
      display: flex;
      align-items: center;
      gap: 8px;
      justify-content: center;
    }

    .sort-button {
      background: none;
      border: none;
      padding: 4px;
      cursor: pointer;
      display: flex;
      align-items: center;
      transition: transform 0.2s;
    }
    
    .sort-button:hover {
      transform: scale(1.1);
    }

    .sort-icon {
      fill: #ccc;
      transition: transform 0.2s, fill 0.2s;
    }

    .sort-icon.active {
      fill: #4a90e2;
    }

    .sort-icon.asc {
      transform: rotate(180deg);
    }

    td {
      font-size: 0.85rem;
      color: #333;
    }
    
    .loading-text {
      color: #999;
      font-style: italic;
    }
    
    .td-content {
      width: 100%;
      overflow-wrap: break-word; /* 确保长文本会换行 */
    }
    
    .empty-row td {
      text-align: center;
      padding: 40px 0;
    }
    
    .empty-placeholder {
      color: #999;
      font-size: 1rem;
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
      padding: 6px 10px;
      background-color: #ffffff;
      border: 1px solid #d9d9d9;
      border-radius: 4px;
      cursor: pointer;
      display: flex;
      align-items: center;
      gap: 4px;
      min-width: 4rem;
      height: 100%;
      transition: all 0.2s;
    }

    .dropdown-button:hover {
      background-color: #f5f5f5;
      border-color: #4a90e2;
    }

    .dropdown-menu {
      position: absolute;
      right: 0;
      top: 100%;
      margin-top: 4px;
      background-color: white;
      border: 1px solid #d9d9d9;
      border-radius: 4px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      z-index: 1000;
      min-width: 120px;
      animation: dropdownFadeIn 0.2s ease-out;
      transform-origin: top right;
    }
    
    @keyframes dropdownFadeIn {
      from {
        opacity: 0;
        transform: scale(0.95);
      }
      to {
        opacity: 1;
        transform: scale(1);
      }
    }

    .dropdown-menu button {
      display: block;
      width: 100%;
      padding: 8px 12px;
      text-align: left;
      border: none;
      background: none;
      cursor: pointer;
      transition: background-color 0.2s;
      color: #333;
    }

    .dropdown-menu button:hover {
      background-color: #f0f7ff;
    }
    
    .dropdown-menu button.edit:hover {
      color: #1890ff;
    }
    
    .dropdown-menu button.delete:hover {
      color: #ff4d4f;
    }

    .dropdown-icon {
      fill: currentColor;
      transition: transform 0.2s;
    }
    
    .dropdown-icon.active {
      transform: rotate(180deg);
    }
    
    /* 拖拽相关样式 */
    tr.dragging {
      opacity: 0.5;
      background-color: #f0f0f0;
      cursor: grabbing;
    }

    tr.drag-over {
      border-top: 2px solid #4a90e2;
    }
    
    /* 分页相关样式 */
    .pagination-container {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 16px;
      border-top: 1px solid #e8e8e8;
      font-size: 0.85rem;
      color: #666;
      flex-wrap: wrap;
      gap: 10px;
    }
    
    .pagination-info {
      display: flex;
      align-items: center;
      gap: 4px;
    }
    
    .pagination-total {
      font-weight: bold;
      color: #4a90e2;
    }
    
    .page-size-select {
      padding: 4px 8px;
      border: 1px solid #d9d9d9;
      border-radius: 4px;
      background-color: white;
      margin: 0 4px;
      cursor: pointer;
      transition: border-color 0.2s;
    }
    
    .page-size-select:hover,
    .page-size-select:focus {
      border-color: #4a90e2;
      outline: none;
    }
    
    .pagination-controls {
      display: flex;
      align-items: center;
      gap: 4px;
    }
    
    .pagination-button {
      display: flex;
      align-items: center;
      justify-content: center;
      min-width: 32px;
      height: 32px;
      padding: 0 8px;
      border: 1px solid #d9d9d9;
      border-radius: 4px;
      background-color: white;
      cursor: pointer;
      transition: all 0.2s;
    }
    
    .pagination-button:hover:not(:disabled) {
      border-color: #4a90e2;
      color: #4a90e2;
    }
    
    .pagination-button.active {
      background-color: #4a90e2;
      color: white;
      border-color: #4a90e2;
    }
    
    .pagination-button:disabled {
      cursor: not-allowed;
      opacity: 0.5;
      background-color: #f5f5f5;
    }
    
    .pagination-jumper {
      display: flex;
      align-items: center;
      gap: 4px;
    }
    
    .pagination-input {
      width: 50px;
      padding: 4px 8px;
      border: 1px solid #d9d9d9;
      border-radius: 4px;
      text-align: center;
      transition: border-color 0.2s;
    }
    
    .pagination-input:hover,
    .pagination-input:focus {
      border-color: #4a90e2;
      outline: none;
    }
    
    @media (max-width: 768px) {
      .pagination-container {
        flex-direction: column;
        align-items: flex-start;
      }
      
      .pagination-controls {
        margin: 10px 0;
      }
    }
  </style>
  