<script lang="ts">
    import { onMount } from "svelte";
    import type { Station, Sevent, SeventDTO, StationDTO } from "../biz/types";
    import { getGlobal } from "../biz/globalStore";
    import { errorHandler } from "../biz/errorHandler";
    import { invoke } from "@tauri-apps/api/core";
    import { hideModal } from "./modalStore";
let {currentStation}:{currentStation:Station}= $props();
const user=getGlobal("user");
    let sevents: Sevent[] =$state([]); 
    let editingId: number | null = $state(null) as number | null;
    let editingSevent: SeventDTO =$state({
        name: "",
        from_date: "",
        to_date: "",
        station_id: currentStation.id,
        created_By: "",
        updated_By: ""
    });
    let newSevent: SeventDTO =$state({
        name: "",
        from_date: new Date().toISOString().split("T")[0],
        to_date: new Date().toISOString().split("T")[0],
        station_id: currentStation.id,
        created_By: user.user,
        updated_By: ""
    });

    onMount(async () => {
        await loadSevents();
    });

    async function loadSevents() {
        if (currentStation.id) {
            sevents = await invoke<Sevent[]>("get_sevents_by_station_id", {id:currentStation.id});
        }
    }

    const addSevent = async () => {
        if (currentStation.id && newSevent.name && newSevent.from_date && newSevent.to_date&&newSevent.from_date<=newSevent.to_date) {
            newSevent.station_id = currentStation.id;
            newSevent.created_By = user.user;
            await invoke("create_sevent", {sevent:newSevent});
            await loadSevents();
            newSevent = {
                name: "",
                from_date: "",
                to_date: "",
                station_id: currentStation.id,
                created_By: "",
                updated_By: ""
            };
        }else{
            errorHandler.showError("信息缺失或时间范围无效");
        }
    };

    const startEdit = (sevent: Sevent) => {
        editingId = sevent.id;
        editingSevent = {
            name: sevent.name,
            from_date: sevent.from_date,
            to_date: sevent.to_date,
            station_id: sevent.station_id,
            created_By: sevent.created_by,
            updated_By: sevent.updated_by
        };
    };

    const cancelEdit = () => {
        editingId = null;
    };

    const saveEdit = async (id: number) => {
        if (editingSevent.name && editingSevent.from_date && editingSevent.to_date) {
           //to save the edit
            
            editingId = null;
            await loadSevents();
        }
    };

    const deleteSevent = async (id: number) => {
        if (confirm('确定要删除这条记录吗？')) {
            await invoke("delete_sevent", {id});
            await loadSevents();
        }
    };
</script>

