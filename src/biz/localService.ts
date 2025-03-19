import { hideModal, showModal } from "../components/modalStore";
import { errorHandler } from "./errorHandler";
import { getGlobal } from "./globalStore";
import type { Reservation, ReservationDTO, Station, User } from "./types";
import ReservationForm from "../components/ReservationForm.svelte";
import ReservationInfo from "../components/ReservationInfo.svelte";
import { createReservation, deleteReservation, updateReservation } from "./operation";
import type { AppError } from "./errors";

/**
 * 填充预约字段
 * @param reservation 预约信息
 * @returns 填充后的预约信息
 */
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
  export const submitReservation = async (reservation: Reservation, isCreate: boolean, user: User) => {
    try {
        console.log('submitReservation', reservation, isCreate, user);
      if (reservation.reservation_status === 'cancelled' && !isCreate) {
        await deleteReservation(reservation, user);
        hideModal();
        return;
      }
      
      if (isCreate) {
        console.log("iscreate")
        const filledReservation = fillEmptyReservationFields(reservation);
        filledReservation.reservate_by=user.username
        console.log(filledReservation)
        const response=await createReservation(filledReservation, user);
        console.log(response)
        errorHandler.showInfo("创建成功");
      } else {
        await updateReservation(reservation, user);
        errorHandler.showInfo("修改已保存");
      }
      
      hideModal();
    } catch (e) {
      errorHandler.handleError(e as AppError);
    }
  }


/**
 * 记录每个工位测试的频率
 * @param currentReservation 当前预约信息
 */
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

 
  /**
 *  获取每个工位最常用的测试s
 * @param init_tests 所有测试
 * @param station_id 工位ID
 */
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