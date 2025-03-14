import { hideModal, modalStore, showModal } from "../components/modalStore";
import type { AppError } from "../biz/errors";
import type { Authorization, Reservation, ReservationDTO, Station, User, Visiting } from "../types/interfaces";
import { errorHandler } from "./errorHandler";
import { confirm, message, open } from "@tauri-apps/plugin-dialog";
import { getGlobal, setGlobal } from "./globalStore";
import { load, Store } from "@tauri-apps/plugin-store";
import { exists, readTextFile } from "@tauri-apps/plugin-fs";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { invoke } from "@tauri-apps/api/core";
import { apiService } from "./apiService";
import ReservationForm from "../components/ReservationForm.svelte";
import ReservationInfo from "../components/ReservationInfo.svelte";



async function logVisiting(u: User) {
  console.log(u);
  const vistings = await invoke<Visiting[]>('get_visits_by_user_and_machine', { user: u.user, machine: u.machine });
  console.log(vistings);
  if (vistings.length > 0) {
    console.log("count+1")
    vistings[0].visit_count += 1;
    console.log(vistings[0])
    // vistings[0].last_visit_time = new Date().toISOString();
    await invoke('update_visting', { v: vistings[0] });
  } else {
    await invoke('create_visting', {
      v: {
        visit_user: u.user,
        visit_machine: u.machine,
        visit_count: 1,
        last_visit_time: new Date().toISOString()
      }
    });
  }
}

export const calendarPageInit=async()=>{
  if(!getGlobal("tests")||!getGlobal("project_engineers")||!getGlobal("testing_engineers")||!getGlobal("station_orders")||!getGlobal("loadSetting")){
  
    const settings=await apiService.Get("general/settings");
    if(settings){
      setGlobal("tests", settings.tests);
      setGlobal("project_engineers", settings.project_engineers);
      setGlobal("testing_engineers", settings.testing_engineers);
      setGlobal("station_orders", settings.station_orders);
      setGlobal("loadSetting", settings.loadSetting);
    }else{
      errorHandler.showError("获取设置失败，请检查网络连接");
    }
  }
  const ifTokenExisted=localStorage.getItem("accessToken");
  if(ifTokenExisted&&!getGlobal("user")){
    const user=await apiService.Post("/auth/me");
    console.log(user)
    if(user){
      setGlobal("user",user);
    }
  }
}
export const dayPageInit = async () => {
  if(!getGlobal("tests")||!getGlobal("project_engineers")||!getGlobal("testing_engineers")||!getGlobal("station_orders")||!getGlobal("loadSetting")){
  
    const settings=await apiService.Get("general/settings");
    if(settings){
      setGlobal("tests", settings.tests);
      setGlobal("project_engineers", settings.project_engineers);
      setGlobal("testing_engineers", settings.testing_engineers);
      setGlobal("station_orders", settings.station_orders);
      setGlobal("loadSetting", settings.loadSetting);
    }else{
      errorHandler.showError("获取设置失败，请检查网络连接");
    }
  }
  const ifTokenExisted=localStorage.getItem("accessToken");
  if(ifTokenExisted&&!getGlobal("user")){
    const user=await apiService.Post("/auth/me");
    if(user){
      console.log(user)
      setGlobal("user",user);
    }
  }
  if(!getGlobal("test_frequency")){

    const test_frequency=localStorage.getItem("test_frequency");
    if(test_frequency){
      setGlobal("test_frequency", JSON.parse(test_frequency));
    }
  }
};

export const settingPageInit = async () => {
  if(!getGlobal("user")||!getGlobal("tests")||!getGlobal("project_engineers")||!getGlobal("testing_engineers")||!getGlobal("station_orders")||!getGlobal("loadSetting")){
  
    const settings=await apiService.Get("general/settings");
    if(settings){
      setGlobal("tests", settings.tests);
      setGlobal("project_engineers", settings.project_engineers);
      setGlobal("testing_engineers", settings.testing_engineers);
      setGlobal("station_orders", settings.station_orders);
      setGlobal("loadSetting", settings.loadSetting);
    }else{
      errorHandler.showError("获取设置失败，请检查网络连接");
    }
  }
  const ifTokenExisted=localStorage.getItem("accessToken");
  if(ifTokenExisted&&!getGlobal("user")){
    const user=await apiService.Post("/auth/me");
    if(user){
      console.log(user)
      setGlobal("user",user);
    }else{
      errorHandler.showError("获取用户失败，请检查网络连接");
    }
  }
  if(!getGlobal("test_frequency")){

    const test_frequency=localStorage.getItem("test_frequency");
    if(test_frequency){
      setGlobal("test_frequency", JSON.parse(test_frequency));
    }
  }
};

