<script lang="ts">
	import { get, writable } from "svelte/store";
	import { goto } from "$app/navigation";
	import { calendar } from "../../biz/calendar";
	import type {
		Reservation,
		ReservationDTO,
		Station,
	} from "../../biz/types";
	import { onMount } from "svelte";
	import { repository } from "../../biz/database";
	import { modalStore } from "../../components/modalStore";
	import ReservationInfo from "../../components/ReservationInfo.svelte";
	import { load } from "@tauri-apps/plugin-store";
	import { convertFileSrc } from "@tauri-apps/api/core";
	import type { PageData } from "./$types";
	import { getGlobal } from "../../biz/globalStore";
	import { init } from "../../biz/operation";
	import { exists } from "@tauri-apps/plugin-fs";
    import { errorHandler } from "../../biz/errorHandler";
    import type { AppError } from "../../biz/errors";
    import Source from "../../components/Source.svelte";
    import { apiService } from "../../biz/apiService";

	let { data }: { data: PageData } = $props();
	let { stationId,date }:{stationId:string|null,date:string|null} = data;
	let isDisabled=$state(false);
	console.log(stationId);
	const mode=getGlobal("run_mode");
	// 使用calendar实例的store
	const currentMonth = calendar.currentMonth;
	let selectedDate = $derived(calendar.selectedDate);
	// 加载月度预约数据
	async function loadMonthData(): Promise<Reservation[]> {
		// 获取整月的预约数据
		let reservations:Reservation[]=[];
		try{
		if(mode==="page"){
			reservations=await apiService.get(`/reservations/station/${stationId}?month=${$currentMonth}`);
		}else{
			reservations=await repository.getReservationsByStationAndMonth(
				$currentMonth,
				parseInt(stationId as string),
			);
		}
		return reservations;

		}catch(e){
			errorHandler.handleError(e as AppError);
			return [];
		}
	}

	// 计算日历数据

	const daysInMonth = $derived(calendar.getDaysInMonth($currentMonth));
	const firstDayOfMonth = $derived(
		calendar.getFirstDayOfMonth($currentMonth),
	);
	const calendarDays = $derived(calendar.getCalendarDays($currentMonth));
	const monthDisplay = $derived(calendar.getMonthDisplay($currentMonth));
	const showModal = writable(false);

	let photoAvailable = $state(false);
	async function loadStationInfo(stationId: string,loadingIndicator:number,selectedDate:string): Promise<Station> {
		console.log("start to load StationInfo");
		let stationInfos:Station[];
		if(mode==="page"){
			 const station= await apiService.get(`/stations/${stationId}`);
			 stationInfos=[station];
		}else{
			 stationInfos= await repository.getStationById(
				parseInt(stationId),
			);
		}
		console.log(stationInfos);
		photoAvailable = await exists(getPhotoPath(stationInfos[0].photo_path));
		console.log(photoAvailable);
		//获取sevents并filter
		let sevents=[];
		if(mode==="page"){
			 const sevent=await apiService.get(`/sevents/station/${stationId}`);
			 sevents=[sevent];
		}else{
		 sevents=await repository.getSeventsByStationId(parseInt(stationId));
		}
		console.log(sevents);
		const filteredSevents=sevents.filter(s=>new Date(s.from_date)<=new Date(selectedDate)&&new Date(s.to_date)>=new Date(selectedDate));
		console.log(filteredSevents);
		stationInfos[0].status=filteredSevents.length===0?'in_service':filteredSevents[0].name;
		isDisabled=stationInfos[0].status!=='in_service';
		console.log(stationInfos);
		return stationInfos[0];
	}

	let loadingIndicator = $state(0);
	
	const handlePhotoPath = (path: string) => {
		return convertFileSrc(getPhotoPath(path));
	};
	const getPhotoPath = (path: string) => {
		if (path.includes(":")) {
			return path;
		} else {
			const remote_source = getGlobal("remote_source");
			return `${remote_source}\\station_pics\\${path}`;
		}
	};
	// Add keyboard event listener for day navigation
	const handleKeydown = (event: KeyboardEvent) => {
		// 如果modal正在显示，且不是在输入框内
		if ($modalStore.show) {
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
			calendar.changeMonth(-1);
		} else if (event.key === "ArrowRight") {
			calendar.changeMonth(1);
		}
	};

	onMount(() => {
		window.addEventListener("keydown", handleKeydown);
		return () => window.removeEventListener("keydown", handleKeydown);
	});

	const init_page=async()=>{
		const user=getGlobal("user");
		const tests=getGlobal("tests");
		const project_engineers=getGlobal("project_engineers");
		const test_engineers=getGlobal("testing_engineers");
		if(!user||!tests||!project_engineers||!test_engineers){
			await init();
		}
		await new Promise(resolve => setTimeout(resolve, 200));
	}
