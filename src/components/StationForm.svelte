<script lang="ts">
        import type { Station } from "../biz/types";
        import { hideModal, showModal } from "./modalStore";
    import SeventTab from "./Sevent.svelte";

    let {label,submitHandler,onNegative,station}:{label:string,submitHandler:(station:any,isCreate:boolean)=>void,onNegative:()=>void,station:Station|undefined}= $props();
    let isCreate=station===undefined||station.id===undefined||station.id===0
    console.log(isCreate)
    let currentStation =$state(station?? {id:0,name:"",short_name:"",description:"",photo_path:"",status:"in_service",sequence_no:0,created_On:new Date(),updated_On:new Date()});
    let activeTab = $state("basic");
    

    

    const handleSubmit = async () => {
        submitHandler(currentStation as Station,isCreate);
        hideModal();
    };

    
</script>

<h2>{label}:{currentStation.name}</h2>

<div class="tab-container">
    <div class="tabs">
        <button class="tab-button" class:active={activeTab === "basic"} onclick={() => activeTab = "basic"}>
            <span>基本信息</span>
        </button>
        {#if !isCreate}
        <button class="tab-button" class:active={activeTab === "events"} onclick={() => activeTab = "events"}>
            <span>工位事件</span>
        </button>
        {/if}
    </div>
</div>

<div class="tab-content">
    {#if activeTab === "basic"}
        <form onsubmit={handleSubmit}>
            <div class="form-group">
                <label for="name">名称</label>
                <input type="text" bind:value={currentStation.name} required />
            </div>
            <div class="form-group">
                <label for="short_name">简称</label>
                <input type="text" bind:value={currentStation.short_name} required />
            </div>
            <div class="form-group">
                <label for="description">描述</label>
                <textarea bind:value={currentStation.description} rows="3"></textarea>
            </div>
            <div class="form-group">
                <div style="display: flex; justify-content: space-between;">
                    <label for="photo_path">图片路径</label>
                    <span style="color: #999;font-size: smaller;">如果在数据源中station_pics文件夹中，输入文件名即可</span>
                </div>
                <input type="text" bind:value={currentStation.photo_path} />
            </div>
            
            <div class="button-group">
                <button type="button" class="cancel" onclick={onNegative}>取消</button>
                <button type="submit" class="submit">确定</button>
            </div>
        </form>
    {:else if activeTab === "events"&&!isCreate}
        <SeventTab {currentStation}/>
    {/if}
</div>

<style>
    .tab-container {
        margin: 20px 0;
        border-bottom: 1px solid #e0e0e0;
    }

    .tabs {
        display: flex;
        gap: 4px;
        margin-bottom: -1px;
    }
    .tab-content{
        padding: 20px;
        min-width: 500px;
    }
    .tab-button {
        padding: 12px 24px;
        background: transparent;
        border: none;
        border-bottom: 2px solid transparent;
        color: #666;
        cursor: pointer;
        font-size: 14px;
        transition: all 0.3s ease;
        position: relative;
    }

    .tab-button:hover {
        color: #1a73e8;
    }

    .tab-button.active {
        color: #1a73e8;
        border-bottom: 2px solid #1a73e8;
        font-weight: 500;
    }

    .tab-button span {
        position: relative;
        z-index: 1;
    }

    .form-group {
        margin-bottom: 15px;
    }

    .form-group label {
        display: block;
        margin-bottom: 5px;
    }

    .form-group input,
    .form-group select,
    .form-group textarea {
        width: 100%;
        padding: 8px;
        border: 1px solid #ddd;
        border-radius: 4px;
    }

    .button-group {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
        margin-top: 20px;
    }

    .button-group button {
        padding: 8px 16px;
        border: none;
        border-radius: 4px;
        cursor: pointer;
    }

    .cancel {
        background: #f1f1f1;
    }

    .submit {
        background: #4a90e2;
        color: white;
    }
</style>