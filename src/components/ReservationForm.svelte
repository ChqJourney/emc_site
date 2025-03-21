<script lang="ts">
    import type { Reservation, Station, User } from "../biz/types";

    import MultiSelect from "../components/MultiSelect.svelte";
    import { getGlobal, setGlobal } from "../biz/globalStore";
    import { errorHandler } from "../biz/errorHandler";
    import type { AppError } from "../biz/errors";
    import { hideModal } from "./modalStore";
    import {
        getTestFrequency,
        recordTestsFrequency,
    } from "../biz/localService";
    import SingleSelect from "./SingleSelect.svelte";
    import { apiService } from "../biz/apiService";
    import { onMount } from "svelte";
    import { tick } from "svelte";
    let { item, isSimpleMode, submitHandler, onNegative } = $props();
    // $inspect(reservation);
    let isCreate = item === undefined || item.id === undefined || item.id === 0;
    const initReservation = item;
    let stations: Station[] = $state([]);
    let isSubmitting = $state(false);
    let isLoading = $state(true);
    let isLoadingStationStatus = $state(false);
    let availableStations = $derived(
        stations.filter(
            (station) =>
                station.status === "in_service" ||
                station.status === "maintenance",
        ),
    );

    const initReservationDTO: Reservation = {
        id: 0,
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
    // 定义 config 的类型
    type ConfigType = {
        tests: Array<any>;
        project_engineers: Array<any>;
        test_engineers: Array<any>;
    };

    let config = $state<ConfigType>({
        tests: [],
        project_engineers: [],
        test_engineers: [],
    });
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
                "工位",
            );
            errorHandler.validateRequired(
                currentReservation.reservation_status,
                "预约状态",
            );
            errorHandler.validateDateElapsed(
                currentReservation.reservation_date,
                "日期",
            );

            errorHandler.validateRequired(
                currentReservation.time_slot,
                "时间段",
            );
            errorHandler.validateRequired(currentReservation.tests, "测试项目");
            errorHandler.validateRequired(
                currentReservation.project_engineer,
                "项目工程师",
            );
            errorHandler.validateRequired(
                currentReservation.testing_engineer,
                "测试工程师",
            );

            isSubmitting = true;
            const user = getGlobal("user");
            console.log(currentReservation.reservation_status, isCreate);
            if (
                currentReservation.reservation_status === "cancelled" &&
                !isCreate
            ) {
                const isCancelled = await confirm("确认取消预约?");
                if (isCancelled) {
                    await submitHandler(
                        currentReservation as Reservation,
                        isCreate,
                        user,
                    );
                    return;
                }
            } else {
                await recordTestsFrequency(currentReservation);
                console.log("start submit");

                if (typeof submitHandler === "function") {
                    await submitHandler(
                        currentReservation as Reservation,
                        isCreate,
                        user,
                    );
                } else {
                    console.error("submitHandler 不是一个函数");
                    errorHandler.showError("提交处理函数未定义");
                    return;
                }
            }
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
    let timeSlotsOptions = $state([
        { name: "9:30-12:00", value: "T1", isOccupied: false },
        { name: "13:00-15:00", value: "T2", isOccupied: false },
        { name: "15:00-17:30", value: "T3", isOccupied: false },
        { name: "18:00-20:30", value: "T4", isOccupied: false },
        { name: "20:30-23:59", value: "T5", isOccupied: false },
    ]);

    // 预加载基础数据函数
    async function loadBasicData() {
        try {
            // 使用 setGlobal 而不是直接修改对象属性
            const init_tests = getGlobal("tests") || [];
            const user = getGlobal("user");
            console.log(user);

            // 使用完整对象替换而不是直接修改属性
            // currentReservation = {
            //     ...currentReservation,
            //     reservate_by: user?.username ?? "unknown",
            // };

            const project_engineers = getGlobal("project_engineers") || [];
            const test_engineers = getGlobal("testing_engineers") || [];

            // 并行加载站点数据和测试频率数据
            const [stationsData, testsData] = await Promise.all([
                loadStations(),
                getTestFrequency(init_tests, currentReservation.station_id),
            ]);

            stations = stationsData || [];

            // 完整替换对象
            config = {
                tests: testsData,
                project_engineers,
                test_engineers,
            };

            isLoading = false;
            await tick(); // 确保UI更新

            // 加载完基础数据后，再加载时间槽状态
            await loadStationStatus();
        } catch (error) {
            console.error("初始化错误:", error);
            errorHandler.handleError(error as AppError);
            isLoading = false;
        }
    }

    // 加载站点状态数据
    async function loadStationStatus() {
        try {
            isLoadingStationStatus = true;
            console.log("加载站点状态...");
            const stationStatus = await apiService.Get(
                `/reservations/station_status/?id=${currentReservation.station_id}&date=${currentReservation.reservation_date}`,
            );
            console.log(stationStatus);

            // 创建新数组并完整替换
            const newTimeSlotsOptions = timeSlotsOptions.map((option) => {
                const isOccupied = stationStatus.some((status: any) =>
                    status.timeSlot.includes(option.value),
                );
                return { ...option, isOccupied };
            });

            console.log(newTimeSlotsOptions);
            timeSlotsOptions = [...newTimeSlotsOptions];
        } catch (error) {
            console.error("加载站点状态错误:", error);
            errorHandler.handleError(error as AppError);
        } finally {
            isLoadingStationStatus = false;
        }
    }

    // 当日期或站点改变时重新加载时间槽状态
    $effect(() => {
        if (
            !isLoading &&
            currentReservation.station_id &&
            currentReservation.reservation_date
        ) {
            loadStationStatus();
        }
    });

    // 使用onMount钩子触发数据加载
    onMount(() => {
        loadBasicData();
    });
