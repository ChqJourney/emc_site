<script lang="ts">
  import { goto } from "$app/navigation";
  import type { Reservation, User } from "../biz/types";
  import { onMount } from "svelte";
  import { calendar } from "../biz/calendar";
  import { getGlobal, setGlobal } from "../biz/globalStore";
  import { hideModal, showModal } from "../components/modalStore";
  import { errorHandler } from "../biz/errorHandler";
  import About from "../components/About.svelte";
  import type { AppError } from "../biz/errors";
  import { apiService } from "../biz/apiService";
  import Avartar from "../components/Avartar.svelte";
  import { calendarPageInit } from "../biz/operation";
  import Avatar from '../components/Avartar.svelte';
  // 使用calendar实例的store
  const currentMonth = calendar.currentMonth;
  // 移动设备菜单状态
  let isMenuOpen = false;
  let touchStartX = 0;
  let touchStartY = 0;

  // 处理移动设备上的菜单点击
  function toggleMenu(event: MouseEvent) {
    if (window.innerWidth <= 768) {
      event.preventDefault();
      event.stopPropagation();
      isMenuOpen = !isMenuOpen;
    }
  }

  // 处理移动设备上的触摸滑动
  function handleTouchStart(event: Event) {
    const touchEvent = event as TouchEvent;
    touchStartX = touchEvent.touches[0].clientX;
    touchStartY = touchEvent.touches[0].clientY;
  }

  function handleTouchEnd(event: Event) {
    const touchEvent = event as TouchEvent;
    const touchEndX = touchEvent.changedTouches[0].clientX;
    const touchEndY = touchEvent.changedTouches[0].clientY;

    const deltaX = touchEndX - touchStartX;
    const deltaY = touchEndY - touchStartY;

    // 确保是水平滑动而不是垂直滑动
    if (Math.abs(deltaX) > Math.abs(deltaY) && Math.abs(deltaX) > 50) {
      if (deltaX > 0) {
        // 向右滑动，显示上个月
        calendar.changeMonth(-1);
      } else {
        // 向左滑动，显示下个月
        calendar.changeMonth(1);
      }
    }
  }

  // 点击页面其他地方关闭菜单
  function handleClickOutside(event: MouseEvent) {
    if (isMenuOpen && window.innerWidth <= 768) {
      const target = event.target as HTMLElement;
      if (!target.closest(".dropdown-container")) {
        isMenuOpen = false;
      }
    }
  }

  // 加载月度预约数据
  async function loadMonthData(): Promise<Reservation[]> {
    try {
      const year = parseInt($currentMonth.split("-")[0]);
      const month = parseInt($currentMonth.split("-")[1]);
      const daysInMonth = new Date(year, month, 0).getDate();

      // 获取整月的预约数据
      // monthlyReservations = [];
      const data = await apiService.Get(`/reservations/month/${$currentMonth}`);
      return data;
    } catch (e) {
      errorHandler.handleError(e as AppError);
      return [];
    }
  }

  // 处理日期点击
  function handleDateClick(date: string) {
    console.log(date);
    goto(`/date?date=${date}`);
  }

  // 计算日历数据

  const daysInMonth = $derived(calendar.getDaysInMonth($currentMonth));
  const firstDayOfMonth = $derived(calendar.getFirstDayOfMonth($currentMonth));
  const calendarDays = $derived(calendar.getCalendarDays($currentMonth));
  const monthDisplay = $derived(calendar.getMonthDisplay($currentMonth));

  const init_page = async () => {
    await calendarPageInit();
  };
  // Add keyboard event listener for month navigation
  const handleKeydown = (event: KeyboardEvent) => {
    if (event.key === "ArrowLeft") {
      calendar.changeMonth(-1);
    } else if (event.key === "ArrowRight") {
      calendar.changeMonth(1);
    }
  };
  async function logout() {
    await apiService.logout();
    goto("/auth/login");
  }
  onMount(() => {
    window.addEventListener("keydown", handleKeydown);
    window.addEventListener("click", handleClickOutside);

    // 添加触摸事件监听器
    const calendarElement = document.querySelector(".calendar");
    if (calendarElement) {
      calendarElement.addEventListener(
        "touchstart",
        handleTouchStart as EventListener,
      );
      calendarElement.addEventListener(
        "touchend",
        handleTouchEnd as EventListener,
      );
    }

    return () => {
      window.removeEventListener("keydown", handleKeydown);
      window.removeEventListener("click", handleClickOutside);
      if (calendarElement) {
        calendarElement.removeEventListener(
          "touchstart",
          handleTouchStart as EventListener,
        );
        calendarElement.removeEventListener(
          "touchend",
          handleTouchEnd as EventListener,
        );
      }
    };
  });
