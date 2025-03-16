<script lang="ts">
  import { onMount } from "svelte";
  import type { Reservation, Sevent, Station, User } from "../../biz/types";
  import { goto } from "$app/navigation";
  import type { PageData } from "../$types";
  import { calendar } from "../../biz/calendar";
  import {
    hideModal,
    modalStore,
    showModal,
  } from "../../components/modalStore";
  import ReservationInfo from "../../components/ReservationInfo.svelte";
  import ReservationForm from "../../components/ReservationForm.svelte";
  import { getGlobal, setGlobal } from "../../biz/globalStore";
  import { errorHandler } from "../../biz/errorHandler";
  import Source from "../../components/Source.svelte";
  import type { AppError } from "../../biz/errors";
  import { get } from "svelte/store";
  import { apiService } from "../../biz/apiService";
  import Avartar from "../../components/Avartar.svelte";
  import About from "../../components/About.svelte";
  import { dayPageInit, reservationBlockClickPrecheck } from "../../biz/operation";
    import { submitReservation } from "../../biz/localService";
  let { data }: { data: PageData } = $props();
  console.log("data:", data);
  const user = getGlobal("user");
  console.log(user);
  const initDate = data.date;
  calendar.setDate(initDate);
  const selectedDate = $derived(calendar.selectedDate);
  async function loadStations(): Promise<Station[]> {
    let stations: Station[] = [];
    let stationEntities: Station[] = [];
    let sevents: Sevent[] = [];
    try {
      stationEntities = await apiService.Get(`/stations`);
      sevents = await apiService.Get(`/sevents`);

      console.log(stationEntities);
      //获取orders
      const orders: { id: number; seq: number }[] = getGlobal("station_orders");
      console.log(orders);
      //结合orders给stations排序
      if (orders && orders.length > 0 && stationEntities.length > 0) {
        const sortedStations = stationEntities.map((station: Station) => {
          const seq = orders.find((o) => o.id === station.id);
          return { ...station, sequence_no: seq?.seq ?? 1 };
        });
        stations = [...sortedStations].sort(
          (a, b) => a.sequence_no - b.sequence_no,
        );
      } else {
        stations = [...stationEntities];
      }
      //结合sevents给stations添加状态
      if (sevents.length > 0) {
        stations = stations.map((station) => {
          const sevents_per_station = sevents.filter(
            (sevent) => sevent.station_id === station.id,
          );
          if (sevents_per_station.length === 0) {
            return { ...station, status: "in_service" };
          } else {
            const isUnavailable = sevents_per_station.some(
              (sevent) =>
                new Date(get(selectedDate)) >= new Date(sevent.from_date) &&
                new Date(get(selectedDate)) <= new Date(sevent.to_date),
            );
            if (isUnavailable) {
              const unavailableSevent = sevents_per_station.find(
                (sevent) =>
                  new Date(get(selectedDate)) >= new Date(sevent.from_date) &&
                  new Date(get(selectedDate)) <= new Date(sevent.to_date),
              );
              return {
                ...station,
                status: unavailableSevent?.name ?? "unavailable",
              };
            } else {
              return { ...station, status: "in_service" };
            }
          }
        });
      }
      return stations;
    } catch (e) {
      errorHandler.handleError(e as AppError);
      return [];
    }
  }
  async function loadReservations(date: string) {
    let reservations: Reservation[] = [];

    try {
      reservations = await apiService.Get(`/reservations/${date}`);
      console.log("reservations:", reservations);
      return reservations;
    } catch (e) {
      errorHandler.handleError(e as AppError);
      return [];
    }
  }
  const loadSevents = async () => {
    let sevents: Sevent[] = [];
    try {
      sevents = await apiService.Get(`/sevents`);
      return sevents;
    } catch (e: any) {
      if (e.message?.includes("Request throttled")) {
        // Wait for the throttle delay and retry
        await new Promise((resolve) => setTimeout(resolve, 500));
        return loadSevents();
      }
      errorHandler.handleError(e as AppError);
      return [];
    }
  };

  const init_data = async (date: string, loadingIndicator: number) => {
    const stations = await loadStations();
    console.log("stations:", stations);
    const res = await loadReservations(date);
    console.log("reservations:", res);
    const sevents = await loadSevents();
    console.log("sevents:", sevents);
    return { stations, res, sevents };
  };

  let loadingIndicator = $state(0);

  const init_page = async () => {
    await dayPageInit();
    await new Promise((resolve) => setTimeout(resolve, 200));
  };

  // Add keyboard event listener for day navigation
  const handleKeydown = (event: KeyboardEvent) => {
    // 如果modal正在显示，且不是在输入框内
    if ($modalStore.isShow) {
      const target = event.target as HTMLElement;
      const isInput =
        target.tagName === "INPUT" ||
        target.tagName === "TEXTAREA" ||
        target.isContentEditable;

      // 如果不是在输入框内，才阻止默认行为
      if (
        !isInput &&
        (event.key === "ArrowLeft" || event.key === "ArrowRight")
      ) {
        event.preventDefault();
      }
      return;
    }

    if (event.key === "ArrowLeft") {
      calendar.previous();
      loadReservations($selectedDate);
    } else if (event.key === "ArrowRight") {
      calendar.next();
      loadReservations($selectedDate);
    }
  };

  onMount(() => {
    window.addEventListener("keydown", handleKeydown);
    return () => window.removeEventListener("keydown", handleKeydown);
  });
  async function logout() {
    await apiService.logout();
    goto("/auth/login");
  }

  /**
 * 处理预约块点击事件
 * @param reservation 当前预约信息（如果存在）
 * @param station 工位信息
 * @param selectedDateStr 选中的日期
 */
 const handleReservationBlockClick = (reservation: Reservation | null, station: Station, selectedDateStr: string,timeslot:string) => {
    const user = getGlobal("user");
    console.log('start')
    // 1. 用户为游客（未登录）情况
    if (!user) {
      if (reservation) {
        // 1.a 有预约，显示预约信息
        showModal(ReservationInfo as any, { reservation });
      } else {
        // 1.b 没有预约，显示提示信息
        errorHandler.showInfo("无预约");
      }
      return;
    }
  
    // 2. 用户为管理员情况
    if (user.role.toLowerCase() === 'admin') {
      console.log('admin')
      if (reservation) {
        // 2.a 有预约，显示编辑表单
        showModal(ReservationForm as any, {
          item: reservation,
          isSimpleMode: true,
          submitHandler: async (modifiedReservation: Reservation, isCreate: boolean, user: User) => {
            await submitReservation(modifiedReservation, false, user);
            loadingIndicator++;
          },
          onNegative: () => {
            hideModal();
          }
        });
      } else {
        // 2.b 没有预约，显示创建表单
        showModal(ReservationForm as any, {
          item: {
            station_id: station.id,
            reservation_date: selectedDateStr,
            time_slot: timeslot,
          } as Reservation,
          isSimpleMode: true,
          submitHandler: async (reservation: Reservation, isCreate: boolean, user: User) => {
            await submitReservation(reservation, true, user);
            loadingIndicator++;
          },
          onNegative: () => {
            hideModal();
          }
        });
      }
      return;
    }
  
    // 3. 用户为工程师情况
    if (user.role.toLowerCase() === 'engineer') {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const reserveDate = new Date(selectedDateStr);
      reserveDate.setHours(0, 0, 0, 0);
      console.log("in engineer")
      // 检查日期是否为今天或未来
      const isCurrentOrFuture = reserveDate >= today;
      
      if (reservation) {
        // 3.a 有预约
        // 检查条件A：是否是当前用户的预约
        const isOwnReservation = 
          reservation.project_engineer === user.englishname || 
          reservation.reservate_by === user.username;
        
        // 条件A和B都满足时，显示编辑表单
        if (isCurrentOrFuture && isOwnReservation) {
          showModal(ReservationForm as any, {
            item: reservation,
            isSimpleMode: true,
            submitHandler: async (modifiedReservation: Reservation, isCreate: boolean, user: User) => {
              await submitReservation(modifiedReservation, false, user);
              loadingIndicator++;
            },
            onNegative: () => {
              hideModal();
            }
          });
        } else {
          // 不满足条件，显示提示信息
          if (!isCurrentOrFuture) {
            errorHandler.showWarning("不能修改过去的预约");
          } else if (!isOwnReservation) {
            errorHandler.showWarning("只能修改自己创建的预约");
          }
          
          // 仍然显示预约信息（只读）
          showModal(ReservationInfo as any, { reservation });
        }
      } else {
        // 3.b 没有预约
        // 只检查条件B：时间是否为今天或未来
        if (isCurrentOrFuture) {
          showModal(ReservationForm as any, {
            item: {
              station_id: station.id,
              reservation_date: selectedDateStr,
              time_slot: timeslot,
            } as Reservation,
            isSimpleMode: true,
            submitHandler: async (modifiedReservation: Reservation, isCreate: boolean, user: User) => {
              await submitReservation(modifiedReservation, true, user); 
              loadingIndicator++;
            },
            onNegative: () => {
              hideModal();
            }
          });
        } else {
          errorHandler.showWarning("不能为过去的日期创建预约");
        }
      }
    }
  };
