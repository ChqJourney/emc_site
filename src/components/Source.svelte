<script lang="ts">
    import { Store } from "@tauri-apps/plugin-store";
    import { getGlobal,setGlobal } from "../biz/globalStore";
    import { exists } from "@tauri-apps/plugin-fs";
    import { message,open } from "@tauri-apps/plugin-dialog";
    async function handleChangeDbPath(){
  const folder=await open({
    title: "选择远程数据源",
    directory: true,
    multiple: false,
  })
  if(folder){
    let source_valid=false;
    if(folder){
      const dbConnected=await exists(`${folder}\\data\\data.db`);
      const settingConnected=await exists(`${folder}\\settings.json`);
      console.log(dbConnected,settingConnected);
      if(dbConnected&&settingConnected){
        source_valid=true;  
      }else{
        source_valid=false;
      }
      if(!source_valid){
        await message("远程数据源不可用");
        return;
      }
      const store=await Store.load("settings.json");
      
      await store.set("remote_source",folder);
      setGlobal("remote_source",folder);
      init();
      await message("数据库位置已修改，请保存后，重启软件生效");
    }
  }
 }
    const init = async () => {
        const store = await Store.load("settings.json");
        return await store.get<string>("remote_source");
    };
</script>
{#await init()}
    <div class="loading-container">
        <div class="loading-spinner"></div>
        <span>加载中...</span>
    </div>
{:then remote_source}
    <div class="modal-content">
        <h2>数据源设置</h2>
        <div class="source-card">
            <div class="source-info">
                <span class="label">当前数据源位置</span>
                <p class="path">{remote_source}</p>
            </div>
            <button class="change-button" on:click={handleChangeDbPath}>
                <svg class="icon" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" width="16" height="16">
                    <path d="M369.777778 455.111111h284.444444c17.066667 0 28.444444 11.377778 28.444445 28.444445s-11.377778 28.444444-28.444445 28.444444h-284.444444c-17.066667 0-28.444444-11.377778-28.444445-28.444444s11.377778-28.444444 28.444445-28.444445z"></path>
                    <path d="M56.888889 483.555556C56.888889 625.777778 170.666667 739.555556 312.888889 739.555556H398.222222v-56.888889H312.888889C204.8 682.666667 113.777778 591.644444 113.777778 483.555556S204.8 284.444444 312.888889 284.444444H398.222222V227.555556H312.888889C170.666667 227.555556 56.888889 341.333333 56.888889 483.555556zM711.111111 227.555556H625.777778v56.888888h85.333333C819.2 284.444444 910.222222 375.466667 910.222222 483.555556S819.2 682.666667 711.111111 682.666667H625.777778v56.888889h85.333333C853.333333 739.555556 967.111111 625.777778 967.111111 483.555556S853.333333 227.555556 711.111111 227.555556z"></path>
                </svg>
                修改位置
            </button>
        </div>
    </div>
{:catch error}
    <div class="error-message">
        加载数据源信息失败: {error.message}
    </div>
{/await}

<style>
    .modal-content {
        padding: 2rem;
        max-width: 600px;
        margin: 0 auto;
    }

    h2 {
        color: #333;
        font-size: 1.5rem;
        font-weight: 500;
        margin-bottom: 2rem;
        text-align: center;
    }

    .source-card {
        background: #fff;
        border-radius: 8px;
        padding: 1.5rem;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
        border: 1px solid #eee;
    }

    .source-info {
        margin-bottom: 1.5rem;
    }

    .label {
        color: #666;
        font-size: 0.9rem;
        display: block;
        margin-bottom: 0.5rem;
    }

    .path {
        color: #333;
        background: #f8f9fa;
        padding: 0.75rem;
        border-radius: 4px;
        font-family: monospace;
        word-break: break-all;
        margin: 0;
        border: 1px solid #e9ecef;
    }

    .change-button {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 0.5rem;
        background: #fbc400;
        color: #fff;
        border: none;
        padding: 0.75rem 1.5rem;
        border-radius: 4px;
        cursor: pointer;
        font-size: 0.9rem;
        transition: background-color 0.2s;
        width: 100%;
    }

    .change-button:hover {
        background: #e5b300;
    }

    .icon {
        fill: currentColor;
    }

    .loading-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: 2rem;
        color: #666;
    }

    .loading-spinner {
        border: 3px solid #f3f3f3;
        border-top: 3px solid #fbc400;
        border-radius: 50%;
        width: 24px;
        height: 24px;
        animation: spin 1s linear infinite;
        margin-bottom: 1rem;
    }

    @keyframes spin {
        0% { transform: rotate(0deg); }
        100% { transform: rotate(360deg); }
    }

    .error-message {
        color: #dc3545;
        padding: 1rem;
        background: #fff;
        border-radius: 4px;
        margin: 1rem;
        text-align: center;
    }
</style>