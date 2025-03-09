import ExcelJS from 'exceljs';
import type { Station, Reservation, Visiting } from './types';
import { save } from '@tauri-apps/plugin-dialog';
import { writeFile, BaseDirectory } from '@tauri-apps/plugin-fs';
import { AppError, ErrorCode } from './errors';
import { invoke } from '@tauri-apps/api/core';

// 导出工位数据
export async function exportStations(stations: Station[]) {
  try{

    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet('工位列表');
    
    // 设置表头
    worksheet.columns = [
      { header: '名称', key: 'name', width: 20 },
      { header: '简称', key: 'short_name', width: 15 },
      { header: '描述', key: 'description', width: 30 },
      { header: '图片路径', key: 'photo_path', width: 30 },
      { header: '状态', key: 'status', width: 15 }
    ];
    
    // 添加数据
    worksheet.addRows(stations);
    
    // 设置表头样式
    worksheet.getRow(1).font = { bold: true };
    
    // 生成buffer
    const buffer = await workbook.xlsx.writeBuffer();
    
    // 使用Tauri的对话框选择保存位置
    const filePath = await save({
      filters: [{
        name: 'Excel',
        extensions: ['xlsx']
      }],
      defaultPath: `工位数据_${formatDate(new Date())}.xlsx`
    });
   
    if (filePath) {
      // 保存文件
      await writeFile(filePath, new Uint8Array(buffer), { baseDir: BaseDirectory.Desktop });
    }
  }catch(e){
    throw new AppError(
      ErrorCode.ExportExcelFileError,
      '导出Excel文件时发生错误',
      `导出工位列表失败: ${String(e)}`)
  }
}

// 导出预约数据
export async function exportReservations(reservations: Reservation[]) {
  try{

    const workbook = new ExcelJS.Workbook();
    const worksheet = workbook.addWorksheet('预约列表');
    
    // 获取所有工位信息用于映射ID到名称
    const stations = await invoke<Station[]>("get_all_stations");
    const stationMap = new Map(stations.map(s => [s.id, s.name]));
    const timeSlotMap = new Map();
    timeSlotMap.set('T1',2.5);
    timeSlotMap.set('T2',2);
    timeSlotMap.set('T3',2.5);
    timeSlotMap.set('T4',2.5);
    timeSlotMap.set('T5',3.5);
    
    // 设置表头
    worksheet.columns = [
      { header: '日期', key: 'reservation_date', width: 15 },
      { header: '时间(hours)', key: 'time_slot', width: 15 },
      { header: '工位', key: 'station_id', width: 20 },
      { header: '客户名称', key: 'client_name', width: 20 },
      { header: '产品', key: 'product_name', width: 20 },
      { header: '工程师', key: 'project_engineer', width: 20 },
      { header: '测试工程师', key: 'testing_engineer', width: 20 },
      {header:"测试内容",key:"tests",width:40},
      {header:'项目号',key:'job_no',width:20},
      { header: '状态', key: 'reservation_status', width: 15 },
      { header: '预约人', key: 'reservate_by', width: 15 }
    ];
    
    // 添加数据并转换工位ID为名称
    worksheet.addRows(
      reservations.map(r => ({
        ...r,
        time_slot: timeSlotMap.get(r.time_slot),
        station_id: stationMap.get(r.station_id) || '未知工位'
      }))
    );
    
    // 设置表头样式
    worksheet.getRow(1).font = { bold: true };
    
    // 生成buffer
    const buffer = await workbook.xlsx.writeBuffer();
    
    // 使用Tauri的对话框选择保存位置
    const filePath = await save({
      filters: [{
        name: 'Excel',
        extensions: ['xlsx']
      }],
      defaultPath: `预约数据_${formatDate(new Date())}.xlsx`
    });
    
    if (filePath) {
      // 保存文件
      await writeFile(filePath, new Uint8Array(buffer), { baseDir: BaseDirectory.Desktop });
    }
  }catch(e){
    throw new AppError(
      ErrorCode.ExportExcelFileError,
      '导出Excel文件时发生错误',
      `导出预约列表失败: ${String(e)}`)
  }
}



// 格式化日期为 YYYY-MM-DD 格式
function formatDate(date: Date): string {
  return date.toISOString().split('T')[0];
}