export const recordTestsFrequency = async (currentReservation: Reservation) => {
  try {

    const currentTestFrequency: Record<string, number> = JSON.parse(localStorage.getItem("test_frequency") || "{}");

    const newTestFrequency = currentReservation.tests.split(";").reduce((acc: Record<string, number>, test: string) => {
      return currentTestFrequency ? {
        ...acc,
        [`${currentReservation.station_id}_${test}`]: (currentTestFrequency.hasOwnProperty(`${currentReservation.station_id}_${test}`) ? currentTestFrequency[`${currentReservation.station_id}_${test}`] : 0) + 1,
      } : {
        ...acc,
        [`${currentReservation.station_id}_${test}`]: 1
      };
    }, {});
    localStorage.setItem("test_frequency", JSON.stringify({ ...currentTestFrequency, ...newTestFrequency }));
  } catch (e) {
    errorHandler.handleError(e as AppError);
  }
}
export const getTestFrequency = async (init_tests: string[], station_id: number) => {
  const testFrequencies = JSON.parse(localStorage.getItem("test_frequency") || "{}");
  const freqs = testFrequencies;  
  console.log(freqs);
  const tests = testFrequencies ? [...init_tests].map(test => { return { name: test, most_used: freqs[`${station_id}_${test}`] ? true : false }; }).sort((a, b) => {
    const freqA = freqs[`${station_id}_${a.name}`] ?? 0;
    const freqB = freqs[`${station_id}_${b.name}`] ?? 0;
    return freqB - freqA;
  }) : [...init_tests].map(test => { return { name: test, most_used: 0 }; });
  return tests;
}
function fillEmptyReservationFields(reservation: Partial<ReservationDTO>): ReservationDTO {
  const now = new Date().toISOString();
  return {
    reservation_date: reservation.reservation_date || '',
    time_slot: reservation.time_slot || '',
    station_id: reservation.station_id || 0,
    client_name: reservation.client_name || '',
    product_name: reservation.product_name || '',
    contact_name: reservation.contact_name || '',
    contact_phone: reservation.contact_phone || '',
    tests: reservation.tests || '',
    job_no: reservation.job_no || '',
    purpose_description: reservation.purpose_description || '',
    sales: reservation.sales || '',
    project_engineer: reservation.project_engineer || '',
    testing_engineer: reservation.testing_engineer || '',
    reservate_by: reservation.reservate_by || '',
    reservation_status: reservation.reservation_status || 'normal'
  };
}
export const submitReservation = async (reservation: Reservation, isCreate: boolean,role:string) => {
  try {
    if (isCreate) {

      if (reservation.reservation_status === 'cancelled') {
        await invoke("delete_reservation", { id: reservation.id });
        return;
      }
      if (!isCreate) {
        await invoke("update_reservation", { reservation: reservation as Reservation });
        errorHandler.showInfo("修改已保存");
      } else {
        const filledReservation = fillEmptyReservationFields(reservation);
        await invoke("create_reservations", { reservation: filledReservation });
        errorHandler.showInfo("创建成功");
      }
      hideModal();
      // loadingIndicator++;
    }
  } catch (e) {
    errorHandler.handleError(e as AppError);
  }
}

export const deleteReservation = async (reservation: Reservation) => {
  if (
    await confirm("确定要删除这个预约吗？不可恢复", {
      title: "删除警告",
      kind: "warning",
    })
  ) {
    try {

      await invoke("delete_reservation", { id: reservation.id });
      errorHandler.showInfo("删除成功");
    } catch (error) {
      errorHandler.handleError(error as AppError);
    }
  }
}

/**
 * 检查用户是否可以编辑预约
 * @param reservationDate 预约日期
 * @param user 当前用户
 * @param projectEngineer 预约的项目工程师
 * @param reservateBy 预约创建者
 * @returns 是否允许编辑
 */
export const reservationBlockClickPrecheck = (reservationDate: string, user: User, projectEngineer: string, reservateBy: string): boolean => {
  if (!user) return false;
  
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const reserveDate = new Date(reservationDate);
  reserveDate.setHours(0, 0, 0, 0);
  
  // 检查日期是否为今天或未来
  const isCurrentOrFuture = reserveDate >= today;
  
  // 如果是管理员，可以编辑任何预约
  if (user.role === 'admin') return true;
  
  // 如果是工程师，只能编辑自己的预约，且只能编辑当天或未来的预约
  if (user.role === 'engineer') {
    const isOwnReservation = 
      projectEngineer === user.username || 
      reservateBy === user.username;
    
    return isCurrentOrFuture && isOwnReservation;
  }
  
  return false;
};

/**
 * 处理预约块点击事件
 * @param reservation 当前预约信息（如果存在）
 * @param station 工作站信息
 * @param selectedDate 选中的日期
 */
export const handleReservationBlockClick = (reservation: Reservation | null, station: Station, selectedDate: string) => {
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
        submitHandler: async () => {
          await submitReservation(reservation, false, user.role);
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
          reservation_date: selectedDate,
        } as Reservation,
        isSimpleMode: true,
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
    const reserveDate = new Date(selectedDate);
    reserveDate.setHours(0, 0, 0, 0);
    
    // 检查日期是否为今天或未来
    const isCurrentOrFuture = reserveDate >= today;
    
    if (reservation) {
      // 3.a 有预约
      // 检查条件A：是否是当前用户的预约
      const isOwnReservation = 
        reservation.project_engineer === user.username || 
        reservation.reservate_by === user.username;
      
      // 条件A和B都满足时，显示编辑表单
      if (isCurrentOrFuture && isOwnReservation) {
        showModal(ReservationForm as any, {
          item: reservation,
          isSimpleMode: true,
          submitHandler: async () => {
            await submitReservation(reservation, false, user.role);
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
            reservation_date: selectedDate,
          } as Reservation,
          isSimpleMode: true,
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




