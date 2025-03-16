<script lang="ts">
  import { goto } from "$app/navigation";
  import { getGlobal } from "../../biz/globalStore";
  import Avartar from "../../components/Avartar.svelte";
  import { hideModal, showModal } from "../../components/modalStore";
  import About from "../../components/About.svelte";
  import { settingPageInit } from "../../biz/operation";
    import { apiService } from "../../biz/apiService";
  let { children } = $props();
  let activeTab = $state("reservations"); // stations,reservations,visits, 或 system

  function goHome() {
    goto("/");
  }
  $effect(()=>{
    console.log(activeTab)
    switch(activeTab){
      case "reservations":
        goto("/settings/reservations");
        break;
      case "stations":
        goto("/settings/stations");
        break;
      case "others":
        goto("/settings/others");
        break;
    }
  })

  const init_page = async () => {
  await settingPageInit();
  console.log(getGlobal("user"));
    await new Promise((resolve) => setTimeout(resolve, 500));
  };

  // 添加退出登录函数
  async function logout() {
    await apiService.logout();
    goto("/auth/login");
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
    <div class="page-wrapper">
      <div class="page-header">
        <div class="header-left">
          <div class="return-button bg-[#fbc400]">
            <button aria-label="return" class="fill-white" title="return" onclick={() => goto("/")}>
              <svg class="menu-icon"  viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" width="200" height="200"><path d="M795.91835938 490.13457031c-49.66523437-60.0609375-111.53320313-102.77578125-179.10878907-123.73857422a439.74755859 439.74755859 0 0 0-102.54902344-19.37988281V162.25859375a30.79335937 30.79335937 0 0 0-11.46972656-27.40429688c-7.91015625-5.47910156-17.74160156-4.12382812-24.40898437 3.27832032L139.31914062 448.32324219a35.08769531 35.08769531 0 0 0-8.92792968 23.33496093c0 9.4359375 3.84257812 18.24960937 10.28320312 23.39121094l340.41972657 312.50742188a17.96748047 17.96748047 0 0 0 23.10820312 3.27832031 32.60039063 32.60039063 0 0 0 11.41347656-27.34804688v-174.3046875c128.03203125 4.63183594 213.74472656 49.77773437 273.80390625 130.0078125 28.81582031 36.66972656 49.94824219 81.36210937 61.47421875 130.29257813 2.71142578 13.78652344 12.825 23.50371094 24.40898438 23.39121094h2.48554687c12.20449219-1.29902344 21.58242187-13.73027344 21.75292969-28.75957032 1.35527344-159.33339844-33.22265625-284.65136719-103.62304688-374.26289062v0.22675781z" ></path></svg>
            </button>
          </div>
        </div>
        
        <div class="tabs">
          <button
            aria-label="预约数据"
            class:active={activeTab === "reservations"}
            onclick={() => (activeTab = "reservations")}
          >
            预约数据
          </button>
          {#if getGlobal("user")?.role?.toLowerCase() === "admin"}
          <button
            aria-label="工位数据"
            class:active={activeTab === "stations"}
            onclick={() => (activeTab = "stations")}
          >
            工位数据
          </button>
          {/if}
          <button
            aria-label="其他设置"
            class:active={activeTab === "others"}
            onclick={() => (activeTab = "others")}
          >
            系统设置
          </button>
        </div>
        
        <div class="header-right">
          <div class="dropdown-container tooltip-container">
            {#if localStorage.getItem("accessToken")}
              {@const user = getGlobal("user")}
              <button class="dropdown-trigger" onclick={() => goto("/auth/profile")} aria-label="menu">

                <Avartar username={user?.username ?? ""} />
              </button>
            {:else}
              <button class="dropdown-trigger"  aria-label="menu">
                <svg
                  class="logo"
                  style="fill: #fbc400;"
                  viewBox="0 0 1024 1024"
                  version="1.1"
                  xmlns="http://www.w3.org/2000/svg"
                  width="200"
                  height="200"
                  ><path
                    d="M341.333333 533.333333a128 128 0 0 1 128 128v149.333334a128 128 0 0 1-128 128H192a128 128 0 0 1-128-128v-149.333334a128 128 0 0 1 128-128h149.333333z m469.333334 0a128 128 0 0 1 128 128v149.333334a128 128 0 0 1-128 128h-149.333334a128 128 0 0 1-128-128v-149.333334a128 128 0 0 1 128-128h149.333334z m-469.333334 64H192a64 64 0 0 0-63.893333 60.245334L128 661.333333v149.333334a64 64 0 0 0 60.245333 63.893333L192 874.666667h149.333333a64 64 0 0 0 63.893334-60.245334L405.333333 810.666667v-149.333334a64 64 0 0 0-60.245333-63.893333L341.333333 597.333333z m469.333334 0h-149.333334a64 64 0 0 0-63.893333 60.245334L597.333333 661.333333v149.333334a64 64 0 0 0 60.245334 63.893333L661.333333 874.666667h149.333334a64 64 0 0 0 63.893333-60.245334L874.666667 810.666667v-149.333334a64 64 0 0 0-60.245334-63.893333L810.666667 597.333333zM341.333333 64a128 128 0 0 1 128 128v149.333333a128 128 0 0 1-128 128H192a128 128 0 0 1-128-128V192a128 128 0 0 1 128-128h149.333333z m469.333334 0a128 128 0 0 1 128 128v149.333333a128 128 0 0 1-128 128h-149.333334a128 128 0 0 1-128-128V192a128 128 0 0 1 128-128h149.333334zM341.333333 128H192a64 64 0 0 0-63.893333 60.245333L128 192v149.333333a64 64 0 0 0 60.245333 63.893334L192 405.333333h149.333333a64 64 0 0 0 63.893334-60.245333L405.333333 341.333333V192a64 64 0 0 0-60.245333-63.893333L341.333333 128z m469.333334 0h-149.333334a64 64 0 0 0-63.893333 60.245333L597.333333 192v149.333333a64 64 0 0 0 60.245334 63.893334L661.333333 405.333333h149.333334a64 64 0 0 0 63.893333-60.245333L874.666667 341.333333V192a64 64 0 0 0-60.245334-63.893333L810.666667 128z"
                    fill="#fbc400"
                  ></path></svg
                >
              </button>
            {/if}

            <div class="dropdown-menu">
              {#if !getGlobal("user")}
                <button class="dropdown-item" onclick={() => goto("/auth/login")}>
                  <svg class="menu-icon" viewBox="0 0 24 24"
                    ><path
                      d="M11 7L9.6 8.4l2.6 2.6H2v2h10.2l-2.6 2.6L11 17l5-5l-5-5zm9 12h-8v2h8c1.1 0 2-0.9 2-2V5c0-1.1-0.9-2-2-2h-8v2h8v14z"
                    /></svg
                  >
                  登录
                </button>
              {:else}
                <button
                  class="dropdown-item"
                  onclick={() => goto("/settings/reservations")}
                >
                  <svg class="menu-icon" viewBox="0 0 24 24"
                    ><path
                      d="M19.14,12.94c0.04-0.3,0.06-0.61,0.06-0.94c0-0.32-0.02-0.64-0.07-0.94l2.03-1.58c0.18-0.14,0.23-0.41,0.12-0.61 l-1.92-3.32c-0.12-0.22-0.37-0.29-0.59-0.22l-2.39,0.96c-0.5-0.38-1.03-0.7-1.62-0.94L14.4,2.81c-0.04-0.24-0.24-0.41-0.48-0.41 h-3.84c-0.24,0-0.43,0.17-0.47,0.41L9.25,5.35C8.66,5.59,8.12,5.92,7.63,6.29L5.24,5.33c-0.22-0.08-0.47,0-0.59,0.22L2.74,8.87 C2.62,9.08,2.66,9.34,2.86,9.48l2.03,1.58C4.84,11.36,4.8,11.69,4.8,12s0.02,0.64,0.07,0.94l-2.03,1.58 c-0.18,0.14-0.23,0.41-0.12,0.61l1.92,3.32c0.12,0.22,0.37,0.29,0.59,0.22l2.39-0.96c0.5,0.38,1.03,0.7,1.62,0.94l0.36,2.54 c0.05,0.24,0.24,0.41,0.48,0.41h3.84c0.24,0,0.44-0.17,0.47-0.41l0.36-2.54c0.59-0.24,1.13-0.56,1.62-0.94l2.39,0.96 c0.22,0.08,0.47,0,0.59-0.22l1.92-3.32c0.12-0.22,0.07-0.47-0.12-0.61L19.14,12.94z M12,15.6c-1.98,0-3.6-1.62-3.6-3.6 s1.62-3.6,3.6-3.6s3.6,1.62,3.6,3.6S13.98,15.6,12,15.6z"
                    /></svg
                  >
                  设置
                </button>
                {#if getGlobal("user")?.role.toLowerCase() === "admin"}
                <button class="dropdown-item" onclick={() => goto("/admin")}>
                  <svg  class="menu-icon" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" p-id="4568" width="200" height="200"><path d="M947.188 835.927l-50.079-35.055c-18.559-12.992-56.418-22.725-85.694-22.725H585.19v-79.844h288.761c2.2 0 4-1.8 4-4V156.702c0-2.2-1.8-4-4-4H176.663c-2.2 0-4 1.8-4 4v537.601c0 2.2 1.8 4 4 4h302.069v79.844H212.585c-29.276 0-67.134 9.734-85.694 22.725l-50.079 35.055c-26.411 18.488-17.538 35.371 22.786 35.371h824.803c40.324 0 49.198-16.883 22.787-35.371zM427.175 491.008a16.001 16.001 0 0 1-27.018 4.917l-66.792-76.29-30.074 34.372a16.002 16.002 0 0 1-12.042 5.464h-38.742c-8.836 0-16-7.164-16-16s7.164-16 16-16h31.481l37.331-42.666a16 16 0 0 1 24.08-0.004l61.215 69.92 60.521-161.261a16 16 0 0 1 30.347 1.168l41.855 144.388c0.045 0.156 0.088 0.313 0.128 0.47l17.727 68.934 53.023-135.33a16.001 16.001 0 0 1 14.896-10.163h0.1a16 16 0 0 1 14.87 10.347l35.146 93.072 36.371-46.095a16 16 0 0 1 12.561-6.089H784.8c8.837 0 16 7.164 16 16s-7.163 16-16 16h-52.885l-49.098 62.224a16 16 0 1 1-27.529-4.259l-30.451-80.64-55.913 142.705a16 16 0 0 1-30.393-1.852l-29.996-116.649-28.371-97.873-52.989 141.19z"></path></svg>系统
                </button>
                {/if}
                <button class="dropdown-item" onclick={async () => await logout()}>
                  <svg class="menu-icon" viewBox="0 0 24 24"
                    ><path
                      d="M17 7l-1.41 1.41L18.17 11H8v2h10.17l-2.58 2.58L17 17l5-5zM4 5h8V3H4c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h8v-2H4V5z"
                    /></svg
                  >
                  退出
                </button>
              {/if}
              <button
                class="dropdown-item"
                onclick={() => showModal(About, { onNegative: () => hideModal() })}
              >
                <svg class="menu-icon" viewBox="0 0 24 24"
                  ><path
                    d="M11 7h2v2h-2zm0 4h2v6h-2zm1-9C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8z"
                  /></svg
                >
                关于
              </button>
            </div>
          </div>
        </div>
      </div>
      <div class="content">
        {@render children()}
      </div>
    </div>
  {:catch e}
    <div>{"Error: " + e.message}</div>
  {/await}
</div>

<style>
  :global(html) {
    overflow: hidden;
  }
  .container {
    display: flex;
    align-items: center;
    flex-direction: column;
    height: 100vh;
    width: 100vw;
    max-width: 100%;
    margin: 0;
    padding: 1rem;
    box-sizing: border-box;
    overflow: hidden; /* 防止出滚动条 */
  }
  
  .page-wrapper {
    width: 100%;
    max-width: 1400px;
    margin: 0 auto;
    padding: 0 20px;
    box-sizing: border-box;
  }
  
  .page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 20px;
    width: 100%;
    padding: 10px 0;
    border-bottom: 1px solid #eee;
  }
  
  .header-left, .header-right {
    display: flex;
    align-items: center;
  }
  
  .return-button {
    width: 40px;
    height: 40px;
    box-shadow: 0 0 10px 0 rgba(0, 0, 0, 0.1);
    display: flex;
    align-items: center;
    justify-content: center;
    border-radius: 50%;
    margin-right: 15px;
  }
  
  .return-button:hover {
    box-shadow: 0 0 10px 0 rgba(0, 0, 0, 0.2);
    background-color: #666;
  }
  
  .content {
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    align-items: stretch;
    margin-top: 1rem;
  }
  
  .tabs {
    display: flex;
    gap: 10px;
  }

  .tabs button {
    padding: 8px 16px;
    border: none;
    background: #f1f1f1;
    cursor: pointer;
    font-size: 0.7rem;
    border-radius: 4px;
    transition: all 0.2s ease;
  }

  .tabs button.active {
    background: #fb9040;
    color: white;
    transform: translateY(-2px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }
  
  .tabs button:hover:not(.active) {
    background: #e5e5e5;
    transform: translateY(-1px);
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
  .menu-icon {
  width: 1.5rem;
  height: 1.5rem;
  fill: white;
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
    0% {
      transform: rotate(0deg);
    }
    100% {
      transform: rotate(360deg);
    }
  }

  @keyframes pulse {
    0%,
    100% {
      opacity: 0.6;
    }
    50% {
      opacity: 1;
    }
  }
  .dropdown-container {
    position: relative;
  }

  .dropdown-trigger {
    background: none;
    border: none;
    padding: 8px;
    cursor: pointer;
    border-radius: 8px;
    transition: all 0.3s ease;
  }

  .dropdown-trigger:hover {
    background-color: rgba(251, 196, 0, 0.1);
    transform: translateY(-2px);
  }

  .dropdown-menu {
    position: absolute;
    top: 100%;
    right: 0;
    background: white;
    border-radius: 8px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
    padding: 8px;
    min-width: 8rem;
    opacity: 0;
    visibility: hidden;
    transform: translateY(10px);
    transition: all 0.3s ease;
    z-index: 50;
  }

  .dropdown-container:hover .dropdown-menu {
    opacity: 1;
    visibility: visible;
    transform: translateY(0);
  }

  .dropdown-item {
    display: flex;
    align-items: center;
    gap: 8px;
    width: 100%;
    padding: 8px 16px;
    border: none;
    background: none;
    color: #333;
    cursor: pointer;
    transition: all 0.2s ease;
    border-radius: 4px;
    text-align: left;
  }

  .dropdown-item:hover {
    background-color: rgba(251, 196, 0, 0.1);
  }

  

  .dropdown-item:hover .menu-icon {
    fill: #fbc400;
  }
</style>
