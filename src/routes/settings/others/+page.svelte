<script lang="ts">
    import About from "../../../components/About.svelte";
    import { hideModal, showModal } from "../../../components/modalStore";
    import { apiService } from "../../../biz/apiService";
    import { goto } from "$app/navigation";
  
    let remote_source = $state("");
    let store: any = $state(null);
    
  
    const loadLoadSetting = async () => {
      return undefined;
    };
    const showAbout = () => {
      console.log("about");
      showModal(About, { onNegative: () => hideModal() });
    };
  </script>
  
  <div class="content">
    <div class="settings-section">
      {#await loadLoadSetting()}
        <p>加载中...</p>
      {:then _}
        <h4>其他设置</h4>
        <div class="settings-card">
          <h4 class="settings-title">日志</h4>
          <div
            class="settings-grid"
            style="justify-content: space-between;align-items: center;"
          >
            <button class="about-btn" onclick={()=>goto("/admin/logs")}>查看日志</button>
          </div>
        </div>
        <div class="settings-card">
          <h4 class="settings-title">用户管理</h4>
          <div
            class="settings-grid"
            style="justify-content: space-between;align-items: center;"
          >
            <button class="about-btn" onclick={async()=>{
              goto("/auth/users");
            }}>用户管理</button>
          </div>
        </div>
        
        <div class="settings-card">
          <h4 class="settings-title">关于</h4>
          <div
            class="settings-grid"
            style="justify-content: space-between;align-items: center;"
          >
            <p class="settings-description">版本号：Engineer v20250228</p>
            <button class="about-btn" onclick={showAbout}>Details</button>
          </div>
        </div>
      {/await}
    </div>
  </div>
  
  <style>
    .content {
      padding: 20px;
      height: 100%;
    }
  
    .settings-section {
      max-height: calc(100vh - 150px); /* 设置最大高度，留出上下边距 */
      overflow-y: auto; /* 添加垂直滚动条 */
      padding-right: 10px; /* 为滚动条留出空间 */
    }
  
    /* 自定义滚动条样式 */
    .settings-section::-webkit-scrollbar {
      width: 8px;
    }
  
    .settings-section::-webkit-scrollbar-track {
      background: #f1f1f1;
      border-radius: 4px;
    }
  
    .settings-section::-webkit-scrollbar-thumb {
      background: #fb9040;
      border-radius: 4px;
    }
  
    .settings-section::-webkit-scrollbar-thumb:hover {
      background: #e3b300;
    }
  
    .settings-card {
      background: white;
      border-radius: 8px;
      padding: 20px;
      margin-bottom: 20px;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }
  
    .settings-title {
      color: #333;
      font-size: 1.1rem;
      margin: 0 0 20px 0;
      padding-bottom: 12px;
      border-bottom: 1px solid #eee;
    }
  
    .settings-grid {
      display: grid;
      gap: 24px;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
    }
  
    .setting-item {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }
  
    .setting-item label {
      font-size: 0.9rem;
      color: #666;
      font-weight: 500;
    }
  
    .input-wrapper {
      display: flex;
      align-items: center;
      gap: 8px;
      background: #f5f7fa;
      padding: 8px 12px;
      border-radius: 6px;
      border: 1px solid #e0e4e8;
    }
  
    .input-wrapper.low {
      border-left: 4px solid #4caf50;
    }
    .input-wrapper.medium {
      border-left: 4px solid #ffc107;
    }
    .input-wrapper.high {
      border-left: 4px solid #f44336;
    }
  
    .number-input {
      width: 60px;
      border: none;
      background: transparent;
      font-size: 1rem;
      color: #333;
      text-align: center;
      -moz-appearance: textfield;
    }
  
    .number-input::-webkit-outer-spin-button,
    .number-input::-webkit-inner-spin-button {
      -webkit-appearance: none;
      margin: 0;
    }
  
    .unit {
      color: #666;
      font-size: 0.9rem;
    }
  
    .settings-actions {
      margin-top: 24px;
      display: flex;
      justify-content: flex-end;
    }
  
    .action-button {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 8px 16px;
      border: none;
      border-radius: 6px;
      cursor: pointer;
      font-size: 14px;
      font-weight: 500;
      transition: all 0.2s ease;
    }
  
    .action-button:hover {
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }
  
    .save-button {
      background: #4a90e2;
      color: white;
      border: none;
      padding: 10px 24px;
      border-radius: 6px;
      cursor: pointer;
      font-weight: 500;
      transition: background 0.2s;
    }
  
    .save-button:hover {
      background: #357abd;
    }
  
    .about-btn {
      padding: 6px 16px;
      font-size: 14px;
      border: 1px solid #dcdfe6;
      border-radius: 4px;
      background-color: white;
      color: #606266;
      cursor: pointer;
      transition: all 0.3s ease;
    }
  
    .about-btn:hover {
      color: #409eff;
      border-color: #c6e2ff;
      background-color: #ecf5ff;
    }
  
    .about-btn:active {
      border-color: #409eff;
    }
  </style>
  