</script>

<div class="modal-content">
    {#if isLoading}
        <div class="loading-container">
            <p>加载中...</p>
            <div class="loading-spinner"></div>
        </div>
    {:else}
        <h2>{isCreate ? "创建预约" : "修改预约"}</h2>
        <form onsubmit={handleSubmit}>
            <!-- 第一行：工位 -->
            <div class="form-row">
                <div class="form-group full-width">
                    <label for="station_id">工位*</label>
                    <select
                        disabled={isSimpleMode}
                        bind:value={currentReservation.station_id}
                    >
                        <option value="">请选择工位</option>
                        {#each availableStations as station}
                            <option
                                disabled={station.status === "maintenance"}
                                value={station.id}
                                >{station.name}{station.status === "maintenance"
                                    ? "(维护)"
                                    : ""}</option
                            >
                        {/each}
                    </select>
                </div>
                <div class="form-group full-width">
                    <label for="reservation_status">预约状态*</label>
                    <select bind:value={currentReservation.reservation_status}>
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
                        min={isCreate
                            ? new Date().toISOString().split("T")[0]
                            : null}
                    />
                </div>
                <div class="form-group">
                    <label for="time_slot">时间段*</label>
                    {#if isLoadingStationStatus}
                        <div class="mini-loader">加载时间槽...</div>
                    {:else}
                        <MultiSelect
                            isCreated={isCreate}
                            placeholder="请选择时间段"
                            options={timeSlotsOptions}
                            selected={currentReservation.time_slot
                                ? currentReservation.time_slot
                                      .split(";")
                                      .filter((value: string) => value)
                                      .map((value: string) => ({
                                          value,
                                          name: reverseTimeSlots[value],
                                      }))
                                : []}
                            onChange={(selectedValues) => {
                                currentReservation = {
                                    ...currentReservation,
                                    time_slot: selectedValues
                                        .map((s: any) => s.value)
                                        .sort((a: any, b: any) =>
                                            a.localeCompare(b),
                                        )
                                        .join(";"),
                                };
                            }}
                        />
                    {/if}
                </div>
            </div>

            <!-- 第三行：测试项目 -->
            <div class="form-row">
                <div class="form-group full-width">
                    <label for="tests">测试项目*</label>
                    <MultiSelect
                        placeholder="请选择测试项目"
                        options={config.tests
                            ? config.tests?.map((testStr: any) => {
                                  return { ...testStr, value: testStr.name };
                              })
                            : []}
                        selected={currentReservation.tests
                            ? currentReservation.tests
                                  .split(";")
                                  .filter((name: string) => name)
                                  .map((name: string) => ({
                                      name,
                                      value: name,
                                  }))
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
                        options={config.project_engineers || []}
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
                        options={config.test_engineers || []}
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
                    <input type="text" bind:value={currentReservation.job_no} />
                </div>
                <div class="form-group full-width">
                    <label for="sales">销售</label>
                    <input type="text" bind:value={currentReservation.sales} />
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
                            value={currentReservation.created_on || ""}
                        />
                    </div>
                    <div class="form-group full-width">
                        <label for="updated_at">更新时间</label>
                        <input
                            disabled
                            type="text"
                            value={typeof currentReservation.updated_on ===
                            "object"
                                ? currentReservation.updated_on
                                      .toISOString()
                                      .substring(0, 19)
                                : ""}
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
                    <button type="button" class="cancel" onclick={onNegative}
                        >取消</button
                    >
                    <button
                        type="submit"
                        class="submit"
                        disabled={isSubmitting}
                    >
                        {isSubmitting ? "提交中..." : "提交"}
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
        padding-bottom: calc(4rem + 80px); /* 增加底部padding，为固定按钮组留出空间 */
        -webkit-overflow-scrolling: touch;
    }

    h2 {
        margin: 0 0 1.5rem;
        color: #2c3e50;
        font-weight: 500;
        position: sticky;
        top: 0;
        background: white;
        padding: 1rem 0;
        z-index: 1;
    }

    .form-row {
        display: flex;
        gap: 1rem;
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
        width: 100%;
        padding: 0.75rem;
        border: 1px solid #e2e8f0;
        border-radius: 4px;
        font-size: 1rem;
        background: white;
        transition: border-color 0.2s;
        box-sizing: border-box;
    }

    input[type="date"] {
        padding: 0.75rem;
        width: 100%;
    }

    input:focus,
    select:focus,
    textarea:focus {
        outline: none;
        border-color: #4299e1;
        box-shadow: 0 0 0 2px rgba(66, 153, 225, 0.2);
    }

    .button-group {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        margin-top: 1rem;
        position: fixed; /* 改为 fixed 定位 */
        bottom: 0;
        left: 0;
        right: 0;
        background: white;
        padding: 1rem;
        z-index: 10;
        box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.1);
    }
    form{
            padding-bottom: 20px;
        }
    .button-group button {
        padding: 0.75rem 1.5rem;
        border: none;
        border-radius: 4px;
        cursor: pointer;
        font-size: 1rem;
    }

    .button-group .cancel {
        background: #f1f1f1;
        color: #333;
    }

    .button-group .submit {
        background: #4a90e2;
        color: white;
        min-width: 100px;
    }

    .button-group .submit:hover {
        background: #357abd;
    }

    .loading-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        height: 200px;
    }

    .loading-spinner {
        width: 40px;
        height: 40px;
        border: 3px solid #f3f3f3;
        border-top: 3px solid #4a90e2;
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin-top: 10px;
    }

    .mini-loader {
        font-size: 0.8rem;
        color: #666;
        padding: 8px;
        display: flex;
        align-items: center;
    }

    @keyframes spin {
        0% {
            transform: rotate(0deg);
        }
        100% {
            transform: rotate(360deg);
        }
    }

    /* 移动设备适配 */
    @media screen and (max-width: 768px) {
        .modal-content {
            padding: 0 0.5rem;
            max-height: 100vh;
            padding-bottom: calc(5rem + 20px); /* 移动端增加更多底部空间 */
        }

        .form-row {
            flex-direction: column;
            gap: 1rem;
            margin-bottom: 1rem;
        }
        form{
            padding-bottom: 120px;
        }
        .form-group {
            width: 100%;
            margin-right: 0;
        }

        .form-group:not(:last-child) {
            margin-right: 0;
        }

        input,
        select,
        textarea {
            width: 100%;
            font-size: 16px; /* 防止 iOS 自动缩放 */
        }

        .button-group {
            padding: 1rem 0.5rem;
            background: rgba(255, 255, 255, 0.98);
            backdrop-filter: blur(10px);
        }

        .button-group button {
            padding: 0.75rem 1rem;
            flex: 1;
            min-height: 44px; /* 确保移动端按钮有足够的触摸区域 */
        }

        h2 {
            padding: 1rem 0.5rem;
            margin-bottom: 1rem;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        }
    }
</style>
