<script lang="ts">
    import type {
        Reservation,
        Station
    } from "../biz/types";

    import MultiSelect from "../components/MultiSelect.svelte";
    import { getGlobal, setGlobal } from "../biz/globalStore";
    import { errorHandler } from "../biz/errorHandler";
    import type { AppError } from "../biz/errors";
    import { Store } from "@tauri-apps/plugin-store";
    // import { getTestFrequency, recordTestsFrequency } from "../biz/operation";
    import { confirm } from "@tauri-apps/plugin-dialog";
    import { invoke } from "@tauri-apps/api/core";
    import { hideModal } from "./modalStore";
    import { getTestFrequency, recordTestsFrequency } from "../biz/operation";
    import SingleSelect from "./SingleSelect.svelte";
    import { apiService } from "../biz/apiService";
    import { onMount } from "svelte";
   let {item,isSimpleMode,submitHandler,onNegative}:{item:Reservation,isSimpleMode:boolean,submitHandler:(reservation:Reservation,isCreate:boolean)=>Promise<void>,onNegative:()=>void}= $props();
    // $inspect(reservation);
    let isCreate=item===undefined||item.id===undefined||item.id===0
    const initReservation = item;
    console.log(initReservation)
    let stations: Station[] = $state([]);
    let isSubmitting = $state(false);
    let isLoading = $state(true);
    let availableStations = $derived(
        stations.filter((station) => station.status === "in_service" || station.status === "maintenance")
    );
    
    const initReservationDTO: Reservation = {
        id:0,
        reservation_date: `${new Date().toISOString().split("T")[0]}`,
        time_slot: ``,
        client_name: ``,
        product_name: ``,
        station_id: 1,
        reservation_status: "normal",
        reservate_by: "",
        contact_name: "",
        contact_phone: "",
        purpose_description: "",
        sales: "",
        project_engineer: "",
        testing_engineer: "",
        tests: "",
        job_no: "",
        created_on: new Date(),
        updated_on: new Date(),
    };
    let config = $state({});
    let currentReservation = $state(initReservation ?? initReservationDTO);
    let modalExtraInfoShow = $state(false);
    async function loadStations() {
        try {
            const stations = await apiService.Get("/stations");
            return stations;
        } catch (error) {
            errorHandler.handleError(error as AppError);
        } 
    }
    const handleSubmit = async () => {
        console.log(currentReservation);
        try {
            errorHandler.validateRequired(
                currentReservation.station_id,
                "工位"
            );
            errorHandler.validateRequired(
                currentReservation.reservation_status,
                "预约状态"
            );
            errorHandler.validateDateElapsed(
                currentReservation.reservation_date,
                "日期"
            );

            errorHandler.validateRequired(
                currentReservation.time_slot,
                "时间段"
            );
            errorHandler.validateRequired(
                currentReservation.tests,
                "测试项目"
            );
            errorHandler.validateRequired(
                currentReservation.project_engineer,
                "项目工程师"
            );
            errorHandler.validateRequired(
                currentReservation.testing_engineer,
                "测试工程师"
            );

            isSubmitting = true;
            if(currentReservation.reservation_status==="cancelled"&&!isCreate){
                const isCancelled=await confirm("确认取消预约?",{title:"取消预约",kind:"warning"});
                if(!isCancelled){
                    return;
                }
            }
            await recordTestsFrequency(currentReservation);
            await submitHandler(currentReservation as Reservation,isCreate);
           hideModal();
        } catch (error) {
            console.error(error);
            errorHandler.handleError(error as AppError);
        } finally {
            isSubmitting = false;
        }
    };
    const reverseTimeSlots: Record<string, string> = {
        T1: "9:30-12:00",
        T2: "13:00-15:00",
        T3: "15:00-17:30",
        T4: "18:00-20:30",
        T5: "20:30-23:59",
    };
    let timeSlotsOptions = [
        { name: "9:30-12:00", value: "T1", isOccupied: false },
        { name: "13:00-15:00", value: "T2", isOccupied: false },
        { name: "15:00-17:30", value: "T3", isOccupied: false },
        { name: "18:00-20:30", value: "T4", isOccupied: false },
        { name: "20:30-23:59", value: "T5", isOccupied: false },
    ];
    const init = async () => {
        isLoading = true;
        const init_tests = getGlobal("tests")||[];
        const user = getGlobal("user");
        currentReservation = {
            ...currentReservation,
            reservate_by: user?.user,
        };
        const project_engineers = getGlobal("project_engineers");
        const test_engineers = getGlobal("testing_engineers");
        const tests=await getTestFrequency(init_tests,currentReservation.station_id);
        stations = await loadStations()||[];
        config = { tests, project_engineers, test_engineers };
        console.log("status")
        const stationStatus = await apiService.Get(`/reservations/station_status/?id=${currentReservation.station_id}&date=${currentReservation.reservation_date}`);
        console.log(stationStatus);
        const newTimeSlotsOptions = timeSlotsOptions.map((option: { name: string; value: string; isOccupied: boolean }) => {
            const isOccupied = stationStatus.some((status: { timeSlot: string; isOccupied: boolean }) => status.timeSlot.includes(option.value));
            return { ...option, isOccupied };
        });
        timeSlotsOptions = newTimeSlotsOptions;
        isLoading = false;

    };
   
   onMount(()=>{
    init();
   })
