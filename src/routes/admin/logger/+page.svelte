<script lang="ts">
    import { onMount } from 'svelte';
    import { fade } from 'svelte/transition';
    import { apiService } from '../../../biz/apiService';
    
    // 定义类型
    interface LogFile {
        fileName: string;
        date: string;
        size: number;
        lastModified: string;
    }
    
    interface ExceptionInfo {
        Type?: string;
        Message?: string;
        StackTrace?: string;
    }
    
    interface RequestInfo {
        TraceId?: string;
        Method?: string;
        Path?: string;
        QueryString?: string;
        ClientIp?: string;
        UserAgent?: string;
    }
    
    interface LogEntry {
        Timestamp: string;
        Level: string;
        Message: string;
        Exception?: ExceptionInfo;
        ThreadId: number;
        MachineName: string;
        ProcessId: number;
        RequestInfo?: RequestInfo;
    }
    
    // 状态变量
    let logFiles: LogFile[] = [];
    let selectedDate = '';
    let logs: LogEntry[] = [];
    let loading = false;
    let error: string | null = null;
    
    // 过滤和排序状态
    let levelFilter = 'all';
    let timeRange = 'today';
    let sortField: keyof LogEntry = 'Timestamp';
    let sortDirection: 'asc' | 'desc' = 'desc';
    let searchKeyword = '';
    
    // 时间范围选项
    const timeRanges = [
        { id: 'today', name: '当日' },
        { id: 'this-month', name: '当月' },
        { id: 'this-year', name: '当年' }
    ];
    
    // 日志级别选项
    const logLevels = [
        { id: 'all', name: '全部' },
        { id: 'Debug', name: '调试' },
        { id: 'Information', name: '信息' },
        { id: 'Warning', name: '警告' },
        { id: 'Error', name: '错误' }
    ];
    
    // 加载日志文件列表
    async function loadLogFiles(): Promise<void> {
        loading = true;
        error = null;
        
        try {
            // 添加更详细的日志
            console.log('开始加载日志文件列表...');
            
            // apiService.Get直接返回解析后的数据，而不是Response对象
            const data = await apiService.Get('/logs');
            console.log('获取到的日志文件列表:', data);
            
            logFiles = data;
            console.log('日志文件列表:', logFiles);
            
            // 如果有日志文件，默认选择最新的日志文件
            if (logFiles.length > 0) {
                selectedDate = logFiles[0].date;
                await loadLogs(selectedDate);
            } else {
                console.log('没有找到日志文件');
            }
        } catch (err: unknown) {
            const errorMessage = err instanceof Error ? err.message : JSON.stringify(err);
            error = `加载日志文件时出错: ${errorMessage}`;
            console.error('加载日志文件出错:', err);
        } finally {
            loading = false;
        }
    }
    
    // 加载指定日期的日志
    async function loadLogs(date: string): Promise<void> {
        if (!date) return;
        
        loading = true;
        error = null;
        
        try {
            // 构建查询参数
            let url = `/logs/${date}`;
            
            if (levelFilter !== 'all' || searchKeyword) {
                url = `/logs/filter?date=${date}`;
                
                if (levelFilter !== 'all') {
                    url += `&level=${levelFilter}`;
                }
                
                if (searchKeyword) {
                    url += `&keyword=${encodeURIComponent(searchKeyword)}`;
                }
            }
            
            // 直接获取数据，而不是Response对象
            logs = await apiService.Get(url);
            console.log('获取到的日志数据:', logs);
            sortLogs();
        } catch (err: unknown) {
            const errorMessage = err instanceof Error ? err.message : JSON.stringify(err);
            error = `加载日志数据时出错: ${errorMessage}`;
            console.error(error);
            logs = [];
        } finally {
            loading = false;
        }
    }
    
    // 应用过滤器
    function applyFilters(): void {
        if (selectedDate) {
            loadLogs(selectedDate);
        }
    }
    
    // 处理时间范围变化
    function handleTimeRangeChange(): void {
        const now = new Date();
        
        switch (timeRange) {
            case 'today':
                // 当日 - 使用当前日期
                selectDateByString(formatDate(now));
                break;
            case 'this-month':
                // 当月 - 查找当月的所有日志文件
                const currentMonth = now.getMonth() + 1;
                const currentYear = now.getFullYear();
                const monthPrefix = `${currentYear}-${currentMonth.toString().padStart(2, '0')}`;
                
                const monthFiles = logFiles.filter(file => file.date.startsWith(monthPrefix));
                if (monthFiles.length > 0) {
                    selectDateByString(monthFiles[0].date);
                }
                break;
            case 'this-year':
                // 当年 - 查找当年的所有日志文件
                const yearPrefix = now.getFullYear().toString();
                const yearFiles = logFiles.filter(file => file.date.startsWith(yearPrefix));
                if (yearFiles.length > 0) {
                    selectDateByString(yearFiles[0].date);
                }
                break;
        }
    }
    
    // 选择指定日期字符串对应的日志文件
    function selectDateByString(dateStr: string): void {
        const matchingFile = logFiles.find(file => file.date === dateStr);
        if (matchingFile) {
            selectedDate = matchingFile.date;
            loadLogs(selectedDate);
        }
    }
    
    // 格式化日期为 YYYY-MM-DD
    function formatDate(date: Date): string {
        const year = date.getFullYear();
        const month = (date.getMonth() + 1).toString().padStart(2, '0');
        const day = date.getDate().toString().padStart(2, '0');
        return `${year}-${month}-${day}`;
    }
    
    // 对日志排序
    function sortLogs(): void {
        if (!logs || logs.length === 0) return;
        
        logs = [...logs].sort((a, b) => {
            let aValue = a[sortField];
            let bValue = b[sortField];
            
            // 处理特殊字段
            if (sortField === 'Timestamp') {
                aValue = new Date(aValue as string).getTime();
                bValue = new Date(bValue as string).getTime();
            }
            
            // 确定排序方向
            const direction = sortDirection === 'asc' ? 1 : -1;
            
            // 处理可能的undefined值
            if (aValue === undefined && bValue === undefined) return 0;
            if (aValue === undefined) return direction; // undefined值排在后面
            if (bValue === undefined) return -direction;
            
            if (aValue < bValue) return -1 * direction;
            if (aValue > bValue) return 1 * direction;
            return 0;
        });
    }
    
    // 更改排序字段
    function changeSortField(field: keyof LogEntry): void {
        if (sortField === field) {
            // 如果点击的是当前排序字段，切换排序方向
            sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
        } else {
            // 否则，更改排序字段并设置默认排序方向
            sortField = field;
            sortDirection = 'desc';
        }
        
        sortLogs();
    }
    
    // 获取日志级别对应的样式类
    function getLevelClass(level: string): string {
        switch (level) {
            case 'Debug':
                return 'bg-gray-200 text-gray-800';
            case 'Information':
                return 'bg-blue-100 text-blue-800';
            case 'Warning':
                return 'bg-yellow-100 text-yellow-800';
            case 'Error':
                return 'bg-red-100 text-red-800';
            default:
                return 'bg-gray-200 text-gray-800';
        }
    }
    
    // 格式化时间戳
    function formatTimestamp(timestamp: string): string {
        if (!timestamp) return '';
        
        const date = new Date(timestamp);
        return date.toLocaleString('zh-CN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit'
        });
    }
    
    // 组件挂载时加载数据
    onMount(() => {
        loadLogFiles();
    });
