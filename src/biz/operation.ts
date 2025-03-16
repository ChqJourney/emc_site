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



/**
 * 创建预约
 * @param reservation 预约信息
 * @param user 当前用户
 */
export const createReservation = async (reservation: Reservation,user:User) => {  
  const userRole = user.role.toLowerCase();
  
  // 管理员有全部权限
  if (userRole === "admin") {
    return await apiService.Post(`/reservations`, reservation);
  }
  
  // 工程师只能创建自己预约的或自己负责的项目
  if (userRole === "engineer") {
    const isOwner = reservation.reservate_by === user.username;
    const isProjectEngineer = reservation.project_engineer === user.englishname;
    
    if (isOwner || isProjectEngineer) {
      return await apiService.Post(`/reservations`, reservation);
    }
  }
}
/**
 * 修改预约
 * @param reservation 预约信息
 * @param user 当前用户
 */
export const updateReservation = async (reservation: Reservation, user: User) => {
  const userRole = user.role.toLowerCase();
  
  // 管理员有全部权限
  if (userRole === "admin") {
    return await apiService.Put(`/reservations`, reservation);
  }
  
  // 工程师只能修改自己预约的或自己负责的项目
  if (userRole === "engineer") {
    const isOwner = reservation.reservate_by === user.username;
    const isProjectEngineer = reservation.project_engineer === user.englishname;
    
    if (isOwner || isProjectEngineer) {
      return await apiService.Put(`/reservations`, reservation);
    }
    
    errorHandler.showError("您没有权限修改此预约");
    return;
  }
  
  // 其他角色无权限
  errorHandler.showError("您没有权限修改预约");
}
/**
 * 删除预约
 * @param reservation 预约信息
 * @param user 当前用户
 */
export const deleteReservation = async (reservation: Reservation,user:User) => {
  
  const userRole = user.role.toLowerCase();
  
  // 管理员有全部权限
  if (userRole === "admin") {
    return await apiService.Delete(`/reservations/${reservation.id}`);
  }
  
  // 工程师只能删除自己预约的或自己负责的项目
  if (userRole === "engineer") {
    const isOwner = reservation.reservate_by === user.username;
    const isProjectEngineer = reservation.project_engineer === user.englishname;
    
    if (isOwner || isProjectEngineer) {
      return await apiService.Delete(`/reservations/${reservation.id}`);
    }
    
    errorHandler.showError("您没有权限删除此预约");
    return;
  }
  
  // 其他角色无权限
  errorHandler.showError("您没有权限删除预约");
  
}


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





