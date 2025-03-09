<script lang="ts">
    import { exportReservations } from "../../../biz/exportSheets";
    import type { Reservation, Station } from "../../../biz/types";
    import ReservationForm from "../../../components/ReservationForm.svelte";
    import SortableTable from "../../../components/SortableTable.svelte";
    import { errorHandler } from "../../../biz/errorHandler";
    import type { AppError } from "../../../biz/errors";
    
    import { getGlobal } from "../../../biz/globalStore";
      import { invoke } from "@tauri-apps/api/core";
    import { hideModal, showModal } from "../../../components/modalStore";
    let timeRangeForReservations = $state("month");
    let loadingIndicator = $state(0);
    async function loadReservations(indicator: number) {
      try {
        const user = getGlobal("user");
        console.log(user)
        return []
      } catch (error) {
        errorHandler.handleError(error as AppError);
        return [];
      }
    }
  
    async function handleReservationSubmit(
      reservation: Reservation,
      isCreate: boolean,
    ) {
    //   await submitReservation(reservation, isCreate);
      loadingIndicator++;
    }
    async function handleReservationDelete(reservation: Reservation) {
    //   await deleteReservation(reservation);
      loadingIndicator++;
    }
    async function handleExportReservations() {
      const user = getGlobal("user");
      try {
      console.log(user)
      const reservations=await invoke<Reservation[]>("get_reservations_by_reservate_by_and_pe", {timerange:timeRangeForReservations, reservateby:user.user, pe:user.fullname});
        await exportReservations(reservations);
        errorHandler.showInfo("导出成功");
      } catch (e) {
        errorHandler.handleError(e as AppError);
      }
    }
  </script>
  
  <div class="content">
    {#await loadReservations(loadingIndicator)}
      <p>加载中...</p>
    {:then reservations}
      <div class="header">
        <div class="title-group">
          <h4>我的预约数据</h4>
          <select
            bind:value={timeRangeForReservations}
            class="time-range-select"
            onchange={() => loadingIndicator++}
          >
            <option value="month">本月</option>
            <option value="year">本年</option>
            <option value="all">所有</option>
          </select>
          {#if reservations.length > 0}
            <div style="text-align: center; font-size: smaller;">
              {reservations.length} items in total
            </div>
          {/if}
        </div>
        <div class="button-group">
          <button
            aria-label="导出数据"
            class="action-button export-button"
            onclick={handleExportReservations}
          >
            <svg
              class="logo"
              viewBox="0 0 1309 1024"
              version="1.1"
              xmlns="http://www.w3.org/2000/svg"
              width="200"
              height="200"
            >
              <path
                d="M1177.713778 344.945778 740.408889 673.308444l0-230.968889c-418.360889 0-436.878222 253.667556-437.447111 288.199111 0-16.782222 0-82.460444 0-93.44 0-145.009778 84.849778-389.518222 437.447111-389.518222L740.408889 28.444444 1177.713778 344.945778zM355.697778 534.186667c0 0 87.808-140.544 396.458667-140.544 22.584889 0 46.392889 0 46.392889 0l0 170.410667 289.507556-219.107556L798.549333 137.671111l0 164.096c0 0-23.864889 0-46.392889 0C421.063111 301.767111 355.697778 534.186667 355.697778 534.186667zM302.961778 730.538667c0 2.474667 0 3.953778 0 3.953778S302.904889 733.041778 302.961778 730.538667zM186.311111 175.559111l0 706.019556c0 32.540444 26.112 58.823111 58.311111 58.823111l728.974222 0c32.199111 0 58.311111-26.311111 58.311111-58.823111l0-176.526222 58.339556 0 0 176.526222c0 64.967111-52.224 117.674667-116.650667 117.674667L244.650667 999.253333c-64.426667 0-116.650667-52.707556-116.650667-117.674667L128 175.559111c0-64.995556 52.224-117.674667 116.650667-117.674667l262.428444 0 0 58.823111L244.650667 116.707556C212.423111 116.707556 186.311111 143.047111 186.311111 175.559111z"
              ></path>
            </svg>
            <span>导出数据</span>
          </button>
          <button
            aria-label="创建预约"
            class="action-button add-button"
            onclick={() =>
              showModal(ReservationForm, {
                isSimpleMode: false,
                submitHandler: handleReservationSubmit,
                onNegative: () => hideModal(),
                reservation: undefined,
              })}
          >
            <svg viewBox="0 0 1024 1024" width="20" height="20">
              <path
                d="M832 448H576V192a64 64 0 0 0-128 0v256H192a64 64 0 0 0 0 128h256v256a64 64 0 1 0 128 0V576h256a64 64 0 1 0 0-128z"
              />
            </svg>
            <span>创建预约</span>
          </button>
        </div>
      </div>
      <SortableTable
        data={reservations}
        isIdxShow={false}
        columns={[
          {
            key: "reservation_date",
            label: "日期",
            sortable: true,
            maxWidth: "250px",
          },
          {
            key: "time_slot",
            label: "时间",
            sortable: true,
            formatter: async (timeSlot) => {
              switch (timeSlot) {
                case "T1":
                  return Promise.resolve("9:30-12:00");
                case "T2":
                  return Promise.resolve("13:00-15:00");
                case "T3":
                  return Promise.resolve("15:00-17:30");
                case "T4":
                  return Promise.resolve("18:00-20:30");
                case "T5":
                  return Promise.resolve("20:30-23:59");
                default:
                  return Promise.resolve(String(timeSlot));
              }
            },
          },
          {
            key: "station_id",
            label: "工位",
            sortable: true,
            formatter: async (id) => {
              const name = (await invoke<Station>("get_station_by_id", { id: id.toString() })).name;
                return name || "Unknown station";
            },
          },
          { key: "client_name", label: "客户", sortable: true },
          {
            key: "project_engineer",
            label: "项目工程师",
            sortable: true,
            maxWidth: "150px",
          },
          {
            key: "testing_engineer",
            label: "测试工程师",
            sortable: true,
            maxWidth: "150px",
          },
          { key: "job_no", label: "Job No.", sortable: true },
          {
            key: "reservation_status",
            label: "状态",
            sortable: true,
            maxWidth: "150px",
          },
          {
            key: "reservate_by",
            label: "创建",
            sortable: true,
            maxWidth: "150px",
          },
        ]}
        actions={[
          {
            label: "编辑",
            class: "edit",
            handler: (reservation) => {
            //   if (!reservationBlockClickPrecheck(reservation.reservation_date))
            //     return;
              showModal(ReservationForm, {
                submitHandler: handleReservationSubmit,
                onNegative: () => hideModal(),
                reservation: reservation as Reservation,
              });
            },
          },
          {
            label: "删除",
            class: "delete",
            handler: (reservation) => handleReservationDelete(reservation),
          },
        ]}
      />
    {/await}
  </div>
  
  <style>
    .logo {
      fill: #4a90e2;
      width: 20px;
      height: 20px;
    }
    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 20px;
    }
    .content {
      max-height: calc(100vh - 150px);
      overflow: hidden;
      padding-bottom: 20px;
    }
    .title-group {
      display: flex;
      align-items: center;
      gap: 16px;
    }
    .button-group {
      display: flex;
      gap: 12px;
      align-items: center;
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
      fill: #4a90e2;
    }
  
    .action-button:hover {
      transform: translateY(-1px);
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }
    .export-button:hover {
      background-color: #f5f9ff;
    }
  
    .export-button .logo {
      width: 16px;
      height: 16px;
    }
    .time-range-select {
      padding: 4px 8px;
      border: 1px solid #e0e4e8;
      border-radius: 4px;
      background-color: white;
      font-size: 14px;
      color: #333;
      cursor: pointer;
    }
  
    .time-range-select:hover {
      border-color: #4a90e2;
    }
  
    .time-range-select:focus {
      outline: none;
      border-color: #4a90e2;
      box-shadow: 0 0 0 2px rgba(74, 144, 226, 0.2);
    }
  </style>
  