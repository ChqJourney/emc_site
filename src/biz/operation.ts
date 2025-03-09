import { modalStore } from "../components/modalStore";
import type { AppError } from "../biz/errors";
import type { Authorization, Reservation, ReservationDTO, Station, User, Visiting } from "../types/interfaces";
import { errorHandler } from "./errorHandler";
import { confirm, message, open } from "@tauri-apps/plugin-dialog";
import { getGlobal, setGlobal } from "./globalStore";
import { load, Store } from "@tauri-apps/plugin-store";
import { exists, readTextFile } from "@tauri-apps/plugin-fs";
import { getCurrentWindow } from "@tauri-apps/api/window";
import { invoke } from "@tauri-apps/api/core";
// import { AuthUtils } from "./auth";


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

const check_database_available = async (remote_source: string) => {
  const result = await invoke("check_db");
  console.log("result", result)
  if (result === 'OK') {
    return true
  }
  return false
}
const check_settings_available = async (remote_source: string) => {
  const settingConnected = await exists(`${remote_source}\\settings.json`);
  if (settingConnected) {
    const setting_okay = await init_settings(remote_source);
    if (!setting_okay) {
      return false;
    }
    return true;
  } else {
    errorHandler.showError("远程设置文件不存在");
    return false;
  }
}
const init_settings = async (remote_source: string) => {
  try {

    const settings = await readTextFile(`${remote_source}\\settings.json`);
    const config = JSON.parse(settings);

    if (config.tests && config.project_engineers && config.testing_engineers && config.station_orders) {

      setGlobal("tests", config.tests);
      setGlobal("project_engineers", config.project_engineers);
      setGlobal("testing_engineers", config.testing_engineers);
      setGlobal("station_orders", config.station_orders);
      setGlobal("loadSetting", config.loadSetting);
      return true;
    }
    errorHandler.showError("设置文件信息不完整");
    return false;
  } catch (e) {
    errorHandler.showError("初始化设置文件出错：" + e);
    return false;
  }

}

const check_if_user_okay = async (remote_source: string) => {
  const u: string = await invoke("check_user");
  try {

    const currentUser: User = JSON.parse(u);
    const encryptedContent = await readTextFile(`${remote_source}\\auth.json`);
    const authContent: Authorization[] = await AuthUtils.decryptFile(encryptedContent);
    const userAllowable = authContent?.find((a: Authorization) => a.username.toLowerCase() === currentUser.user.toLowerCase() && a.machinename.toLowerCase() === currentUser.machine.toLowerCase());
    console.log(userAllowable)
    if (userAllowable) {
      setGlobal("user", { ...currentUser, role: userAllowable.role, fullname: userAllowable.englishname });
      return true;
    }
    return false;
  } catch (e) {
    errorHandler.showError("检查用户授权信息出错：" + e);
    return false;
  }
}
const set_remote_source=async()=>{
  const init_result = await confirm("请检查下面内容：\n 1.远程数据源当前不可用，或未设置\n 2.你未被授权\n 请重新选择远程数据源或退出", {
    title: "错误",
    kind: "warning",
  });
  if (!init_result) {
    await message("没有选择远程数据源或者远程数据源暂时不可用,无法使用本软件，软件将退出！");
    const window = await getCurrentWindow();
    await window.close();
    return;
  }
  const result = await open({
    title: "远程数据源不可用或未设置，请选择远程数据源或退出",
    directory: true,
  });
  if (result) {
    const store = await Store.load("settings.json");
    await store.set("remote_source", result);
    await message("已设置远程数据源,请重启软件已生效！");
  } else {
    await message("没有选择远程数据源或者远程数据源暂时不可用,无法使用本软件，软件将退出！");
  }
  const window = await getCurrentWindow();
  await window.close();
  return;
}
export const init = async () => {
  // 1. load settings.json from appData folder under current windows user
  const store = await Store.load("settings.json");
  // 2. load remote_source directory from settings.json
  const remote_source: string | undefined =
    await store.get<string>("remote_source");
  console.log(remote_source);
  if (!remote_source) {
    errorHandler.showError("无设置，请设置远程数据源");
    // await set_remote_source();
    return;
  }
  //3. check database
  const db_ok = await check_database_available(remote_source as string);
  if (!db_ok) {
    errorHandler.showError("数据库不可用");
  }
  console.log("db_ok", db_ok)
  // 4. check settings
  const setting_ok = await check_settings_available(remote_source as string);
  if (!setting_ok) {
    errorHandler.showError("设置不可用");
  }
  console.log("setting_ok", setting_ok)
  // 5. check user
  const user_ok = await check_if_user_okay(remote_source as string);
  if (!user_ok) {
    errorHandler.showError("用户不可用");
  }
  console.log("user_ok", user_ok)

  if (!db_ok || !setting_ok || !user_ok) {
    // await set_remote_source();
    console.log('return')
    return;
  }
  setGlobal("remote_source", remote_source);
  console.log('start to log visiting')
  logVisiting(getGlobal("user"));
  // 6. load station_orders in local appData
  const station_order: { id: number, seq: number }[] | undefined = await store.get<{ id: number, seq: number }[]>("station_orders");
  if (station_order) {
    setGlobal("station_orders", station_order);
  }
};

export const reservationBlockClickPrecheck = (reservation_date: string, current_user?: User, project_engineer?: string, reservate_by?: string) => {
  //当date转换成日期后，在今天之前，不允许预约。如果正好是今天，允许预约

  const today = new Date().toISOString().split('T')[0];
  // 如果正好是今天，允许预约
  if (reservation_date < today) {
    errorHandler.showWarning("不能创建过去的预约");
    return false;
  }
  
  if(!current_user){
    return true;
  }
  // 当项目工程师不是当前工程师时，不允许修改预约
  if (reservate_by === current_user?.user) {
    errorHandler.showWarning("该预约由你创建，允许修改");
    return true;
  }
  // 当项目工程师不是当前工程师时，不允许修改预约
  if (project_engineer && (project_engineer !== current_user?.user||reservate_by===current_user?.user)) {
    errorHandler.showWarning("不属于你的预约或不是由你创建，无法修改");
    return false;
  }

  return true;
}
export const recordTestsFrequency = async (currentReservation: Reservation) => {
  try {

    const currentTestFrequency: Record<string, number> = getGlobal("test_frequency");

    const newTestFrequency = currentReservation.tests.split(";").reduce((acc: Record<string, number>, test: string) => {
      return currentTestFrequency ? {
        ...acc,
        [`${currentReservation.station_id}_${test}`]: (currentTestFrequency.hasOwnProperty(`${currentReservation.station_id}_${test}`) ? currentTestFrequency[`${currentReservation.station_id}_${test}`] : 0) + 1,
      } : {
        ...acc,
        [`${currentReservation.station_id}_${test}`]: 1
      };
    }, {});
    setGlobal("test_frequency", { ...currentTestFrequency, ...newTestFrequency });
    const store = await Store.load("settings.json");
    await store.set("test_frequency", { ...currentTestFrequency, ...newTestFrequency });
  } catch (e) {
    errorHandler.handleError(e as AppError);
  }
}
export const getTestFrequency = async (init_tests: string[], station_id: number) => {
  const store = await Store.load("settings.json");
  const testFrequencies = await store.get<Record<string, number>>("test_frequency");
  setGlobal("test_frequency", testFrequencies);
  const freqs = getGlobal("test_frequency");
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
export const submitReservation = async (reservation: Reservation, isCreate: boolean) => {
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
      modalStore.close();
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




