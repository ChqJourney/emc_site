<script lang="ts">
    import { goto } from "$app/navigation";
      import { getGlobal } from "../../biz/globalStore";
    let {children}=$props();
    let activeTab = $state("reservations"); // stations,reservations,visits, 或 system
  
    function goHome() {
      goto('/');
    }
  
  const init_page=async()=>{
    const user=getGlobal("user");
    const tests=getGlobal("tests");
    const project_engineers=getGlobal("project_engineers");
    const test_engineers=getGlobal("testing_engineers");
    // if(!user||!tests||!project_engineers||!test_engineers){
    //   await init();
    // }
    await new Promise(resolve => setTimeout(resolve, 500));
  }
  </script>
  
  <div class="container">
    {#await init_page()}
    <div class="loading-container">
      <div class="loading-spinner-wrapper">
        <div class="loading-spinner"></div>
      </div>
      <span class="loading-text">加载中...</span>
    </div>
    {:then _} 
      
   
    <div class="page-header">
      <div class="tabs">
        
        <button 
          aria-label="预约数据"
          class:active={activeTab === "reservations"} 
          onclick={() => activeTab = "reservations"}
        >
          预约数据
        </button>
        <button 
          aria-label="其他设置"
          class:active={activeTab === "others"} 
          onclick={() => activeTab = "others"}
        >
          系统设置
        </button>
  
      </div>
      <button aria-label="返回首页" class="home-button" onclick={goHome}>
        <svg viewBox="0 0 1024 1024" width="30" height="30">
          <path d="M946.5 505L560.1 118.8l-25.9-25.9c-12.3-12.2-32.1-12.2-44.4 0L77.5 505c-12.3 12.3-18.9 28.6-18.8 46 0.4 35.2 29.7 63.3 64.9 63.3h42.5V940h691.8V614.3h43.4c17.1 0 33.2-6.7 45.3-18.8 12.1-12.1 18.7-28.2 18.7-45.3 0-17-6.7-33.1-18.8-45.2zM568 868H456V664h112v204zm217.9-325.7V868H632V640c0-22.1-17.9-40-40-40H432c-22.1 0-40 17.9-40 40v228H238.1V542.3h-96l370-369.7 23.1 23.1L882 542.3h-96.1z" fill="currentColor"/>
        </svg>
      </button>
    </div>
  
    {@render children()}
    {:catch e}
    <div>{"Error: " + e.message}</div> 
    {/await}
  </div>
  
  
  <style>
    .container {
      padding: 20px;
    }
    .tabs {
      display: flex;
      gap: 10px;
      margin-bottom: 20px;
    }
  
    .tabs button {
      padding: 10px 20px;
      border: none;
      background: #f1f1f1;
      cursor: pointer;
    }
  
    .tabs button.active {
      background: #fb9040;
      color: white;
    }
  
    .page-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
  
    .home-button {
      background: transparent;
      border: none;
      cursor: pointer;
      padding: 12px;
      color: #fb9040;
      display: flex;
      align-items: center;
      justify-content: center;
      border-radius: 4px;
      transition: background-color 0.2s;
    }
  
    .home-button:hover {
      background: rgba(74, 144, 226, 0.1);
    }
    .loading-container {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      background-color: rgba(255, 255, 255, 0.9);
      z-index: 1000;
    }
  
    .loading-spinner-wrapper {
      width: 80px;
      height: 80px;
      margin-bottom: 20px;
    }
  
    .loading-spinner {
      width: 100%;
      height: 100%;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #fb9040;
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }
  
    .loading-text {
      color: #666;
      font-size: 1.2rem;
      font-weight: 500;
      margin-top: 10px;
      animation: pulse 1.5s ease-in-out infinite;
    }
  
    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }
  
    @keyframes pulse {
      0%, 100% { opacity: 0.6; }
      50% { opacity: 1; }
    }
  </style>
  