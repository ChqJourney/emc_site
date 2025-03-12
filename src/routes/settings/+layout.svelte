<script lang="ts">
  import { goto } from "$app/navigation";
  import { getGlobal } from "../../biz/globalStore";
  import Avartar from "../../components/Avartar.svelte";
  import { hideModal, showModal } from "../../components/modalStore";
  import About from "../../components/About.svelte";
  import { settingPageInit } from "../../biz/operation";
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
    await new Promise((resolve) => setTimeout(resolve, 500));
  };

  // 添加退出登录函数
  async function logout() {
    localStorage.removeItem("accessToken");
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
        <div class="tabs">
          <button
            aria-label="预约数据"
            class:active={activeTab === "reservations"}
            onclick={() => (activeTab = "reservations")}
          >
            预约数据
          </button>
          <button
            aria-label="工位数据"
            class:active={activeTab === "stations"}
            onclick={() => (activeTab = "stations")}
          >
            工位数据
          </button>
          <button
            aria-label="其他设置"
            class:active={activeTab === "others"}
            onclick={() => (activeTab = "others")}
          >
            系统设置
          </button>
        </div>
        <div class="dropdown-container tooltip-container">
          {#if localStorage.getItem("accessToken")}
            {@const user = getGlobal("user")}
            <Avartar username={user?.username ?? ""} />
          {:else}
            <button class="dropdown-trigger" aria-label="menu">
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
  
  .content {
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    align-items: stretch;
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
    width: 100%;
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

  .menu-icon {
    width: 20px;
    height: 20px;
    fill: #666;
  }

  .dropdown-item:hover .menu-icon {
    fill: #fbc400;
  }
</style>