</script>

{#snippet showDayBlock(
  timeslot: string,
  station: Station,
  reservations: Reservation[],
  sevents: Sevent[],
)}
  <td
    class="fixed-width"
    class:station-unavailable={station.status !== "in_service"}
    style="text-align:left;font-size:12px;"
  >
    {#if station.status !== "in_service"}
      <button
        class="tooltip-container unavailable-cell"
        onclick={() => errorHandler.showInfo("工位不可用")}
      >
        <span class={`${timeslot === "T5" ? "tooltip-top" : "tooltip"}`}
          >工位不可用</span
        >
        <svg
          class="logo"
          style="fill: #fbc400;opacity:0.8"
          viewBox="0 0 1024 1024"
          version="1.1"
          xmlns="http://www.w3.org/2000/svg"
          width="200"
          height="200"
          ><path
            d="M821.344 458.656v106.688H202.656v-106.688h618.688zM1024 512c0 282.784-229.216 512-512 512S0 794.784 0 512 229.216 0 512 0s512 229.216 512 512z m-85.344 0c0-235.264-191.392-426.656-426.656-426.656S85.344 276.736 85.344 512 276.736 938.656 512 938.656 938.656 747.264 938.656 512z"
          ></path></svg
        >
        <span
          style="font-size: 0.8rem;margin-top:0.5rem"
          class="first-letter-capital"
          >{station.status.replaceAll("_", " ")}</span
        >
      </button>
    {:else}
      {#await Promise.resolve(reservations.filter((f) => f.time_slot === timeslot && f.station_id === station.id)[0])}
        <div>Loading...</div>
      {:then reservation}
        {#if reservation}
          <button
            style="gap: 5px; display: flex; flex-direction: column; align-items: center; width: 100%;"
            class="tooltip-container"
              onclick={() => handleReservationBlockClick(reservation, station, $selectedDate,timeslot)}
          >
            <span class={timeslot === "T5" ? "tooltip-top" : "tooltip"}
              >点击查看预约</span
            >
            <div
              class="engineer-info"
              style="display: flex; flex-direction: column; gap: 0.25rem; font-size: 0.75rem;"
            >
              <div
                class="engineer-item"
                style="display: flex; align-items: center; gap: 0.25rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;"
              >
                <span
                  class="badge"
                  style=""
                  >PE</span
                >
                <span
                  class="name"
                  style="color: #1e293b; overflow: hidden; text-overflow: ellipsis;"
                  >{reservation.project_engineer}</span
                >
              </div>
              <div
                class="engineer-item"
                style="display: flex; align-items: center; gap: 0.25rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis;"
              >
                <span
                  class="badge"
                  >TE</span
                >
                <span
                  class="name"
                  style="color: #1e293b; overflow: hidden; text-overflow: ellipsis;"
                  >{reservation.testing_engineer}</span
                >
              </div>
            </div>
            {#if reservation.job_no || reservation.product_name || reservation.client_name}
              <div class="divider"></div>
              {#if reservation.job_no}
                <div
                  style="font-size: smaller; text-align: center; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 100%;"
                >
                  <!-- <span style="font-weight:bold;">Job No.:</span
          > -->
                  {reservation.job_no}
                </div>
              {/if}
              {#if reservation.product_name}
                <div
                  style="font-size: smaller; text-align: center; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 100%;"
                >
                  <!-- <span style="font-weight:bold;">Product:</span
            > -->
                  {reservation.product_name}
                </div>
              {/if}
              {#if reservation.client_name}
                <div
                  style="font-size: smaller; text-align: center; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 100%;"
                >
                  <!-- <span style="font-weight:bold;">Client:</span
            > -->
                  {reservation.client_name}
                </div>
              {/if}
            {/if}
          </button>
        {:else}
          <button
            class="tooltip-container coffee-btn"
            aria-label="open_new"
            onclick={() => handleReservationBlockClick(null, station, $selectedDate,timeslot)}
          >
            <span class={timeslot === "T5" ? "tooltip-top" : "tooltip"}
              >无预约</span
            >
            <svg
              class="logo spare"
              viewBox="0 0 1024 1024"
              version="1.1"
              xmlns="http://www.w3.org/2000/svg"
              width="200"
              height="200"
              ><path
                d="M797.994667 448.725333v123.306667a243.626667 243.626667 0 0 1-243.626667 243.669333H371.626667A243.626667 243.626667 0 0 1 128 572.074667V358.826667c0-16.810667 13.653333-30.421333 30.464-30.421334h119.381333C292.096 319.146667 298.666667 305.408 298.666667 285.184c0-13.098667-6.058667-23.253333-25.344-44.928l-3.541334-4.010667c-23.125333-26.112-32.725333-42.538667-32.554666-67.114666 0.256-37.333333 15.658667-65.536 45.482666-81.322667a21.333333 21.333333 0 0 1 19.968 37.717333c-15.232 8.064-22.613333 21.589333-22.784 43.946667-0.085333 10.581333 5.376 19.925333 21.845334 38.485333l3.498666 3.968c25.984 29.226667 36.096 46.08 36.096 73.258667 0 16.128-2.986667 30.634667-8.874666 43.264h116.010666c14.250667-9.301333 20.864-23.04 20.864-43.264 0-13.098667-6.058667-23.253333-25.344-44.928l-3.541333-4.010667c-23.125333-26.112-32.725333-42.538667-32.554667-67.114666 0.256-37.333333 15.658667-65.536 45.482667-81.322667a21.333333 21.333333 0 1 1 19.968 37.717333c-15.232 8.064-22.613333 21.589333-22.784 43.946667-0.085333 10.581333 5.376 19.925333 21.845333 38.485333l3.498667 3.968c25.984 29.226667 36.096 46.08 36.096 73.258667 0 16.128-2.986667 30.634667-8.874667 43.264h93.696c16.853333 0 30.464 13.610667 30.464 30.421333v46.976A149.333333 149.333333 0 1 1 810.666667 704a21.333333 21.333333 0 1 1 0-42.666667 106.666667 106.666667 0 1 0-12.672-212.608zM213.333333 917.333333a21.333333 21.333333 0 1 1 0-42.666666h512a21.333333 21.333333 0 1 1 0 42.666666H213.333333z"
              ></path></svg
            >
          </button>
        {/if}
      {/await}
    {/if}
  </td>
{/snippet}

<main class="container">
  {#await init_page()}
    <div class="loading-container">
      <div class="loading-spinner-wrapper">
        <div class="loading-spinner"></div>
      </div>
      <span class="loading-text">加载中...</span>
    </div>
  {:then _}
    <div class="fixed-header">
      <button
        aria-label="calendar_view"
        class="tooltip-container"
        onclick={() => goto("/")}
      >
        <span class="tooltip">返回月度视图</span>
        <svg
          class="logo"
          style="fill: #fbc400;"
          viewBox="0 0 1024 1024"
          version="1.1"
          xmlns="http://www.w3.org/2000/svg"
          width="200"
          height="200"
          ><path
            d="M109.714286 950.857143l164.571429 0 0-164.571429-164.571429 0 0 164.571429zM310.857143 950.857143l182.857143 0 0-164.571429-182.857143 0 0 164.571429zM109.714286 749.714286l164.571429 0 0-182.857143-164.571429 0 0 182.857143zM310.857143 749.714286l182.857143 0 0-182.857143-182.857143 0 0 182.857143zM109.714286 530.285714l164.571429 0 0-164.571429-164.571429 0 0 164.571429zM530.285714 950.857143l182.857143 0 0-164.571429-182.857143 0 0 164.571429zM310.857143 530.285714l182.857143 0 0-164.571429-182.857143 0 0 164.571429zM749.714286 950.857143l164.571429 0 0-164.571429-164.571429 0 0 164.571429zM530.285714 749.714286l182.857143 0 0-182.857143-182.857143 0 0 182.857143zM329.142857 256l0-164.571429q0-7.460571-5.412571-12.873143t-12.873143-5.412571l-36.571429 0q-7.460571 0-12.873143 5.412571t-5.412571 12.873143l0 164.571429q0 7.460571 5.412571 12.873143t12.873143 5.412571l36.571429 0q7.460571 0 12.873143-5.412571t5.412571-12.873143zM749.714286 749.714286l164.571429 0 0-182.857143-164.571429 0 0 182.857143zM530.285714 530.285714l182.857143 0 0-164.571429-182.857143 0 0 164.571429zM749.714286 530.285714l164.571429 0 0-164.571429-164.571429 0 0 164.571429zM768 256l0-164.571429q0-7.460571-5.412571-12.873143t-12.873143-5.412571l-36.571429 0q-7.460571 0-12.873143 5.412571t-5.412571 12.873143l0 164.571429q0 7.460571 5.412571 12.873143t12.873143 5.412571l36.571429 0q7.460571 0 12.873143-5.412571t5.412571-12.873143zM987.428571 219.428571l0 731.428571q0 29.696-21.723429 51.419429t-51.419429 21.723429l-804.571429 0q-29.696 0-51.419429-21.723429t-21.723429-51.419429l0-731.428571q0-29.696 21.723429-51.419429t51.419429-21.723429l73.142857 0 0-54.857143q0-37.741714 26.843429-64.585143t64.585143-26.843429l36.571429 0q37.741714 0 64.585143 26.843429t26.843429 64.585143l0 54.857143 219.428571 0 0-54.857143q0-37.741714 26.843429-64.585143t64.585143-26.843429l36.571429 0q37.741714 0 64.585143 26.843429t26.843429 64.585143l0 54.857143 73.142857 0q29.696 0 51.419429 21.723429t21.723429 51.419429z"
          ></path></svg
        >
      </button>
      <!-- <img class="brand" src="/intertek.png" alt="Intertek" /> -->
      <div class="month-nav">
        <button
          class="tooltip-container"
          aria-label="previous_month"
          onclick={() => {
            calendar.previous();
            loadReservations($selectedDate);
          }}
        >
          <span class="tooltip">上一天</span>
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
          type="date"
          bind:value={$selectedDate}
          onchange={(e: any) => {
            calendar.setDate(e.target?.value);
            loadReservations($selectedDate);
          }}
          class="date-input"
        />
        <button
          class="tooltip-container"
          aria-label="next_month"
          onclick={() => {
            calendar.next();
            loadReservations($selectedDate);
          }}
        >
          <span class="tooltip">下一天</span>
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
      <div class="dropdown-container tooltip-container">
        {#if localStorage.getItem("accessToken")}
          {@const user = getGlobal("user")}
          <button class="dropdown-trigger" onclick={() => goto("/auth/profile")} aria-label="profile">
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

    <div class="table-container">
      <div class="table-wrapper">
        {#await init_data($selectedDate, loadingIndicator)}
          <div class="loading-container">
            <div class="loading-spinner-wrapper">
              <div class="loading-spinner"></div>
            </div>
            <span class="loading-text">加载中...</span>
          </div>
        {:then obj}
          <table>
            <thead>
              <tr>
                <th class="sticky-column">时间</th>
                {#each obj.stations as station}
                  <th
                    class="fixed-width tooltip-container"
                    class:station-unavailable={station.status !== "in_service"}
                  >
                    <span class="tooltip">点击查看工位详情</span>
                    <a
                      title={station.name}
                      href={`/station?stationId=${station.id}&date=${$selectedDate}`}
                      class="link"
                    >
                      {station.short_name}
                    </a>
                  </th>
                {/each}
              </tr>
            </thead>
            <tbody>
              {#each ["9:30-12:00", "13:00-15:00", "15:00-17:30", "18:00-20:30", "20:30-23:59"] as timeSlot, idx}
                <tr>
                  <td class="sticky-column">{timeSlot}</td>
                  {#each obj.stations as station}
                    {@render showDayBlock(
                      `T${idx + 1}`,
                      station,
                      obj.res,
                      obj.sevents,
                    )}
                  {/each}
                </tr>
              {/each}
            </tbody>
          </table>
        {/await}
      </div>
    </div>
  {:catch error}
    <div class="error-container">
      <div class="error-icon">❌</div>
      <div class="error-content">
        <h3 class="error-title">抱歉，出现了问题</h3>
        <p class="error-message">
          {error.message + " " + error.details.originalError.toString()}
        </p>
      </div>
      <button class="retry-button" onclick={() => window.location.reload()}>
        重新加载
      </button>
    </div>
  {/await}
</main>

<style>
  .container {
    display: flex;
    flex-direction: column;
    height: 100vh; /* 占满整个视口高度 */
    font-family: "Segoe UI", Tahoma, Geneva, Verdana, sans-serif;
    background-color: #f0f4f8;
    justify-content: center;
    width: 100%;
  }
  .logo {
    width: 25px;
    height: 25px;
  }

  .fixed-header {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    background-color: #f8f9fa;
    z-index: 100;
    padding: 1.2rem;
    display: flex;
    justify-content: space-between;
    align-items: center;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  }

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
    z-index: 1000;
  }
  .tooltip-top {
    top: -35px;
    position: absolute;
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
    z-index: 1000;
  }
  /* 添加小三角形 */
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
  .tooltip-top::before {
    content: "";
    position: absolute;
    width: 8px;
    height: 8px;
    background-color: rgba(0, 0, 0, 0.8);
    bottom: -4px;
    left: 50%;
    transform: translateX(-50%) rotate(45deg);
  }
  .tooltip-container:hover .tooltip {
    opacity: 1;
    visibility: visible;
    transform: translateX(-50%) translateY(0);
  }
  .tooltip-container:hover .tooltip-top {
    opacity: 1;
    visibility: visible;
    transform: translateX(-50%) translateY(0);
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
  .month-nav {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: 24px;
  }

  .month-nav button {
    background: none;
    border: 1px solid #e0e0e0;
    padding: 4px 4px;
    border-radius: 6px;
    color: #666;
    transition: all 0.3s ease;
  }

  .month-nav button:hover {
    background-color: #f0f0f0;
    color: #333;
    transform: translateY(-1px);
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
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

  .date-input::-webkit-calendar-picker-indicator {
    filter: invert(70%) sepia(70%) saturate(1000%) hue-rotate(360deg);
    cursor: pointer;
    width: 1.5rem;
    height: 1.5rem;
    padding: 0px;
  }

  .table-container {
    flex: 1; /* 占据剩余空间 */
    margin-top: 60px; /* 留出操作栏的空间 */
    padding: 20px;
    display: flex;
    width: 100vw;
    align-items: stretch; /* 使表格占满容器高度 */
  }

  .table-wrapper {
    width: 100%;
    overflow-x: auto;
    overflow-y: hidden;
    position: relative;
  }

  table {
    width: auto; /* 改为自动宽度 */
    height: 100%; /* 表格高度100% */
    border-collapse: collapse;
    background-color: #fff;
    border-radius: 8px;
    box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    margin: 0 auto; /* 水平居中 */
  }

  tbody {
    height: 100%; /* tbody高度100% */
    /* overflow: auto; */
  }

  tr {
    height: 20%; /* 每行平均分配高度 */
  }

  .fixed-width {
    width: 100px;
    min-width: 100px;
    max-width: 100px;
    word-wrap: break-word;
  }
  table thead th .fixed-width{
    width: 100px;
    min-width: 100px;
    max-width: 100px;
    word-wrap: break-word;
    font-size: 0.5rem;
  }

  .sticky-column {
    position: sticky;
    left: 0;
    background-color: #fff;
    z-index: 1;
    font-family: Arial, Helvetica, sans-serif;
    font-size: 0.8rem;
    font-weight: 600;
    /* 添加阴影效果以区分固定列 */
    box-shadow: 2px 0 4px rgba(0, 0, 0, 0.1);
  }
  
  th,
  td {
    border: 1px solid #ddd;
    padding: 6px;
    text-align: center;
    height: 50px; /* 设置单元格高度 */
  }

  th {
    background-color: #f7f9fc;
  }

  /* 确保固定列的表头样式正确 */
  th.sticky-column {
    background-color: #f7f9fc;
    z-index: 2;
  }

  td:hover {
    background-color: #f1f1f1;
  }
  td > button {
    background-color: transparent;
    border: none;
    outline: none;
    cursor: pointer;
    width: 100%;
    height: 100%;
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: start;
  }
  .badge{
    background-color: #e2e8f0;
    color: #475569; 
    padding: 0.125rem 0.25rem; 
    border-radius: 0.25rem; 
    font-size: 0.7rem; 
    font-weight: 600; 
    flex-shrink: 0;
  }
  .coffee-btn {
    align-items: center;
  }
  td > button:hover {
    /* background-color: #94a3b8; */
    color: #626058;
  }

  .link {
    text-decoration: none;
    color: gray;
    font-family: Arial, Helvetica, sans-serif;
  }

  .station-unavailable {
    background-color: #dfdee4 !important; /* 浅红色背景 */
  }

  .unavailable-cell {
    color: #dbdbe0;
    background-color: transparent;
    width: 100%;
    height: 100%;
    display: flex;
    justify-content: center;
    align-items: center;
    cursor: pointer;
  }

  .unavailable-cell:hover {
    background-color: rgba(114, 28, 36, 0.1);
  }

  .unavailable-cell:hover svg.logo {
    fill: rgb(194, 187, 187) !important;
  }
  /* 确保固定列的表头样式正确 */
  th.sticky-column.station-unavailable {
    background-color: #f8d7da;
    z-index: 2;
  }

  .spare {
    fill: #f9fafb;
    width: 3rem;
    height: 3rem;
  }
  .spare:hover {
    fill: #fbc400;
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

  @media (min-width: 768px) {
    .badge{
      font-size: 0.5rem;
      padding: 0.125rem 0.25rem;
    }
  }
</style>