</script>

<div class="container-wrapper h-screen overflow-y-auto">
    <div class="container mx-auto py-4 md:px-4 md:py-8">
        <div class="mb-4">
            <h4 class="text-xl font-bold text-gray-800 mb-2">系统日志</h4>
            <p class="text-gray-600">查看和管理系统日志记录</p>
        </div>
        
        <!-- 过滤器和控制区域 -->
        <div class="bg-white shadow rounded-lg mb-4 md:p-6 md:mb-8">
            <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
                <!-- 时间范围选择器 -->
                <div>
                    <label class="block text-sm font-medium text-gray-700 mb-2">时间范围</label>
                    <div class="flex space-x-4">
                        {#each timeRanges as range}
                            <label class="inline-flex items-center">
                                <input 
                                    type="radio" 
                                    bind:group={timeRange} 
                                    value={range.id} 
                                    on:change={handleTimeRangeChange}
                                    class="form-radio h-4 w-4 text-blue-600" 
                                />
                                <span class="ml-2 text-gray-700">{range.name}</span>
                            </label>
                        {/each}
                    </div>
                </div>
                
                <!-- 日志级别过滤器 -->
                <div>
                    <label for="level-filter" class="block text-sm font-medium text-gray-700 mb-2">日志级别</label>
                    <select 
                        id="level-filter" 
                        bind:value={levelFilter} 
                        on:change={applyFilters}
                        class="block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                    >
                        {#each logLevels as level}
                            <option value={level.id}>{level.name}</option>
                        {/each}
                    </select>
                </div>
                
                <!-- 搜索框 -->
                <div>
                    <label for="search" class="block text-sm font-medium text-gray-700 mb-2">搜索</label>
                    <div class="relative">
                        <input 
                            id="search" 
                            type="text" 
                            bind:value={searchKeyword} 
                            placeholder="搜索日志内容..." 
                            class="block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                        />
                        <button 
                            on:click={applyFilters}
                            class="absolute right-0 top-0 h-full px-3 py-2 text-blue-600 hover:text-blue-800"
                        >
                            搜索
                        </button>
                    </div>
                </div>
            </div>
            
            <!-- 日志文件选择器 -->
            <div class="mt-6">
                <label for="date-select" class="block text-sm font-medium text-gray-700 mb-2">选择日期</label>
                <select 
                    id="date-select" 
                    bind:value={selectedDate} 
                    on:change={() => loadLogs(selectedDate)}
                    class="block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500"
                >
                    <option value="">请选择日期</option>
                    {#each logFiles as file}
                        <option value={file.date}>{file.date} ({(file.size / 1024).toFixed(1)} KB)</option>
                    {/each}
                </select>
            </div>
        </div>
        
        <!-- 加载中提示 -->
        {#if loading}
            <div class="flex justify-center my-8" transition:fade>
                <div class="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
            </div>
        {/if}
        
        <!-- 错误信息 -->
        {#if error}
            <div class="bg-red-100 border-l-4 border-red-500 text-red-700 p-4 mb-8" transition:fade>
                <p>{error}</p>
            </div>
        {/if}
        
        <!-- 日志列表 -->
        {#if !loading && logs.length > 0}
            <div class="bg-white shadow overflow-hidden rounded-lg">
                <div class="table-container">
                    <table class="min-w-full divide-y divide-gray-200">
                        <thead class="bg-gray-50">
                            <tr>
                                <th 
                                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                                    on:click={() => changeSortField('Timestamp')}
                                >
                                    时间
                                    {#if sortField === 'Timestamp'}
                                        <span class="ml-1">{sortDirection === 'asc' ? '↑' : '↓'}</span>
                                    {/if}
                                </th>
                                <th 
                                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider cursor-pointer hover:bg-gray-100"
                                    on:click={() => changeSortField('Level')}
                                >
                                    级别
                                    {#if sortField === 'Level'}
                                        <span class="ml-1">{sortDirection === 'asc' ? '↑' : '↓'}</span>
                                    {/if}
                                </th>
                                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    信息
                                </th>
                            </tr>
                        </thead>
                        <tbody class="bg-white divide-y divide-gray-200">
                            {#each logs as log}
                                <tr class="hover:bg-gray-50">
                                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                                        {formatTimestamp(log.Timestamp)}
                                    </td>
                                    <td class="px-6 py-4 whitespace-nowrap">
                                        <span class={`px-2 py-1 inline-flex text-xs leading-5 font-semibold rounded-full ${getLevelClass(log.Level)}`}>
                                            {log.Level}
                                        </span>
                                    </td>
                                    <td class="px-6 py-4 text-sm text-gray-500">
                                        <div class="max-w-xl break-words">
                                            {log.Message}
                                            
                                            {#if log.Exception}
                                                <div class="mt-2 text-red-600 text-xs">
                                                    <details>
                                                        <summary class="cursor-pointer">查看异常详情</summary>
                                                        <div class="mt-2 p-2 bg-red-50 rounded">
                                                            <p><strong>类型:</strong> {log.Exception.Type || '未知'}</p>
                                                            <p><strong>消息:</strong> {log.Exception.Message || '未知'}</p>
                                                            {#if log.Exception.StackTrace}
                                                                <pre class="mt-2 text-xs overflow-x-auto bg-red-100 p-2 rounded">{log.Exception.StackTrace}</pre>
                                                            {/if}
                                                        </div>
                                                    </details>
                                                </div>
                                            {/if}
                                            
                                            {#if log.RequestInfo && (log.RequestInfo.Path || log.RequestInfo.Method)}
                                                <div class="mt-2 text-xs text-gray-500">
                                                    <details>
                                                        <summary class="cursor-pointer">请求信息</summary>
                                                        <div class="mt-2 p-2 bg-gray-50 rounded grid grid-cols-2 gap-1">
                                                            {#if log.RequestInfo.Method}
                                                                <div><strong>方法:</strong> {log.RequestInfo.Method}</div>
                                                            {/if}
                                                            {#if log.RequestInfo.Path}
                                                                <div><strong>路径:</strong> {log.RequestInfo.Path}</div>
                                                            {/if}
                                                            {#if log.RequestInfo.ClientIp}
                                                                <div><strong>IP:</strong> {log.RequestInfo.ClientIp}</div>
                                                            {/if}
                                                            {#if log.RequestInfo.TraceId}
                                                                <div><strong>跟踪ID:</strong> {log.RequestInfo.TraceId}</div>
                                                            {/if}
                                                        </div>
                                                    </details>
                                                </div>
                                            {/if}
                                        </div>
                                    </td>
                                </tr>
                            {/each}
                        </tbody>
                    </table>
                </div>
            </div>
            <div class="mt-4 text-sm text-gray-500 text-right">
                共 {logs.length} 条日志记录
            </div>
        {:else if !loading && !error}
            <div class="bg-white shadow rounded-lg p-8 text-center text-gray-500">
                {selectedDate ? '没有找到符合条件的日志记录' : '请选择日期查看日志'}
            </div>
        {/if}
    </div>
</div>

<style>
    /* 确保表格内容更整洁 */
    table {
        border-collapse: collapse;
        width: 100%;
    }
    
    /* 表格容器样式，添加固定高度和垂直滚动 */
    .table-container {
        max-height: calc(100vh - 300px);  /* 减去上方控件的大约高度 */
        overflow-y: auto;
        overflow-x: auto;
    }
    
    /* 添加容器包装样式 */
    :global(.container-wrapper) {
        height: 100vh;
        overflow-y: auto;
        background-color: #f3f4f6;
    }
    
    /* 自定义滚动条样式 */
    :global(.overflow-x-auto::-webkit-scrollbar),
    :global(.table-container::-webkit-scrollbar) {
        width: 8px;
        height: 8px;
    }
    
    :global(.overflow-x-auto::-webkit-scrollbar-track),
    :global(.table-container::-webkit-scrollbar-track) {
        background: #f1f1f1;
        border-radius: 4px;
    }
    
    :global(.overflow-x-auto::-webkit-scrollbar-thumb),
    :global(.table-container::-webkit-scrollbar-thumb) {
        background: #ddd;
        border-radius: 4px;
    }
    
    :global(.overflow-x-auto::-webkit-scrollbar-thumb:hover),
    :global(.table-container::-webkit-scrollbar-thumb:hover) {
        background: #ccc;
    }
</style>
