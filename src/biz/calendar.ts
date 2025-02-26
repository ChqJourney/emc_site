import { get, writable, type Writable } from 'svelte/store';
import type { Reservation } from '../biz/types';
import { getGlobal } from './globalStore';

export class Calendar {
    private static instance: Calendar;
    
    public currentMonth: Writable<string>;
    public selectedDate: Writable<string>;
    private constructor() {
        const today = new Date().toISOString().split('T')[0];
        this.currentMonth = writable(new Date().toISOString().split('-').slice(0,2).join('-'));
        this.selectedDate = writable(today); // 初始化为今天
    }

    public static getInstance(): Calendar {
        if (!Calendar.instance) {
            Calendar.instance = new Calendar();
        }
        return Calendar.instance;
    }

    // 改变月份
    public changeMonth(delta: number): void {
        const [year, month] = get(this.currentMonth).split('-').map(Number);
        const newDate = new Date(year, month - 1 + delta, 1);
        const newMonth = `${newDate.getFullYear()}-${(newDate.getMonth() + 1).toString().padStart(2, '0')}`;
        this.currentMonth.set(newMonth);
        
        // 更新选中日期到新月份的第一天
        this.selectedDate.set(`${newMonth}-01`);
    }

    // 获取当月天数
    public getDaysInMonth(yearMonth: string): number {
        const [year, month] = yearMonth.split('-').map(Number);
        // 通过设置下个月的第0天来获取当月最后一天
        return new Date(year, month, 0).getDate();
    }

    // 获取当月第一天是星期几
    public getFirstDayOfMonth(yearMonth: string): number {
        const [year, month] = yearMonth.split('-').map(Number);
        // 注意：这里月份要减1
        return new Date(year, month - 1, 1).getDay();
    }

    // 获取日历天数数组
    public getCalendarDays(yearMonth: string): number[] {
        const daysInMonth = this.getDaysInMonth(yearMonth);
        return Array.from({ length: daysInMonth }, (_, i) => i + 1);
    }

    // 获取某天的预约数量
    private getDayReservations(date: string, reservations: Reservation[]): Reservation[] {
        return reservations?.filter(r => r.reservation_date === date) ?? [];
    }

    // 获取某天的预约状态颜色
    public getDayColor(date: string, reservations: Reservation[]): string {
        const count = this.getDayReservations(date, reservations).length;
        const loadingSetting:{low_load:number,medium_load:number,high_load:number} = getGlobal("loadSetting")
        
        if (count === 0) return 'transparent';
        if (count <= loadingSetting.low_load) return `rgba(75, 192, 92, 0.${count * 2})`; // 绿色渐变
        if (count <= loadingSetting.medium_load) return `rgba(255, 205, 86, 0.${count * 2})`; // 黄色渐变
        if (count <= loadingSetting.high_load) return `rgba(255, 153, 0, 0.${count * 2})`; // 橙色渐变
        return 'rgba(255, 99, 132, 0.8)'; // 红色
    }

    // 获取每天的预约数量
    public getDayReservationCount(date: string, reservations: Reservation[]): number {
        return this.getDayReservations(date, reservations).length;
    }

    // 格式化日期
    public formatDate(yearMonth: string, day: number): string {
        return `${yearMonth}-${day.toString().padStart(2, '0')}`;
    }

    // 获取月份显示文本
    public getMonthDisplay(yearMonth: string): string {
        const [year, month] = yearMonth.split('-').map(Number);
        // 注意：这里月份要减1
        return new Date(year, month - 1).toLocaleString('zh-CN', { 
            year: 'numeric', 
            month: 'long' 
        });
    }

    // 新增方法：设置选中日期
    // 设置指定日期
    public setDate(date: string): void {
        this.selectedDate.set(date);
        // 如果月份变化了，同时更新currentMonth
        const newMonth = date.substring(0, 7);
        if (newMonth !== get(this.currentMonth)) {
            this.currentMonth.set(newMonth);
        }
    }
    // 改变日期（前一天/后一天）
    public change(delta: number): void {
        const currentDate = new Date(get(this.selectedDate));
        currentDate.setDate(currentDate.getDate() + delta);
        const newDate = currentDate.toISOString().split('T')[0];
        this.setDate(newDate);
    }
    // 前一天
    public previous(): void {
        this.change(-1);
    }

    // 后一天
    public next(): void {
        this.change(1);
    }
    getPreviousMonthDays(currentMonth: string): number[] {
        const [year, month] = currentMonth.split('-').map(Number);
        const firstDay = new Date(year, month - 1, 1).getDay(); // 获取当月第一天是周几
        if (firstDay === 0) return []; // 如果当月第一天是周日，则不需要显示上月日期
        
        // 获取上个月的最后几天
        const prevMonth = month - 1;
        const prevYear = prevMonth === 0 ? year - 1 : year;
        const prevMonthDays = new Date(prevYear, prevMonth === 0 ? 12 : prevMonth, 0).getDate();
        
        const days: number[] = [];
        for (let i = prevMonthDays - firstDay + 1; i <= prevMonthDays; i++) {
            days.push(i);
        }
        return days;
    }
    isToday(date: string): boolean {
        const today = new Date();
        const [year, month, day] = date.split('-').map(Number);
      
        return today.getFullYear() === year &&
               today.getMonth() + 1 === month &&
               today.getDate() === day;
      }
    getNextMonthDays(currentMonth: string): number[] {
        const [year, month] = currentMonth.split('-').map(Number);
        const lastDay = new Date(year, month, 0).getDate(); // 当月最后一天
        const lastDayOfWeek = new Date(year, month - 1, lastDay).getDay(); // 当月最后一天是周几
        
        if (lastDayOfWeek === 6) return []; // 如果当月最后一天是周六，则不需要显示下月日期
        
        const days: number[] = [];
        for (let i = 1; i <= 6 - lastDayOfWeek; i++) {
            days.push(i);
        }
        return days;
    }
}

export const calendar = Calendar.getInstance();