</script>

<div class="page-container">
  {#await init_page()}
    <div class="loading-container">
      <div class="loading-spinner"></div>
      <span class="loading-text">加载中...</span>
    </div>
  {:then _}
    <div class="fixed-header">
      <button
        aria-label="today"
        class="tooltip-container"
        onclick={() => handleDateClick(new Date().toISOString().split("T")[0])}
      >
        <span class="tooltip">返回今天</span>
        <svg
          class="logo"
          style="fill: #fbc400;"
          viewBox="0 0 1024 1024"
          version="1.1"
          xmlns="http://www.w3.org/2000/svg"
          width="200"
          height="200"
          ><path
            d="M810.653961 127.994116l-42.664705 0L767.989255 42.664705l-85.329411 0 0 85.329411L341.340155 127.994116 341.340155 42.664705l-85.329411 0 0 85.329411-42.664705 0c-47.14474 0-84.902692 38.184671-84.902692 85.329411l-0.426719 597.307921c0 47.14474 38.184671 85.329411 85.329411 85.329411l597.307921 0c47.14474 0 85.329411-38.184671 85.329411-85.329411L895.983371 213.32455C895.983371 166.17981 857.7987 127.994116 810.653961 127.994116zM810.653961 810.632471 213.346039 810.632471 213.346039 341.318666l597.307921 0L810.653961 810.632471zM298.67545 426.6491l213.32455 0 0 213.32455L298.67545 639.97365 298.67545 426.6491z"
          ></path></svg
        >
      </button>
      <!-- <img class="brand" src="/intertek.png" alt="Intertek" /> -->
      <div class="month-nav">
        <button
          class="tooltip-container"
          aria-label="previous_month"
          onclick={() => calendar.changeMonth(-1)}
        >
          <span class="tooltip">上个月</span>
          <svg
            class="logo"
            style="fill: #fbc400;"
            viewBox="0 0 1024 1024"
            version="1.1"
            xmlns="http://www.w3.org/2000/svg"
            width="200"
            height="200"
            ><path
              d="M629.291 840.832l60.331-60.331-268.501-268.501 268.501-268.501-60.331-60.331-328.832 328.832z"
            ></path></svg
          >
        </button>
        <input
          type="month"
          class="month-input"
          bind:value={$currentMonth}
          onchange={() => loadMonthData()}
        />
        <button
          class="tooltip-container"
          aria-label="next_month"
          onclick={() => calendar.changeMonth(1)}
        >
          <span class="tooltip">下个月</span>
          <svg
            class="logo"
            style="fill: #fbc400;"
            viewBox="0 0 1024 1024"
            version="1.1"
            xmlns="http://www.w3.org/2000/svg"
            width="200"
            height="200"
            ><path
              d="M689.621 512l-328.832-328.832-60.331 60.331 268.501 268.501-268.501 268.501 60.331 60.331z"
            ></path></svg
          >
        </button>
      </div>
      <div
        class="dropdown-container"
        class:active={isMenuOpen}
        onclick={toggleMenu}
      >
        {#if localStorage.getItem("accessToken")}
          {@const user = getGlobal("user")}
          <button
            class="dropdown-trigger"
            onclick={() => goto("/auth/profile")}
            aria-label="profile"
          >
            <Avartar username={user?.username ?? ""} />
          </button>
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
            {#if getGlobal("user")?.role.toLowerCase() === "admin"}
            <button class="dropdown-item" onclick={() => goto("/admin")}>
              <svg  class="menu-icon" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" p-id="4568" width="200" height="200"><path d="M947.188 835.927l-50.079-35.055c-18.559-12.992-56.418-22.725-85.694-22.725H585.19v-79.844h288.761c2.2 0 4-1.8 4-4V156.702c0-2.2-1.8-4-4-4H176.663c-2.2 0-4 1.8-4 4v537.601c0 2.2 1.8 4 4 4h302.069v79.844H212.585c-29.276 0-67.134 9.734-85.694 22.725l-50.079 35.055c-26.411 18.488-17.538 35.371 22.786 35.371h824.803c40.324 0 49.198-16.883 22.787-35.371zM427.175 491.008a16.001 16.001 0 0 1-27.018 4.917l-66.792-76.29-30.074 34.372a16.002 16.002 0 0 1-12.042 5.464h-38.742c-8.836 0-16-7.164-16-16s7.164-16 16-16h31.481l37.331-42.666a16 16 0 0 1 24.08-0.004l61.215 69.92 60.521-161.261a16 16 0 0 1 30.347 1.168l41.855 144.388c0.045 0.156 0.088 0.313 0.128 0.47l17.727 68.934 53.023-135.33a16.001 16.001 0 0 1 14.896-10.163h0.1a16 16 0 0 1 14.87 10.347l35.146 93.072 36.371-46.095a16 16 0 0 1 12.561-6.089H784.8c8.837 0 16 7.164 16 16s-7.163 16-16 16h-52.885l-49.098 62.224a16 16 0 1 1-27.529-4.259l-30.451-80.64-55.913 142.705a16 16 0 0 1-30.393-1.852l-29.996-116.649-28.371-97.873-52.989 141.19z"></path></svg>
              系统
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

    <div class="calendar-container">
      {#await loadMonthData()}
        <div class="loading-container">
          <div class="loading-spinner"></div>
          <span class="loading-text">加载中...</span>
        </div>
      {:then monthlyReservations}
        <img src="intertek.png" alt="bg" />
        <div class="calendar">
          <div class="weekdays">
            <div style="text-align: center;">日</div>
            <div style="text-align: center;">一</div>
            <div style="text-align: center;">二</div>
            <div style="text-align: center;">三</div>
            <div style="text-align: center;">四</div>
            <div style="text-align: center;">五</div>
            <div style="text-align: center;">六</div>
          </div>

          <div class="days">
            {#each Array(firstDayOfMonth) as _, index}
              {@const prevMonthDay =
                calendar.getPreviousMonthDays($currentMonth)[index]}
              <div class="day empty other-month">{prevMonthDay}</div>
            {/each}

            {#each calendarDays as day}
              {@const date = `${$currentMonth}-${day.toString().padStart(2, "0")}`}
              <!-- svelte-ignore a11y_click_events_have_key_events -->
              <!-- svelte-ignore a11y_no_static_element_interactions -->
              <div
                class="tooltip-container day {calendar.isToday(date)
                  ? 'today'
                  : ''}"
                title={`Go to ${date}`}
                style="background-color: {calendar.getDayColor(
                  date,
                  monthlyReservations,
                )}"
                onclick={() => handleDateClick(date)}
              >
                <div class="tooltip">
                  {`${calendar.getDayReservationCount(date, monthlyReservations)}个预约`}
                </div>
                {day}
              </div>
            {/each}

            {#each calendar.getNextMonthDays($currentMonth) as day}
              <div class="day empty other-month">{day}</div>
            {/each}
          </div>
        </div>
        <div class="calendar-footer">
          <div class="calendar-footer-item">
            <div class="footer-content">
              <div class="footer-left">
                {#if getGlobal("user")}
                  <Avatar username={getGlobal("user")?.username ?? ""} />
                  <span class="footer-text">已登录</span>
                {:else}
                  <Avatar username="ME" />
                  <span class="footer-text">游客</span>
                {/if}
              </div>
              <div class="footer-right">
                <span class="version-text">v20250316</span>
              </div>
            </div>
          </div>
        </div>
      {:catch error}
        <div class="error-container">
          <div class="error-icon">❌</div>
          <div class="error-content">
            <h3 class="error-title">抱歉，服务器当前不可用</h3>
            <p class="error-message">{error.message}</p>
          </div>
          <button class="retry-button" onclick={() => window.location.reload()}>
            重新加载
          </button>
        </div>
      {/await}
    </div>
  {:catch error}
    <div class="error-container">
      <div class="error-icon">❌</div>
      <div class="error-content">
        <h3 class="error-title">抱歉，服务器当前不可用</h3>
        <p class="error-message">{error.message + " " + error.details}</p>
      </div>
      <button class="retry-button" onclick={() => window.location.reload()}>
        重新加载
      </button>
    </div>
  {/await}
</div>

<style>
  /* 基础样式 - 适用于所有屏幕尺寸 */
  .month-input::-webkit-calendar-picker-indicator {
    filter: invert(70%) sepia(70%) saturate(1000%) hue-rotate(360deg);
    cursor: pointer;
    width: 1.5rem;
    height: 1.5rem;
    padding: 0px;
  }
  .calendar-footer {
    margin-top: auto;
    width: 100%;
    background: linear-gradient(to bottom, transparent, rgba(248, 249, 250, 0.95));
    padding: 1rem;
    position: fixed;
    bottom: 0;
    left: 0;
    right: 0;
    z-index: 10;
  }

  .calendar-footer-item {
    max-width: 800px;
    margin: 0 auto;
    padding: 0 1rem;
  }

  .footer-content {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 0.5rem 1rem;
    border-radius: 12px;
    background-color: rgba(255, 255, 255, 0.8);
    backdrop-filter: blur(8px);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  }

  .footer-left {
    display: flex;
    align-items: center;
    gap: 0.8rem;
  }

  .footer-text {
    color: #666;
    font-size: 0.9rem;
  }

  .footer-right {
    display: flex;
    align-items: center;
  }

  .version-text {
    color: #94a3b8;
    font-size: 0.8rem;
  }

  @media (min-width: 768px) {
    .calendar-footer {
      padding: 1.5rem;
    }

    .footer-content {
      padding: 0.75rem 1.5rem;
    }

    .footer-text {
      font-size: 1rem;
    }

    .version-text {
      font-size: 0.9rem;
    }
  }

  .page-container {
    display: flex;
    flex-direction: column;
    height: 100vh;
    width: 100%;
    overflow: hidden;
  }

  .fixed-header {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    background-color: #f8f9fa;
    z-index: 1000;
    padding: 1.2rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  }

  .fixed-header::before {
    content: "";
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    height: 72px;
    background-color: #f8f9fa;
    z-index: -1;
  }

  .fixed-header button {
    background-color: transparent;
    border: none;
    outline: none;
    padding: 8px;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.3s ease;
    display: flex;
    align-items: center;
    justify-content: center;
  }

  .fixed-header button:hover {
    background-color: rgba(251, 196, 0, 0.1);
    transform: translateY(-2px);
    box-shadow: 0 2px 8px rgba(251, 196, 0, 0.2);
  }

  .fixed-header button:active {
    transform: translateY(0);
    background-color: rgba(251, 196, 0, 0.15);
    box-shadow: 0 1px 2px rgba(251, 196, 0, 0.1);
  }

  .fixed-header button:focus {
    outline: 2px solid rgba(251, 196, 0, 0.3);
    outline-offset: 2px;
  }

  .logo {
    width: 1.5rem;
    height: 1.5rem;
  }

  .month-nav {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 24px;
    flex: 1;
    max-width: 400px;
    margin: 0 auto;
  }

  /* 小屏幕样式 - 默认样式 */
  .month-nav button {
    background: none;
    border: 1px solid #e0e0e0;
    padding: 1px 1px;
    border-radius: 3px;
    color: #666;
    transition: all 0.3s ease;
  }

  .month-nav input {
    padding: 6px 10px;
    border: 1px solid #e0e0e0;
    border-radius: 6px;
    color: #94a3b8;
    font-size: 1rem;
    min-width: 150px;
    font-family: Arial, Helvetica, sans-serif;
    height: 2rem;
    transition: all 0.3s ease;
    flex: 1;
    min-width: 150px;
    max-width: 200px;
  }
  @media (min-width: 480px) {
    .calendar{
      overflow: auto;
    }
  }
  /* 大屏幕样式 (min-width: 768px) */
  @media (min-width: 768px) {
    .calendar{
      overflow: auto;
    }
    .fixed-header {
      max-width: 1600px;
      margin: 0 auto;
      left: 50%;
      transform: translateX(-50%);
      width: 100%;
    }
   

    .month-nav button {
      background: none;
      border: 1px solid #e0e0e0;
      padding: 4px 4px;
      border-radius: 6px;
      color: #666;
      transition: all 0.3s ease;
    }

    .logo {
      width: 2rem;
      height: 2rem;
    }
    .calendar-footer {
      display: none;
    }
  }

  /* 其他通用样式 */
  .tooltip-container {
    position: relative;
  }

  .tooltip {
    position: absolute;
    bottom: -35px;
    left: 50%;
    transform: translateX(-50%) translateY(10px);
    background-color: rgba(0, 0, 0, 0.8);
    color: white;
    padding: 6px 12px;
    border-radius: 4px;
    font-size: 0.85rem;
    white-space: nowrap;
    opacity: 0;
    visibility: hidden;
    transition: all 0.3s ease;
  }

  .tooltip::before {
    content: "";
    position: absolute;
    top: -4px;
    left: 50%;
    transform: translateX(-50%) rotate(45deg);
    width: 8px;
    height: 8px;
    background-color: rgba(0, 0, 0, 0.8);
  }

  .tooltip-container:hover .tooltip {
    opacity: 1;
    visibility: visible;
    transform: translateX(-50%) translateY(0);
  }

  .month-nav button:hover {
    background-color: #f0f0f0;
    color: #333;
    transform: translateY(-1px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  }

  .month-nav input:hover {
    border-color: #d0d0d0;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
    transform: translateY(-1px);
  }

  .month-nav input:focus {
    outline: none;
    border-color: #fbc400;
    box-shadow: 0 0 0 3px rgba(251, 196, 0, 0.1);
  }

  .calendar-container {
    margin-top: 80px;
    padding: 0 20px 20px;
    flex: 1;
    position: relative;
    display: flex;
    align-items: start;
    justify-content: center;
    overflow: hidden;
    height: calc(100vh - 80px);
  }

  .calendar-container > img {
    position: absolute;
    z-index: -10;
    opacity: 0.2;
    border-radius: 3rem;
  }

  .calendar {
    width: min(95vw, 800px);
    aspect-ratio: 7/6;
    max-height: 95%;
    background-color: white;
    opacity: 0.8;
    border-radius: 12px;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
    padding: 16px;
  }

  .weekdays {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    background-color: #fbfbfb;
    padding: 0.8rem 0;
    color: #666;
    font-weight: 500;
  }

  .days {
    display: grid;
    grid-template-columns: repeat(7, 1fr);
    gap: 2px;
    background-color: #fbfbfb;
    padding: 2px;
  }

  .day {
    aspect-ratio: 1;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: clamp(0.8rem, 1.5vw, 1.2rem);
    cursor: pointer;
    background-color: white;
    transition: all 0.3s ease;
    border-radius: 6px;
    border: 1px solid transparent;
  }

  .day:hover {
    background-color: #f8f9fa;
    transform: translateY(-2px);
    box-shadow: 0 3px 6px rgba(0, 0, 0, 0.08);
    border-color: #e5e7eb;
    color: #1a1a1a;
  }

  .day:active {
    transform: translateY(0);
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.05);
  }

  .day.empty {
    background-color: #fffdf7;
  }

  .today {
    border: 2px solid #9ca3af;
    color: #4b5563;
    font-weight: 900;
  }

  .other-month {
    color: #bbb;
    background-color: #fafafa !important;
  }

  .other-month:hover {
    transform: none;
    box-shadow: none;
    background-color: #fafafa !important;
    border-color: transparent;
  }

  :global(body) {
    margin: 0;
    padding: 0;
    overflow: hidden;
  }

  :global(html) {
    overflow: hidden;
  }

  /* 加载和错误状态样式 */
  .loading-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 20px;
  }

  .loading-spinner {
    width: 40px;
    height: 40px;
    border: 3px solid #f3f3f3;
    border-top: 3px solid #fbc400;
    border-radius: 50%;
    animation: spin 1s linear infinite;
    margin-bottom: 10px;
  }

  .loading-text {
    color: #666;
    font-size: 14px;
    font-family: Arial, sans-serif;
  }

  @keyframes spin {
    0% { transform: rotate(0deg); }
    100% { transform: rotate(360deg); }
  }

  .error-container {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 2rem;
    margin: 2rem auto;
  }

  .error-icon {
    font-size: 3rem;
    margin-bottom: 1rem;
    color: #f44336;
  }

  .error-content {
    margin-bottom: 1.5rem;
  }

  .error-title {
    font-size: 1.5rem;
    color: #d32f2f;
    margin: 0 0 0.5rem 0;
  }

  .error-message {
    color: #616161;
    margin: 0;
    font-size: 1rem;
    line-height: 1.5;
  }

  .retry-button {
    background-color: #2196f3;
    color: white;
    border: none;
    border-radius: 4px;
    padding: 0.5rem 1.5rem;
    font-size: 1rem;
    cursor: pointer;
    transition: background-color 0.3s;
  }

  .retry-button:hover {
    background-color: #1976d2;
  }

  /* 下拉菜单样式 */
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