</script>   

    <div class="modal-content">
        <h2>{isCreate?"创建预约":"修改预约"}</h2>
        {#if isLoading}
            <p>加载中...</p>
        {:else}
            <form onsubmit={handleSubmit}>
                <!-- 第一行：工位 -->
                <div class="form-row">
                    <div class="form-group full-width">
                        <label for="station_id">工位*</label>
                        <select  disabled={isSimpleMode}
                            bind:value={currentReservation.station_id}
                        >
                            <option value="">请选择工位</option>
                            {#each availableStations as station}
                                <option
                                    disabled={station.status === "maintenance"}
                                    value={station.id}
                                    >{station.name}{station.status === "maintenance" ? "(维护)" : ""}</option
                                >
                            {/each}
                        </select>
                    </div>
                    <div class="form-group full-width">
                        <label for="reservation_status">预约状态*</label>
                        <select
                            bind:value={currentReservation.reservation_status}
                        >
                            <option value="normal">Normal</option>
                            {#if !isCreate}
                            <option value="cancelled">Cancel</option>
                            {/if}
                        </select>
                    </div>
                </div>

                <!-- 第二行：日期和时间段 -->
                <div class="form-row">
                    <div class="form-group">
                        <label for="reservation_date">日期*</label>
                        <input
                            type="date"
                            disabled={isSimpleMode}
                            bind:value={currentReservation.reservation_date}
                            min={isCreate ? new Date().toISOString().split('T')[0] : null}
                        />
                    </div>
                    <div class="form-group">
                        <label for="time_slot">时间段*</label>
                        <MultiSelect
                            isCreated={isCreate}
                            placeholder="请选择时间段"
                            options={timeSlotsOptions}
                            selected={currentReservation.time_slot
                                ? currentReservation.time_slot
                                      .split(";")
                                      .filter((value: string) => value)
                                      .map((value: string) => ({ value,name:reverseTimeSlots[value] }))
                                : []}
                            onChange={(selectedValues) => {
                                currentReservation = {
                                    ...currentReservation,
                                    time_slot: selectedValues
                                        .map(
                                            (s: any) => s.value,
                                        )
                                        .sort((a: any, b: any) =>
                                            a.localeCompare(b),
                                        )
                                        .join(";"),
                                };
                            }}
                        />
                    </div>
                </div>

                <!-- 第三行：测试项目 -->
                <div class="form-row">
                    <div class="form-group full-width">
                        <label for="tests">测试项目*</label>
                        <MultiSelect
                            placeholder="请选择测试项目"
                            options={config.tests
                                ? config.tests?.map(
                                      (testStr:any) => {
                                          return { ...testStr,value:testStr.name };
                                      },
                                  )
                                : []}
                            selected={currentReservation.tests
                                ? currentReservation.tests
                                      .split(";")
                                      .filter((name: string) => name)
                                      .map((name: string) => ({ name,value:name }))
                                : []}
                            onChange={(selectedNames) => {
                                currentReservation = {
                                    ...currentReservation,
                                    tests: selectedNames
                                        .map((s: any) => s.value)
                                        .sort((a: any, b: any) =>
                                            a.localeCompare(b),
                                        )
                                        .join(";"),
                                };
                            }}
                        />
                    </div>
                </div>

                <!-- 第四行：项目工程师和测试工程师 -->
                <div class="form-row">
                    <div class="form-group">
                        <label for="project_engineer">项目工程师*</label>
                        <SingleSelect
                        options={config.project_engineers||[]}
                        placeholder="请选择项目工程师"
                        value={currentReservation.project_engineer}
                        onSelect={(value: string) => {
                            currentReservation = {
                                ...currentReservation,
                                project_engineer: value,
                            };
                        }}
                        />
                    </div>
                    <div class="form-group">
                        <label for="testing_engineer">测试工程师*</label>
                        <SingleSelect
                        options={config.test_engineers||[]}
                        placeholder="请选择测试工程师"
                        value={currentReservation.testing_engineer}
                        onSelect={(value: string) => {
                            currentReservation = {
                                ...currentReservation,
                                testing_engineer: value,
                            };
                        }}
                        />
                    </div>
                </div>
                <!-- 第五行：客户名称 -->
                <div class="form-row">
                    <div class="form-group full-width">
                        <label for="client_name">客户名称</label>
                        <input
                            type="text"
                            bind:value={currentReservation.client_name}
                        />
                    </div>
                    <div class="form-group full-width">
                        <label for="product_name">产品名称</label>
                        <input
                            type="text"
                            bind:value={currentReservation.product_name}
                        />
                    </div>
                </div>
                
                <div class="form-row">
                    <div class="form-group full-width">
                        <label for="job_no">项目号</label>
                        <input
                        type="text"
                        bind:value={currentReservation.job_no}
                        />
                    </div>
                    <div class="form-group full-width">
                        <label for="sales">销售</label>
                        <input
                            type="text"
                            bind:value={currentReservation.sales}
                        />
                    </div>
                </div>
                {#if modalExtraInfoShow}
                <!-- 第六行：联系人和电话 -->
                <div class="form-row">
                    <div class="form-group">
                        <label for="contact_name">联系人</label>
                        <input
                            type="text" 
                            bind:value={currentReservation.contact_name}
                        />
                    </div>
                    <div class="form-group">
                        <label for="contact_phone">联系电话</label>
                        <input
                            type="tel"
                            bind:value={currentReservation.contact_phone}
                        />
                    </div>
                </div>
                    <div class="form-row">
                        <div class="form-group full-width">
                            <label for="purpose_description">用途描述</label>
                            <textarea
                                rows="3"
                                bind:value={currentReservation.purpose_description}
                            ></textarea>
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="form-group full-width">
                            <label for="reservate_by">创建人</label>
                            <input
                            disabled
                                type="text"
                                value={currentReservation.reservate_by}
                            />
                        </div>
                        <div class="form-group full-width">
                            <label for="id">Id</label>
                            <input
                            disabled
                                type="text"
                                value={currentReservation.id}
                            />
                        </div>
                    </div>
                    <div class="form-row">
                        <div class="form-group full-width">
                            <label for="created_at">创建时间</label>
                            <input
                            disabled
                                type="text"
                                value={currentReservation.created_On||""}
                            />
                        </div>
                        <div class="form-group full-width">
                            <label for="updated_at">更新时间</label>
                            <input
                            disabled
                                type="text"
                                value={currentReservation.updated_On?.substring(0,19)||""}
                            />
                        </div>
                    </div>
                {/if}
                <div class="button-group">
                    <button
                        type="button"
                        style="background-color: transparent;color:cadetblue;text-decoration: underline;"
                        onclick={(e) => {
                            e.preventDefault();
                            e.stopPropagation();
                            modalExtraInfoShow = !modalExtraInfoShow;
                        }}>{modalExtraInfoShow ? "隐藏" : "显示更多"}</button
                    >
                    <div>
                        <button
                            type="button"
                            class="cancel"
                            onclick={onNegative}>取消</button
                        >
                        <button type="submit" class="submit" disabled={isSubmitting}>
                            {isSubmitting ? '提交中...' : '提交'}
                        </button>
                    </div>
                </div>
            </form>
        {/if}
    </div>

<style>
    /* .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.5);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 1000;
    } */

    .modal-content {
        overflow-y: auto;
        padding: 0 1rem;
        width: 100%;
        max-height: 95vh;
        padding-bottom: 8rem; /* 底部多留些空间 */
    }

    h2 {
        margin: 0 0 1.5rem;
        color: #2c3e50;
        font-weight: 500;
    }

    .form-row {
        display: flex;
        gap: 2rem;
        margin-bottom: 1.5rem;
        justify-content: space-between;
    }

    .form-group {
        flex: 1;
        min-width: 0;
    }

    .form-group:not(:last-child) {
        margin-right: 1rem;
    }

    .full-width {
        width: 100%;
    }

    label {
        display: block;
        margin-bottom: 0.5rem;
        color: #4a5568;
        font-size: 0.9rem;
    }

    input,
    select,
    textarea {
        width: 95%;
        padding: 0.5rem;
        border: 1px solid #e2e8f0;
        border-radius: 4px;
        font-size: 0.9rem;
        background: white;
        transition: border-color 0.2s;
    }
    input[type="date"] {
        padding: 0.5rem;
        width: 95%;
    }

    input:focus,
    select:focus {
        outline: none;
        border-color: #4299e1;
        box-shadow: 0 0 0 2px rgba(66, 153, 225, 0.2);
    }

    .button-group {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        margin-top: 1rem;
    }

    .button-group button {
        padding: 0.5rem 1.5rem;
        border: none;
        border-radius: 4px;
        cursor: pointer;
    }

    .button-group .cancel {
        background: #f1f1f1;
        color: #333;
    }

    .button-group .submit {
        background: #4a90e2;
        color: white;
    }

    .button-group .submit:hover {
        background: #357abd;
    }
</style>
