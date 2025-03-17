<script lang="ts">
    import { exportReservations } from "../../../biz/exportSheets";
    import type { Reservation, Station } from "../../../biz/types";
    import ReservationForm from "../../../components/ReservationForm.svelte";
    import SortableTable from "../../../components/SortableTable.svelte";
    import { errorHandler } from "../../../biz/errorHandler";
    import type { AppError } from "../../../biz/errors";
    
    import { getGlobal } from "../../../biz/globalStore";
    import { hideModal, showModal } from "../../../components/modalStore";
    import { apiService } from "../../../biz/apiService";
    import type{ Component } from "svelte";
    import { goto } from "$app/navigation";
    import { deleteReservation } from "../../../biz/operation";
    import { submitReservation } from "../../../biz/localService";
    let timeRangeForReservations = $state("month");
    let loadingIndicator = $state(0);
    let currentPage = $state(1);
    let pageSize = $state(20);

    const actions=[
            {
              label: "编辑",
              class: "edit",
              handler: (reservation: Reservation) => {
              //   if (!reservationBlockClickPrecheck(reservation.reservation_date))
              //     return;
                showModal(ReservationForm as unknown as Component<{}, {}, "">,
                  {
                  submitHandler: handleReservationSubmit,
                  onNegative: () => hideModal(),
                  reservation: reservation as Reservation,
                });
              },
            },
            {
              label: "删除",
              class: "delete",
              handler: (reservation: Reservation) => handleReservationDelete(reservation),
            },
          ];
    async function loadData(indicator: number, pageNumber: number, pageSize: number) {
      try {
        const user = getGlobal("user");
        console.log(user)
        if(!user){
          errorHandler.showError("请先登录");
          goto("/auth/login");
          return;
        }
        const stations=await apiService.Get("/stations");
        if(user.role.toLowerCase()==="admin"){
          const reservations=await apiService.Get(`/reservations?timeRange=${timeRangeForReservations}&pageNumber=${pageNumber}&pageSize=${pageSize}`)
          console.log(reservations)
          return {reservations,stations}
        }else{
          const reservations=await apiService.Get(`/reservations?timeRange=${timeRangeForReservations}&projectEngineer=${user.englishname}&reservatBy=${user.username}&pageNumber=${pageNumber}&pageSize=${pageSize}`)
          return {reservations,stations}
        }
      } catch (error) {
        errorHandler.handleError(error as AppError);
        return {reservations:[],stations:[]};
      }
    }
  
    async function handleReservationSubmit(
      reservation: Reservation,
      isCreate: boolean,
    ) {
      const user = getGlobal("user");
      await submitReservation(reservation, isCreate,user.role??"user");
      loadingIndicator++;
    }
    async function handleReservationDelete(reservation: Reservation) {
      const user = getGlobal("user");
      await deleteReservation(reservation,user);
      loadingIndicator++;
    }
    async function handleExportReservations() {
      const user = getGlobal("user");
      try {
      console.log(user)
      const reservations=await apiService.Get(`/reservations?timeRange=${timeRangeForReservations}&projectEngineer=${user.englishname}&reservatBy=${user.username}`)
        await exportReservations(reservations);
        errorHandler.showInfo("导出成功");
      } catch (e) {
        errorHandler.handleError(e as AppError);
      }
    }
    function handlePageChange(page: number) {
      currentPage = page;
    }

    function handlePageSizeChange(size: number) {
      pageSize = size;
    }
  </script>
  
  <div class="reservation-container">
    {#await loadData(loadingIndicator, currentPage, pageSize)}
      <div class="loading-state">
        <div class="loading-spinner"></div>
        <p>加载中...</p>
      </div>
    {:then data}
      <div class="header">
        <div class="title-group">
          <h3>预约数据</h3>
          <select
            bind:value={timeRangeForReservations}
            class="time-range-select"
            onchange={() => loadingIndicator++}
          >
            <option value="month">本月</option>
            <option value="year">本年</option>
            <option value="all">所有</option>
          </select>
          {#if data?.reservations?.items?.length > 0}
            <div class="data-count">
              共 {data?.reservations?.items?.length} 条数据
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
              class="icon"
              viewBox="0 0 1309 1024"
              version="1.1"
              xmlns="http://www.w3.org/2000/svg"
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
              showModal(ReservationForm as unknown as Component<{}, {}, "">,
                {
                isSimpleMode: false,
                submitHandler: handleReservationSubmit,
                onNegative: () => hideModal(),
                reservation: undefined,
              })}
          >
            <svg class="icon" viewBox="0 0 1024 1024">
              <path
                d="M832 448H576V192a64 64 0 0 0-128 0v256H192a64 64 0 0 0 0 128h256v256a64 64 0 1 0 128 0V576h256a64 64 0 1 0 0-128z"
              />
            </svg>
            <span>创建预约</span>
          </button>
        </div>
      </div>
      <div class="table-container">
        <SortableTable
          data={data?.reservations?.items}
          columns={
            [
            {
              key: "reservation_date",
              label: "日期",
              sortable: true,
              maxWidth: "120px",
            },
            {
              key: "time_slot",
              label: "时间",
              sortable: true,
              maxWidth: "100px",
              formatter: async (timeSlot: string) => {
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
              maxWidth: "100px",
              formatter: async (id: number) => {
                const name = data?.stations?.find((station:Station) => station.id === id)?.name;
                return name || "Unknown station";
              },
            },
            { key: "client_name", label: "客户", sortable: true, maxWidth: "150px" },
            {
              key: "project_engineer",
              label: "项目工程师",
              sortable: true,
              maxWidth: "120px",
            },
            {
              key: "testing_engineer",
              label: "测试工程师",
              sortable: true,
              maxWidth: "120px",
            },
            { key: "job_no", label: "Job No.", sortable: true, maxWidth: "120px" },
            {
              key: "reservation_status",
              label: "状态",
              sortable: true,
              maxWidth: "100px",
            },
            {
              key: "reservate_by",
              label: "创建人",
              sortable: true,
              maxWidth: "100px",
            },
          ]
          }
          actions={actions}
          pagination={true}
          onPageChange={handlePageChange}
          onPageSizeChange={handlePageSizeChange}
          totalItems={data?.reservations?.totalCount}
          pageSize={pageSize}
          currentPage={currentPage}
        />
        
      </div>
    {:catch e}
      <div class="error-state">{"加载失败: " + e.message}</div>
    {/await}
  </div>
  
  <style>
    .button-group span{
      display: none;
    }
    @media screen and (min-width: 768px) {
      .button-group span{
        display: block;
      }
    }
    .reservation-container {
      width: 100%;
      height: calc(100vh - 140px);
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .loading-state {
      height: 100%;
      display: flex;
      flex-direction: column;
      justify-content: center;
      align-items: center;
      color: #666;
    }

    .loading-spinner {
      width: 40px;
      height: 40px;
      border: 4px solid #f3f3f3;
      border-top: 4px solid #fb9040;
      border-radius: 50%;
      animation: spin 1s linear infinite;
      margin-bottom: 10px;
    }

    @keyframes spin {
      0% { transform: rotate(0deg); }
      100% { transform: rotate(360deg); }
    }

    .error-state {
      padding: 20px;
      background-color: #fff5f5;
      border: 1px solid #ffcdd2;
      border-radius: 4px;
      color: #d32f2f;
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
      width: 100%;
    }

    .title-group {
      display: flex;
      align-items: center;
      gap: 16px;
    }

    .title-group h3 {
      margin: 0;
      font-size: 16px;
      font-weight: 600;
      color: #333;
    }

    .data-count {
      font-size: 13px;
      color: #666;
      background-color: #f5f5f5;
      padding: 4px 8px;
      border-radius: 4px;
    }

    .button-group {
      display: flex;
      gap: 12px;
      align-items: center;
    }
    .table-container {
      width: 100%;
      height: 100%;
      overflow-y: auto;
      overflow-x: auto;
    }

    .time-range-select {
      padding: 6px 8px;
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

    .export-button {
      background-color: white;
      color: #4a90e2;
      border: 1px solid #e0e4e8;
    }

    .export-button:hover {
      background-color: #f5f9ff;
      border-color: #4a90e2;
    }

    .add-button {
      background-color: #4a90e2;
      color: white;
    }

    .add-button:hover {
      background-color: #3a80d2;
    }

    .icon {
      fill: currentColor;
      width: 16px;
      height: 16px;
    }
  </style>
  