</script>

{#snippet station_badge(status:string)}
	{#if status === "in_service"}
		<span class="station-badge in_service">正常</span>
	{:else if status === "out_of_service"}
		<span class="station-badge out_of_service">停用</span>
	{:else if status === "maintenance"}
		<span class="station-badge maintenance">维护</span>
		{:else if status==='calibration'}
		<span class="station-badge calibration">校准</span>
		{:else}
		<span class="station-badge unknown">未知状态</span>
	{/if}
{/snippet}

<div class="container">
	{#await init_page()}
	<div class="loading-container">
		<div class="loading-spinner-wrapper">
		  <div class="loading-spinner"></div>
		</div>
		<span class="loading-text">加载中...</span>
	  </div>
	{:then _}
	<!-- 固定的顶部信息 -->
	<header class="station-info">
		<div class="header-content">
			<button
				aria-label="home"
				class="tooltip-container"
				onclick={() => goto(`/date?date=${$selectedDate}`)}
			>
				<span class="tooltip-bottom">返回工位列表</span>
				<svg
					class="home_svg"
					viewBox="0 0 1280 1024"
					version="1.1"
					xmlns="http://www.w3.org/2000/svg"
					width="200"
					height="200"
					><path
						d="M1271.872 986.624c10.944-9.344 17.6-15.04-26.368-198.656-76.288-317.888-378.816-523.008-717.504-553.6V0L0 410.048l528 410.048V585.792c219.52-16.64 412.352 2.496 541.44 141.184 63.808 68.48 140.608 204.16 159.04 244.096 2.56 5.632 7.424 16.064 19.008 20.032l14.016 4.48 10.368-8.96z"
					></path></svg
				>
			</button>
			
			{#await loadStationInfo(stationId || "1",loadingIndicator,$selectedDate)}
				<div class="loading-spinner">加载中...</div>
			{:then stationInfo}
				{#if stationInfo}
					<div class="station-details" class:disableStation={isDisabled}>
						<div class="station-name-container">
							<div class="station-name">
								{stationInfo.name ?? "Unknown"}
								{@render station_badge(stationInfo.status)}
							</div>
							<p class="station-description">
								{stationInfo.description ?? "Unknown"}
							</p>
						</div>
						{#if photoAvailable}
							<img
								src={handlePhotoPath(stationInfo.photo_path)}
								class="station-image"
								alt="station_pic"
							/>
						{:else}
							<svg
								class="image-placeholder"
								viewBox="0 0 24 24"
								fill="none"
								xmlns="http://www.w3.org/2000/svg"
							>
								<path
									d="M4 16L8.586 11.414C8.96106 11.0391 9.46967 10.8284 10 10.8284C10.5303 10.8284 11.0389 11.0391 11.414 11.414L16 16M14 14L15.586 12.414C15.9611 12.0391 16.4697 11.8284 17 11.8284C17.5303 11.8284 18.0389 12.0391 18.414 12.414L20 14M14 8H14.01M6 20H18C18.5304 20 19.0391 19.7893 19.4142 19.4142C19.7893 19.0391 20 18.5304 20 18V6C20 5.46957 19.7893 4.96086 19.4142 4.58579C19.0391 4.21071 18.5304 4 18 4H6C5.46957 4 4.96086 4.21071 4.58579 4.58579C4.21071 4.96086 4 5.46957 4 6V18C4 18.5304 4.21071 19.0391 4.58579 19.4142C4.96086 19.7893 5.46957 20 6 20Z"
									stroke-width="2"
									stroke-linecap="round"
									stroke-linejoin="round"
								/>
							</svg>
						{/if}
					</div>
				{/if}
			{/await}
			<button
      class="tooltip-container"
      onclick={() => modalStore.open(Source, { onNegative: () => modalStore.close() })}
      aria-label="about"
    >
      <span class="tooltip">数据源设置</span>
      <svg class="logo" style="width: 30px;height: 30px" fill="#fbc400" viewBox="0 0 1024 1024" version="1.1" xmlns="http://www.w3.org/2000/svg" width="200" height="200"><path d="M369.777778 455.111111h284.444444c17.066667 0 28.444444 11.377778 28.444445 28.444445s-11.377778 28.444444-28.444445 28.444444h-284.444444c-17.066667 0-28.444444-11.377778-28.444445-28.444444s11.377778-28.444444 28.444445-28.444445z"></path><path d="M56.888889 483.555556C56.888889 625.777778 170.666667 739.555556 312.888889 739.555556H398.222222v-56.888889H312.888889C204.8 682.666667 113.777778 591.644444 113.777778 483.555556S204.8 284.444444 312.888889 284.444444H398.222222V227.555556H312.888889C170.666667 227.555556 56.888889 341.333333 56.888889 483.555556zM711.111111 227.555556H625.777778v56.888888h85.333333C819.2 284.444444 910.222222 375.466667 910.222222 483.555556S819.2 682.666667 711.111111 682.666667H625.777778v56.888889h85.333333C853.333333 739.555556 967.111111 625.777778 967.111111 483.555556S853.333333 227.555556 711.111111 227.555556z"></path></svg>
    </button>
		</div>
	</header>

	<!-- 日历部分 -->
	<div class="calendar-container">
		{#await loadMonthData()}
		<div class="loading-container">
			<div class="loading-spinner-wrapper">
			  <div class="loading-spinner"></div>
			</div>
			<span class="loading-text">加载中...</span>
		  </div>
		{:then monthlyReservations}
			<div class="month-nav">
				<button
					class="tooltip-container"
					aria-label="previous_month"
					onclick={() => calendar.changeMonth(-1)}
				>
					<span class="tooltip">上个月</span>
					<svg
						class="logo"
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
			<div class="calendar" class:disableStation={isDisabled}>
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
							class="day {calendar.isToday(date) ? 'today' : ''}"
							class:selectedDay={$selectedDate === date}
							onclick={e=>{
								e.preventDefault();
								e.stopPropagation();
								calendar.setDate(date);
							}}
						>
							{#await new Promise<Reservation[]>( (resolve) => resolve(monthlyReservations.filter((f) => f.reservation_date === date)), ) then reservations}
								<div
									style="display: flex;flex-direction:column;"
								>
									{#each ["T1", "T2", "T3", "T4", "T5"] as t}
										{@const reservation =
											reservations.filter(
												(f) =>
													f.time_slot === t,
											)}
										<div
											onclick={async (e) => {
												if (isDisabled) {
													return;
												}
												e.preventDefault();
												e.stopPropagation();
												calendar.setDate(
													date,
												);
												console.log($selectedDate)
												console.log('Opening modal with reservation:', reservation[0]);
												if (reservation.length > 0) {
													console.log(reservation[0]);
													modalStore.open(
														ReservationInfo,
														{
															reservation:
																reservation[0],
														},
													);
												}else{
													errorHandler.showInfo('无预约');
												}
											}}
											class="slot tooltip-container"
											class:fill_slot={reservation.length > 0}
										>
											{#if reservation.length > 0}<span
													class="tooltip"
													>{reservation[0]?.project_engineer}</span
												>{/if}
										</div>
									{/each}
								</div>
							{/await}
							<div class="day_no" style="z-index: 0;">
								{day}
							</div>
						</div>
					{/each}

					{#each calendar.getNextMonthDays($currentMonth) as day}
						<div class="day empty other-month">{day}</div>
					{/each}
				</div>
			</div>
		{/await}
	</div>
	{:catch error}
	<div>{"Error: " + error.message}</div>
	{/await}
</div>

<style>
	.station-badge{
    padding: 0.25rem 1rem;
    border-radius: 0.25rem;
    font-size: 0.8rem;
    font-weight: 600;
    flex-shrink: 0;
	}
	.in_service{
		background-color: #5cbeb4;
    color: #191b19;
	}
	.out_of_service{
		background-color: #e2e8f0;
		color: #a01a27;
	}
	.maintenance{
		background-color: #fbc400;
		color: #191b19;
	}
	.calibration{
		background-color: #0d3670;
		color: #faf5ff;
	}
	.disableStation{
		border: 2px solid #ce6868;
	}
	.container {
		display: flex;
		flex-direction: column;
		min-height: 100vh;
		background-color: #f0f2f5;
		width: 100%;
	}
	.home_svg {
		fill: #fbc400;
		width: 2.5rem;
		height: 2.5rem;
	}
	.station-description {
		font-size: 0.8rem;
		color: #94a3b8;
		font-family: Arial, Helvetica, sans-serif;
		width: 90%;
	}
	.station-info {
		background: white;
		width: 100%;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
		border-radius: 12px;
		transition: all 0.3s ease;
		display: flex;
		justify-content: center;
		align-items: center;
		padding: 0 0.5rem;
		box-sizing: border-box; /* 确保padding计入总宽度 */
	}
	.station-info button {
		background: none;
		/* border: 1px solid #e0e0e0; */
		border: none;
		outline: none;
		padding: 6px 8px;
		border-radius: 6px;
		color: #666;
		cursor: pointer;
		transition: all 0.3s ease;
	}

	.station-info button:hover {
		background-color: rgba(251, 196, 0, 0.1);
		transform: translateY(-2px);
		box-shadow: 0 2px 8px rgba(251, 196, 0, 0.2);
		color: #333;
		transform: translateY(-1px);
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
	}
	.logo {
		height: 25px;
		width: 25px;
		fill: #fbc400;
	}
	.station-info .header-content {
		/* max-width: 1200px; */
		margin: 0 auto;
		display: flex;
		justify-content: space-between;
		align-items: center;
		width: 100%;
		box-sizing: border-box;
		padding: 0.5rem 0.5rem;
	}
	.station-name-container {
		padding: 0 0rem;
	}
	.station-details {
		flex: 1;
		display: flex;
		justify-content: space-between;
		align-items: center;
		width: 100%;
		max-width: 1200px;
		margin: 0 1rem;
		background:rgb(249, 249, 249);
		border-radius: 12px;
		padding: 0 1rem;
	}
	.station-image {
		width: 100px;
		height: 100px;
		border-radius: 12px;
		margin-right: 4rem;
		fill: #fbc400;
	}
	.image-placeholder {
		width: 100px;
		height: 100px;
		border-radius: 12px;
		margin-right: 4rem;
		stroke: #837e6a;
	}
	.calendar-container {
		padding: 0;
		flex: 1;
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		overflow-y: hidden; /* 防止出现滚动条 */
	}
	.tooltip-container {
		position: relative;
		z-index: 5000;
	}

	.tooltip {
		position: absolute;
		right: 50%;
		bottom: 100%;
		transform: translateX(50%);
		background-color: rgba(0, 0, 0, 0.8);
		color: white;
		padding: 6px 12px;
		border-radius: 4px;
		font-size: 0.85rem;
		white-space: nowrap;
		opacity: 0;
		visibility: hidden;
		transition: all 0.3s ease;
		z-index: 5000;
		margin-bottom: 10px;
	}

	/* 修改小三角形的位置 */
	.tooltip::before {
		content: "";
		position: absolute;
		bottom: -4px;
		left: 50%;
		transform: translateX(-50%) rotate(45deg);
		width: 8px;
		height: 8px;
		background-color: rgba(0, 0, 0, 0.8);
		z-index: 5000;
	}

	.tooltip-container:hover .tooltip {
		opacity: 1;
		visibility: visible;
		transform: translateX(50%) translateY(0%);
	}
	.tooltip-bottom {
		position: absolute;
		left: -120%;
		top: 100%;
		transform: translateX(50%);
		background-color: rgba(0, 0, 0, 0.8);
		color: white;
		padding: 6px 12px;
		border-radius: 4px;
		font-size: 0.85rem;
		white-space: nowrap;
		opacity: 0;
		visibility: hidden;
		transition: all 0.3s ease;
		z-index: 5000;
		margin-bottom: 10px;
	}
	.tooltip-bottom::before {
		content: "";
		position: absolute;
		top: -4px;
		left: 50%;
		transform: translateX(-50%) rotate(45deg);
		width: 8px;
		height: 8px;
		background-color: rgba(0, 0, 0, 0.8);
		z-index: 5000;
	}
	.tooltip-container:hover .tooltip-bottom {
		opacity: 1;
		visibility: visible;
		transform: translateX(50%) translateY(0%);
	}
	.tooltip-right {
		position: absolute;
		right: 50%;
		top: 100%;
		transform: translateX(50%);
		background-color: rgba(0, 0, 0, 0.8);
		color: white;
		padding: 6px 12px;
		border-radius: 4px;
		font-size: 0.85rem;
		white-space: nowrap;
		opacity: 0;
		visibility: hidden;
		transition: all 0.3s ease;
		z-index: 5000;
		margin-bottom: 10px;
	}
	.tooltip-right::before {
		content: "";
		position: absolute;
		top: -4px;
		left: 50%;
		transform: translateX(-50%) rotate(45deg);
		width: 8px;
		height: 8px;
		background-color: rgba(0, 0, 0, 0.8);
		z-index: 5000;
	}
	.tooltip-container:hover .tooltip-right {
		opacity: 1;
		visibility: visible;
		transform: translateX(50%) translateY(0%);
	}
	.month-nav {
		display: flex;
		justify-content: center;
		align-items: center;
		gap: 2rem;
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
	.month-nav input::-webkit-calendar-picker-indicator {
		filter: invert(70%) sepia(70%) saturate(1000%) hue-rotate(360deg);
		cursor: pointer;
		width: 1.5rem;
		height: 1.5rem;
		padding: 0px;
	}
	.calendar {
		width: min(90vw, 800px); /* 日历最大宽度限制 */
		aspect-ratio: 7/6; /* 保持日历的宽高比 */
		max-height: 90%;
		background-color: white;
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
		position: relative;
	}
	.day_no {
		position: absolute;
		left: 50%;
		top: 50%;
		transform: translate(-50%, -50%);
		color: #94a3b8;
		font-size: 1.5rem;
		opacity: 0.8;
	}
	.slot {
		font-size: 0.8rem;
		width: 100%;
		min-width: 4rem;
		max-width: 5rem;
		min-height: 0.8rem;
		border: 1px solid #e0e0e0;
		border-radius: 4px;
		margin: 2px 0;
		text-align: center;
		opacity: 0.8;
	}
	.slot:hover{
		background-color: #f8f9fa;
		transform: translateY(-2px);
		box-shadow: 0 3px 6px rgba(0, 0, 0, 0.08);
		border-color: #e5e7eb;
		color: #1a1a1a;
		z-index: 10;
	}
	.fill_slot {
		background-color: #bc42b0;
	}
	.day:hover {
		background-color: #f8f9fa;
		transform: translateY(-2px);
		box-shadow: 0 3px 6px rgba(0, 0, 0, 0.08);
		border-color: #e5e7eb;
		color: #1a1a1a;
		z-index: 10;
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
	.selectedDay {
		border: 2px solid #fbc400;
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
	.logo {
		width: 2rem;
		height: 2rem;
	}

	/* 确保页面根元素也不会出现滚动条 */
	:global(body) {
		margin: 0;
		padding: 0;
		overflow: hidden;
		width: 100%;
	}

	:global(html) {
		overflow: hidden;
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
    border-top: 4px solid #fbc400;
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