<div class="events-container">
    <div class="table-responsive">
        <table>
            <thead>
                <tr>
                    <th>事件类型</th>
                    <th>开始日期</th>
                    <th>结束日期</th>
                    <th>操作</th>
                </tr>
            </thead>
            <tbody>
                {#each sevents as sevent}
                    <tr>
                        <td>
                            {#if editingId === sevent.id}
                                <select class="select-type" bind:value={editingSevent.name}>
                                    <option value="">选择类型</option>
                                    <option value="calibration">计量</option>
                                    <option value="maintenance">维护</option>
                                    <option value="out_of_service">停机</option>
                                </select>
                            {:else}
                                {#if sevent.name === 'calibration'}
                                    <span class="badge calibration">计量</span>
                                {:else if sevent.name === 'maintenance'}
                                    <span class="badge maintenance">维护</span>
                                {:else}
                                    <span class="badge out-of-service">停机</span>
                                {/if}
                            {/if}
                        </td>
                        <td>
                            {#if editingId === sevent.id}
                                <input type="date" class="date-input" bind:value={editingSevent.from_date} />
                            {:else}
                                {sevent.from_date}
                            {/if}
                        </td>
                        <td>
                            {#if editingId === sevent.id}
                                <input type="date" class="date-input" bind:value={editingSevent.to_date} />
                            {:else}
                                {sevent.to_date}
                            {/if}
                        </td>
                        <td class="actions">
                            {#if editingId === sevent.id}
                                <button class="btn-icon save" aria-label="SaveEdit" onclick={() => saveEdit(sevent.id)} title="保存">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                        <path d="M19 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v11a2 2 0 0 1-2 2z"></path>
                                        <polyline points="17 21 17 13 7 13 7 21"></polyline>
                                        <polyline points="7 3 7 8 15 8"></polyline>
                                    </svg>
                                </button>
                                <button class="btn-icon cancel" aria-label="CancelEdit" onclick={cancelEdit} title="取消">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                        <line x1="18" y1="6" x2="6" y2="18"></line>
                                        <line x1="6" y1="6" x2="18" y2="18"></line>
                                    </svg>
                                </button>
                            {:else}
                                <button class="btn-icon edit" aria-label="Edit" onclick={() => startEdit(sevent)} title="编辑">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                        <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                                        <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                                    </svg>
                                </button>
                                <button class="btn-icon delete" aria-label="Delete" onclick={() => deleteSevent(sevent.id)} title="删除">
                                    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                        <path d="M3 6h18"></path>
                                        <path d="M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6"></path>
                                        <path d="M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2"></path>
                                    </svg>
                                </button>
                            {/if}
                        </td>
                    </tr>
                {/each}
                <tr class="add-row">
                    <td>
                        <select class="select-type" bind:value={newSevent.name}>
                            <option value="">选择类型</option>
                            <option value="calibration">计量</option>
                            <option value="maintenance">维护</option>
                            <option value="out_of_service">停机</option>
                        </select>
                    </td>
                    <td><input type="date" class="date-input" bind:value={newSevent.from_date} /></td>
                    <td><input type="date" class="date-input" bind:value={newSevent.to_date} /></td>
                    <td>
                        <button class="btn-icon add" aria-label="AddSevent" onclick={addSevent} title="添加">
                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                <line x1="12" y1="5" x2="12" y2="19"></line>
                                <line x1="5" y1="12" x2="19" y2="12"></line>
                            </svg>
                        </button>
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
    <div class="footer">
        <button class="btn-exit" onclick={() => hideModal()}>
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
                <polyline points="16 17 21 12 16 7"></polyline>
                <line x1="21" y1="12" x2="9" y2="12"></line>
            </svg>
            <span>退出</span>
        </button>
    </div>
</div>

<style>
    .events-container {
        background: white;
        border-radius: 8px;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
        padding: 1rem;
        width: 100%;
    }

    .table-responsive {
        overflow-x: auto;
        -webkit-overflow-scrolling: touch;
    }

    table {
        width: 100%;
        border-collapse: separate;
        border-spacing: 0;
        margin-top: 1rem;
    }

    th {
        text-align: left;
        padding: 1rem;
        font-weight: 500;
        color: #666;
        background: #f8f9fa;
        border-bottom: 2px solid #eee;
    }

    td {
        padding: 0.875rem 1rem;
        border-bottom: 1px solid #eee;
        color: #333;
    }

    tr:last-child td {
        border-bottom: none;
    }

    .badge {
        display: inline-block;
        padding: 0.25rem 0.75rem;
        border-radius: 20px;
        font-size: 0.875rem;
        font-weight: 500;
    }

    .calibration {
        background: #e3f2fd;
        color: #1976d2;
    }

    .maintenance {
        background: #fff3e0;
        color: #f57c00;
    }

    .out-of-service {
        background: #ffebee;
        color: #d32f2f;
    }

    .btn-icon {
        padding: 0.5rem;
        border: none;
        border-radius: 6px;
        cursor: pointer;
        background: transparent;
        transition: all 0.2s;
        display: inline-flex;
        align-items: center;
        justify-content: center;
    }

    .btn-icon:hover {
        background: rgba(0, 0, 0, 0.05);
        transform: translateY(-1px);
    }

    .actions {
        display: flex;
        gap: 0.5rem;
    }

    .delete {
        color: #d32f2f;
    }

    .edit {
        color: #1976d2;
    }

    .save {
        color: #2e7d32;
    }

    .cancel {
        color: #757575;
    }

    .add {
        color: #2e7d32;
    }

    .select-type, .date-input {
        width: 100%;
        padding: 0.5rem;
        border: 1px solid #ddd;
        border-radius: 6px;
        font-size: 0.875rem;
        transition: border-color 0.2s;
    }

    .select-type:focus, .date-input:focus {
        border-color: #4a90e2;
        outline: none;
        box-shadow: 0 0 0 2px rgba(74, 144, 226, 0.1);
    }

    .add-row td {
        background: #fafafa;
    }

    .footer {
        display: flex;
        justify-content: flex-end;
        margin-top: 1.5rem;
        padding-top: 1rem;
        border-top: 1px solid #eee;
    }

    .btn-exit {
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.625rem 1.25rem;
        border: none;
        border-radius: 6px;
        background-color: #f5f5f5;
        color: #666;
        font-size: 0.875rem;
        font-weight: 500;
        cursor: pointer;
        transition: all 0.2s ease;
    }

    .btn-exit:hover {
        background-color: #e0e0e0;
        color: #333;
        transform: translateY(-1px);
    }

    .btn-exit:active {
        transform: translateY(0);
    }

    @media (max-width: 768px) {
        .events-container {
            padding: 0.5rem;
        }

        th, td {
            padding: 0.75rem 0.5rem;
        }
    }
</style>